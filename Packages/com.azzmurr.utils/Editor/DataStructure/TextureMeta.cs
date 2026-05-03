using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Azzmurr.Utils {
    [Serializable]
    internal class TextureMeta : IEqualityComparer<TextureMeta> {
        public readonly Texture Texture;

        public TextureMeta(Texture t) {
            Texture = t;
        }

        public string Path => AssetDatabase.GetAssetPath(Texture);
        public bool Poiyomi => Path.Contains("_PoiyomiShaders");
        public TextureImporter Importer => AssetImporter.GetAtPath(Path) as TextureImporter;
        public long Size => GetSize();
        public string SizeString => ToMebiByteString(Size);
        public TextureFormat? Format => GetTextureFormat();
        public RenderTextureFormat? RTFormat => GetRenderTextureFormat();
        public string FormatString => Format != null ? Format.ToString() : RTFormat != null ? RTFormat.ToString() : "";
        public float Bpp => GetBpp();
        public int MinBpp => GetMinBpp();
        public bool HasAlpha => GetHasAlpha();
        public int PcResolution => GetMaxResolution("PC");
        public int AndroidResolution => GetMaxResolution("Android");
        public bool TextureWithChangeableResolution => Importer != null && !Poiyomi;
        public bool TextureWithChangeableFormat => GetTextureHasChangeableFormat();
        public TextureImporterFormat? BetterTextureFormat => GetBetterFormat();
        public string SavedSizeWithBetterTextureFormat => GetSavedSizeString();
        public bool TextureTooBig => Importer != null && PcResolution > 2048;

        public string SaveSizeWithSmallerTexture =>
            ToShortMebiByteString(Size - TextureToBytesUsingBpp(Texture, Bpp, 2048f / PcResolution));

        public TextureImporterFormat? BestTextureFormat => GetTheBestFormat();

        public bool Equals(TextureMeta x, TextureMeta y) {
            return x != null && y != null && x.Texture.Equals(y.Texture);
        }

        public int GetHashCode(TextureMeta obj) {
            return obj.Texture.GetHashCode();
        }

        public void ChangeImportSize(int size) {
            if (!TextureWithChangeableResolution) return;

            var settings = Importer.GetPlatformTextureSettings("PC");
            Importer.maxTextureSize = size;
            settings.maxTextureSize = size;
            Importer.SetPlatformTextureSettings(settings);
            Importer.SaveAndReimport();
        }

        public void ChangeImportSizeAndroid(int size) {
            if (!TextureWithChangeableResolution) return;

            var settings = Importer.GetPlatformTextureSettings("Android");
            settings.maxTextureSize = size;
            settings.overridden = true;
            Importer.SetPlatformTextureSettings(settings);
            Importer.SaveAndReimport();
        }

        public void ChangeImporterFormat(TextureImporterFormat format) {
            if (!TextureWithChangeableFormat) return;

            var settings = Importer.GetPlatformTextureSettings("PC");
            var settingsA = Importer.GetPlatformTextureSettings("Android");

            settings.overridden = (int)format != -1;
            settings.format = format;
            settings.compressionQuality = 100;

            settingsA.overridden = (int)format != -1;
            settingsA.format = format;
            settingsA.compressionQuality = 100;

            Importer.SetPlatformTextureSettings(settings);
            Importer.SetPlatformTextureSettings(settingsA);

            Importer.SaveAndReimport();
        }

        private TextureImporterFormat? GetBetterFormat() {
            Debug.Log($"{Bpp} => {MinBpp}");
            if (TextureWithChangeableFormat && Bpp > MinBpp) return GetTheBestFormat();

            return null;
        }

        private TextureImporterFormat? GetTheBestFormat() {
            if (TextureWithChangeableFormat)
                return HasAlpha || Importer.textureType == TextureImporterType.NormalMap
                    ? TextureImporterFormat.DXT5Crunched
                    : TextureImporterFormat.DXT1Crunched;

            return null;
        }

        private string GetSavedSizeString() {
            return BetterTextureFormat != null
                ? ToShortMebiByteString(Size -
                                        TextureToBytesUsingBpp(Texture,
                                            BPPConfig.BPP[(TextureFormat)BetterTextureFormat]))
                : "";
        }

        private int GetMaxResolution(string platform) {
            if (Importer)
                return Importer.GetPlatformTextureSettings(platform).overridden
                    ? Importer.GetPlatformTextureSettings(platform).maxTextureSize
                    : Importer.GetDefaultPlatformTextureSettings().maxTextureSize;

            return 0;
        }

        private TextureFormat? GetTextureFormat() {
            return Texture switch {
                Texture2D texture2D => texture2D.format,
                Texture2DArray array => array.format,
                Cubemap cubemap => cubemap.format,
                _ => null
            };
        }

        private RenderTextureFormat? GetRenderTextureFormat() {
            return Texture switch {
                RenderTexture texture => texture.format,
                _ => null
            };
        }

        private float GetBpp() {
            return Texture switch {
                Texture2D => Format == null ? 16 : BPPConfig.BPP.GetValueOrDefault((TextureFormat)Format, 16),
                Texture2DArray => Format == null ? 16 : BPPConfig.BPP.GetValueOrDefault((TextureFormat)Format, 16),
                Cubemap => Format == null ? 16 : BPPConfig.BPP.GetValueOrDefault((TextureFormat)Format, 16),
                RenderTexture => RTFormat == null
                    ? 16
                    : BPPConfig.RT_BPP.GetValueOrDefault((RenderTextureFormat)RTFormat, 16) +
                      ((RenderTexture)Texture).depth,
                _ => 16
            };
        }

        private long GetSize() {
            return Texture switch {
                Texture2D => TextureToBytesUsingBpp(Texture, Bpp),
                Texture2DArray => TextureToBytesUsingBpp(Texture, Bpp) * ((Texture2DArray)Texture).depth,
                Cubemap => TextureToBytesUsingBpp(Texture, Bpp) *
                           (((Cubemap)Texture).dimension == TextureDimension.Tex3D ? 6 : 1),
                RenderTexture => TextureToBytesUsingBpp(Texture, Bpp),
                _ => Profiler.GetRuntimeMemorySizeLong(Texture)
            };
        }

        private bool GetHasAlpha() {
            return Texture switch {
                Texture2D => Importer != null && Importer.DoesSourceTextureHaveAlpha(),
                RenderTexture => RTFormat is RenderTextureFormat.ARGB32 or RenderTextureFormat.ARGBHalf
                    or RenderTextureFormat.ARGBFloat,
                _ => false
            };
        }

        private int GetMinBpp() {
            return Texture switch {
                Texture2D => HasAlpha || (Importer != null && Importer.textureType == TextureImporterType.NormalMap)
                    ? 8
                    : 4,
                _ => 8
            };
        }

        private bool GetTextureHasChangeableFormat() {
            return Texture switch {
                Texture2D => !Poiyomi && Importer != null &&
                             Importer.textureType != TextureImporterType.SingleChannel && FormatString.Length > 0,
                _ => false
            };
        }

        private static string ToMebiByteString(long l) {
            if (l < Math.Pow(2, 10)) return l + " B";
            if (l < Math.Pow(2, 20)) return (l / Math.Pow(2, 10)).ToString("n2") + " KiB";
            if (l < Math.Pow(2, 30)) return (l / Math.Pow(2, 20)).ToString("n2") + " MiB";
            return (l / Math.Pow(2, 30)).ToString("n2") + " GiB";
        }

        private static string ToShortMebiByteString(long l) {
            if (l < Math.Pow(2, 10)) return l + " B";
            if (l < Math.Pow(2, 20)) return (l / Math.Pow(2, 10)).ToString("n0") + " KiB";
            if (l < Math.Pow(2, 30)) return (l / Math.Pow(2, 20)).ToString("n1") + " MiB";
            return (l / Math.Pow(2, 30)).ToString("n1") + " GiB";
        }

        private static long TextureToBytesUsingBpp(Texture t, float bpp, float resolutionScale = 1) {
            var width = (int)(t.width * resolutionScale);
            var height = (int)(t.height * resolutionScale);
            long bytes = 0;
            switch (t) {
                case Texture2D or Texture2DArray or Cubemap: {
                    for (var index = 0; index < t.mipmapCount; ++index)
                        bytes += Mathf.RoundToInt(((width * height) >> (2 * index)) * bpp / 8);
                    break;
                }
                case RenderTexture rt: {
                    double mipmaps = 1;
                    for (var i = 0; i < rt.mipmapCount; i++) mipmaps += Math.Pow(0.25, i + 1);
                    bytes = (long)((BPPConfig.RT_BPP[rt.format] + rt.depth) * width * height *
                        (rt.useMipMap ? mipmaps : 1) / 8);
                    break;
                }
                default:
                    bytes = Profiler.GetRuntimeMemorySizeLong(t);
                    break;
            }

            return bytes;
        }
    }
}