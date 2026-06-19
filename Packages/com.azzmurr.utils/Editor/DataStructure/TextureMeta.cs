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
        public TextureFormat? TextureFormat => GetTextureFormat();
        public RenderTextureFormat? RTFormat => GetRenderTextureFormat();
        public string FormatString => TextureFormat != null ? TextureFormat.ToString() : RTFormat != null ? RTFormat.ToString() : "";
        public float Bpp => GetBpp();
        public int MinBpp => GetMinBpp();
        public bool HasAlpha => GetHasAlpha();

        public int DefaultResolution => GetMaxResolution("Default");

        public int PcResolution => GetMaxResolution("PC");
        public TextureImporterFormat? BestPCTextureFormat => GetTheBestPCFormat();
        public TextureImporterFormat? CrunchPCTextureFormat => GetCrunchPCTextureFormat();
        public TextureImporterFormat? PCFormat => GetPCTextureFormat();

        public int AndroidResolution => GetMaxResolution("Android");
        public TextureImporterFormat? BestAndroidTextureFormat => GetTheBestAndroidFormat();
        public TextureImporterFormat? AndroidFormat => GetAndroidTextureFormat();

        public bool TextureWithChangeableResolution => Importer != null && !Poiyomi;
        public bool TextureWithChangeableFormat => GetTextureHasChangeableFormat();
        public bool TextureTooBig => Importer != null && PcResolution > 2048;

        public string SaveSizeWithSmallerTexture =>
            ToShortMebiByteString(Size - TextureToBytesUsingBpp(Texture, Bpp, 2048f / PcResolution));

        public bool Equals(TextureMeta x, TextureMeta y) {
            return x != null && y != null && x.Texture.Equals(y.Texture);
        }

        public int GetHashCode(TextureMeta obj) {
            return obj.Texture.GetHashCode();
        }

        public void ChangeDefaultImportSize(int size) {
            if (!TextureWithChangeableResolution) return;
            Importer.maxTextureSize = size;
            Importer.SaveAndReimport();
        }

        public void ChangePCImportSize(int size) {
            if (!TextureWithChangeableResolution) return;

            var settings = Importer.GetPlatformTextureSettings("PC");
            settings.maxTextureSize = size;
            settings.overridden = true;
            Importer.SetPlatformTextureSettings(settings);
            Importer.SaveAndReimport();
        }

        public void ChangeAndroidImportSize(int size) {
            if (!TextureWithChangeableResolution) return;

            var settings = Importer.GetPlatformTextureSettings("Android");
            settings.maxTextureSize = size;
            settings.overridden = true;
            Importer.SetPlatformTextureSettings(settings);
            Importer.SaveAndReimport();
        }

        public void ChangePCImporterFormat(TextureImporterFormat? format) {
            if (!TextureWithChangeableFormat || format == null) return;

            var settings = Importer.GetPlatformTextureSettings("PC");

            settings.overridden = (int)format.Value != -1;
            settings.format = format.Value;
            settings.compressionQuality = 100;

            Importer.SetPlatformTextureSettings(settings);
            Importer.SaveAndReimport();
        }

        public void ChangeAndroidImporterFormat(TextureImporterFormat? format) {
            if (!TextureWithChangeableFormat || format == null) return;

            var settings = Importer.GetPlatformTextureSettings("Android");

            settings.overridden = (int)format.Value != -1;
            settings.format = format.Value;
            settings.compressionQuality = 100;

            Importer.SetPlatformTextureSettings(settings);
            Importer.SaveAndReimport();
        }

        private TextureImporterFormat? GetTheBestPCFormat() {
            if (!TextureWithChangeableFormat) return null;
            return Importer.textureType switch {
                TextureImporterType.NormalMap => TextureImporterFormat.BC5,
                _ => HasAlpha ? TextureImporterFormat.DXT5 : TextureImporterFormat.BC7,
            };
        }

        private TextureImporterFormat? GetTheBestAndroidFormat() {
            if (!TextureWithChangeableFormat) return null;
            return Importer.textureType switch {
                TextureImporterType.NormalMap => TextureImporterFormat.ASTC_6x6,
                _ => TextureImporterFormat.ASTC_4x4,
            };
        }

        private TextureImporterFormat? GetCrunchPCTextureFormat() {
            if (!TextureWithChangeableFormat) return null;
            return Importer.textureType switch {
                TextureImporterType.NormalMap => TextureImporterFormat.DXT5Crunched,
                _ => HasAlpha ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched,
            };
        }

        private int GetDefaultResolution() {
            return Importer ? Importer.GetDefaultPlatformTextureSettings().maxTextureSize : 0;
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

        private TextureImporterFormat? GetPCTextureFormat() {
            if (Importer) {
                return Importer.GetPlatformTextureSettings("PC").overridden
                    ? Importer.GetPlatformTextureSettings("PC").format
                    : Importer.GetDefaultPlatformTextureSettings().format;
            }

            return null;
        }

        private TextureImporterFormat? GetAndroidTextureFormat() {
            if (Importer) {
                return Importer.GetPlatformTextureSettings("Android").overridden
                    ? Importer.GetPlatformTextureSettings("Android").format
                    : Importer.GetDefaultPlatformTextureSettings().format;
            }

            return null;
        }

        private RenderTextureFormat? GetRenderTextureFormat() {
            return Texture switch {
                RenderTexture texture => texture.format,
                _ => null
            };
        }

        private float GetBpp() {
            return Texture switch {
                Texture2D => TextureFormat == null ? 16 : BPPConfig.BPP.GetValueOrDefault((TextureFormat)TextureFormat, 16),
                Texture2DArray => TextureFormat == null ? 16 : BPPConfig.BPP.GetValueOrDefault((TextureFormat)TextureFormat, 16),
                Cubemap => TextureFormat == null ? 16 : BPPConfig.BPP.GetValueOrDefault((TextureFormat)TextureFormat, 16),
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
                RenderTexture => RTFormat is RenderTextureFormat.ARGB32 or RenderTextureFormat.ARGBHalf or RenderTextureFormat.ARGBFloat,
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
