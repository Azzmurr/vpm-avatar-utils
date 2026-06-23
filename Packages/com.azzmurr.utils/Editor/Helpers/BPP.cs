using System.Collections.Generic;
using UnityEngine;

namespace Azzmurr.Utils {
    //https://docs.unity3d.com/Manual/class-TextureImporterOverride.html
    internal class BPPConfig {
        public static readonly Dictionary<TextureFormat, float> BPP = new() {
            //
            // Summary:
            //     Alpha-only texture format, 8 bit integer.
            { TextureFormat.Alpha8, 9 },
            //
            // Summary:
            //     A 16 bits/pixel texture format. Texture stores color with an alpha channel.
            { TextureFormat.ARGB4444, 16 },
            //
            // Summary:
            //     Color texture format, 8-bits per channel.
            { TextureFormat.RGB24, 24 },
            //
            // Summary:
            //     Color with alpha texture format, 8-bits per channel.
            { TextureFormat.RGBA32, 32 },
            //
            // Summary:
            //     Color with alpha texture format, 8-bits per channel.
            { TextureFormat.ARGB32, 32 },
            //
            // Summary:
            //     A 16 bit color texture format.
            { TextureFormat.RGB565, 16 },
            //
            // Summary:
            //     Single channel (R) texture format, 16 bit integer.
            { TextureFormat.R16, 16 },
            //
            // Summary:
            //     Compressed color texture format.
            { TextureFormat.DXT1, 4 },
            //
            // Summary:
            //     Compressed color with alpha channel texture format.
            { TextureFormat.DXT5, 8 },
            //
            // Summary:
            //     Color and alpha texture format, 4 bit per channel.
            { TextureFormat.RGBA4444, 16 },
            //
            // Summary:
            //     Color with alpha texture format, 8-bits per channel.
            { TextureFormat.BGRA32, 32 },
            //
            // Summary:
            //     Scalar (R) texture format, 16 bit floating point.
            { TextureFormat.RHalf, 16 },
            //
            // Summary:
            //     Two color (RG) texture format, 16 bit floating point per channel.
            { TextureFormat.RGHalf, 32 },
            //
            // Summary:
            //     RGB color and alpha texture format, 16 bit floating point per channel.
            { TextureFormat.RGBAHalf, 64 },
            //
            // Summary:
            //     Scalar (R) texture format, 32 bit floating point.
            { TextureFormat.RFloat, 32 },
            //
            // Summary:
            //     Two color (RG) texture format, 32 bit floating point per channel.
            { TextureFormat.RGFloat, 64 },
            //
            // Summary:
            //     RGB color and alpha texture format, 32-bit floats per channel.
            { TextureFormat.RGBAFloat, 128 },
            //
            // Summary:
            //     A format that uses the YUV color space and is often used for video encoding or
            //     playback.
            { TextureFormat.YUY2, 16 },
            //
            // Summary:
            //     RGB HDR format, with 9 bit mantissa per channel and a 5 bit shared exponent.
            { TextureFormat.RGB9e5Float, 32 },
            //
            // Summary:
            //     HDR compressed color texture format.
            { TextureFormat.BC6H, 8 },
            //
            // Summary:
            //     High quality compressed color texture format.
            { TextureFormat.BC7, 8 },
            //
            // Summary:
            //     Compressed one channel (R) texture format.
            { TextureFormat.BC4, 4 },
            //
            // Summary:
            //     Compressed two-channel (RG) texture format.
            { TextureFormat.BC5, 8 },
            //
            // Summary:
            //     Compressed color texture format with Crunch compression for smaller storage sizes.
            { TextureFormat.DXT1Crunched, 4 },
            //
            // Summary:
            //     Compressed color with alpha channel texture format with Crunch compression for
            //     smaller storage sizes.
            { TextureFormat.DXT5Crunched, 8 },
            //
            // Summary:
            //     PowerVR (iOS) 2 bits/pixel compressed color texture format.
            { TextureFormat.PVRTC_RGB2, 6 },
            //
            // Summary:
            //     PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format.
            { TextureFormat.PVRTC_RGBA2, 8 },
            //
            // Summary:
            //     PowerVR (iOS) 4 bits/pixel compressed color texture format.
            { TextureFormat.PVRTC_RGB4, 12 },
            //
            // Summary:
            //     PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format.
            { TextureFormat.PVRTC_RGBA4, 16 },
            //
            // Summary:
            //     ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
            { TextureFormat.ETC_RGB4, 12 },
            //
            // Summary:
            //     ETC2 EAC (GL ES 3.0) 4 bitspixel compressed unsigned single-channel texture format.
            { TextureFormat.EAC_R, 4 },
            //
            // Summary:
            //     ETC2 EAC (GL ES 3.0) 4 bitspixel compressed signed single-channel texture format.
            { TextureFormat.EAC_R_SIGNED, 4 },
            //
            // Summary:
            //     ETC2 EAC (GL ES 3.0) 8 bitspixel compressed unsigned dual-channel (RG) texture
            //     format.
            { TextureFormat.EAC_RG, 8 },
            //
            // Summary:
            //     ETC2 EAC (GL ES 3.0) 8 bitspixel compressed signed dual-channel (RG) texture
            //     format.
            { TextureFormat.EAC_RG_SIGNED, 8 },
            //
            // Summary:
            //     ETC2 (GL ES 3.0) 4 bits/pixel compressed RGB texture format.
            { TextureFormat.ETC2_RGB, 12 },
            //
            // Summary:
            //     ETC2 (GL ES 3.0) 4 bits/pixel RGB+1-bit alpha texture format.
            { TextureFormat.ETC2_RGBA1, 12 },
            //
            // Summary:
            //     ETC2 (GL ES 3.0) 8 bits/pixel compressed RGBA texture format.
            { TextureFormat.ETC2_RGBA8, 32 },
            //
            // Summary:
            //     ASTC (4x4 pixel block in 128 bits) compressed RGB texture format.
            { TextureFormat.ASTC_4x4, 8 },
            //
            // Summary:
            //     ASTC (5x5 pixel block in 128 bits) compressed RGB texture format.
            { TextureFormat.ASTC_5x5, 5.12f },
            //
            // Summary:
            //     ASTC (6x6 pixel block in 128 bits) compressed RGB texture format.
            { TextureFormat.ASTC_6x6, 3.55f },
            //
            // Summary:
            //     ASTC (8x8 pixel block in 128 bits) compressed RGB texture format.
            { TextureFormat.ASTC_8x8, 2 },
            //
            // Summary:
            //     ASTC (10x10 pixel block in 128 bits) compressed RGB texture format.
            { TextureFormat.ASTC_10x10, 1.28f },
            //
            // Summary:
            //     ASTC (12x12 pixel block in 128 bits) compressed RGB texture format.
            { TextureFormat.ASTC_12x12, 1 },
            //
            // Summary:
            //     Two color (RG) texture format, 8-bits per channel.
            { TextureFormat.RG16, 16 },
            //
            // Summary:
            //     Single channel (R) texture format, 8 bit integer.
            { TextureFormat.R8, 8 },
            //
            // Summary:
            //     Compressed color texture format with Crunch compression for smaller storage sizes.
            { TextureFormat.ETC_RGB4Crunched, 12 },
            //
            // Summary:
            //     Compressed color with alpha channel texture format using Crunch compression for
            //     smaller storage sizes.
            { TextureFormat.ETC2_RGBA8Crunched, 32 },
            //
            // Summary:
            //     ASTC (4x4 pixel block in 128 bits) compressed RGB(A) HDR texture format.
            { TextureFormat.ASTC_HDR_4x4, 8 },
            //
            // Summary:
            //     ASTC (5x5 pixel block in 128 bits) compressed RGB(A) HDR texture format.
            { TextureFormat.ASTC_HDR_5x5, 5.12f },
            //
            // Summary:
            //     ASTC (6x6 pixel block in 128 bits) compressed RGB(A) HDR texture format.
            { TextureFormat.ASTC_HDR_6x6, 3.55f },
            //
            // Summary:
            //     ASTC (8x8 pixel block in 128 bits) compressed RGB(A) texture format.
            { TextureFormat.ASTC_HDR_8x8, 2 },
            //
            // Summary:
            //     ASTC (10x10 pixel block in 128 bits) compressed RGB(A) HDR texture format.
            { TextureFormat.ASTC_HDR_10x10, 1.28f },
            //
            // Summary:
            //     ASTC (12x12 pixel block in 128 bits) compressed RGB(A) HDR texture format.
            { TextureFormat.ASTC_HDR_12x12, 1 },
            //
            // Summary:
            //     Two channel (RG) texture format, 16 bit integer per channel.
            { TextureFormat.RG32, 32 },
            //
            // Summary:
            //     Three channel (RGB) texture format, 16 bit integer per channel.
            { TextureFormat.RGB48, 48 },
            //
            // Summary:
            //     Four channel (RGBA) texture format, 16 bit integer per channel.
            { TextureFormat.RGBA64, 64 }
        };

        public static Dictionary<RenderTextureFormat, float> RT_BPP = new() {
            //
            // Summary:
            //     Color render texture format, 8 bits per channel.
            { RenderTextureFormat.ARGB32, 32 },
            //
            // Summary:
            //     A depth render texture format.
            { RenderTextureFormat.Depth, 0 },
            //
            // Summary:
            //     Color render texture format, 16 bit floating point per channel.
            { RenderTextureFormat.ARGBHalf, 64 },
            //
            // Summary:
            //     A native shadowmap render texture format.
            { RenderTextureFormat.Shadowmap, 8 }, //guessed bpp
            //
            // Summary:
            //     Color render texture format.
            { RenderTextureFormat.RGB565, 32 }, //guessed bpp
            //
            // Summary:
            //     Color render texture format, 4 bit per channel.
            { RenderTextureFormat.ARGB4444, 16 },
            //
            // Summary:
            //     Color render texture format, 1 bit for Alpha channel, 5 bits for Red, Green and
            //     Blue channels.
            { RenderTextureFormat.ARGB1555, 16 },
            //
            // Summary:
            //     Default color render texture format: will be chosen accordingly to Frame Buffer
            //     format and Platform.
            { RenderTextureFormat.Default, 32 }, //fuck
            //
            // Summary:
            //     Color render texture format. 10 bits for colors, 2 bits for alpha.
            { RenderTextureFormat.ARGB2101010, 32 },
            //
            // Summary:
            //     Default HDR color render texture format: will be chosen accordingly to Frame
            //     Buffer format and Platform.
            { RenderTextureFormat.DefaultHDR, 128 }, //fuck
            //
            // Summary:
            //     Four color render texture format, 16 bits per channel, fixed point, unsigned
            //     normalized.
            { RenderTextureFormat.ARGB64, 64 },
            //
            // Summary:
            //     Color render texture format, 32 bit floating point per channel.
            { RenderTextureFormat.ARGBFloat, 128 },
            //
            // Summary:
            //     Two color (RG) render texture format, 32 bit floating point per channel.
            { RenderTextureFormat.RGFloat, 64 },
            //
            // Summary:
            //     Two color (RG) render texture format, 16 bit floating point per channel.
            { RenderTextureFormat.RGHalf, 32 },
            //
            // Summary:
            //     Scalar (R) render texture format, 32 bit floating point.
            { RenderTextureFormat.RFloat, 32 },
            //
            // Summary:
            //     Scalar (R) render texture format, 16 bit floating point.
            { RenderTextureFormat.RHalf, 16 },
            //
            // Summary:
            //     Single channel (R) render texture format, 8 bit integer.
            { RenderTextureFormat.R8, 8 },
            //
            // Summary:
            //     Four channel (ARGB) render texture format, 32 bit signed integer per channel.
            { RenderTextureFormat.ARGBInt, 128 },
            //
            // Summary:
            //     Two channel (RG) render texture format, 32 bit signed integer per channel.
            { RenderTextureFormat.RGInt, 64 },
            //
            // Summary:
            //     Scalar (R) render texture format, 32 bit signed integer.
            { RenderTextureFormat.RInt, 32 },
            //
            // Summary:
            //     Color render texture format, 8 bits per channel.
            { RenderTextureFormat.BGRA32, 32 },
            //
            // Summary:
            //     Color render texture format. R and G channels are 11 bit floating point, B channel is 10 bit floating point.
            { RenderTextureFormat.RGB111110Float, 32 },
            //
            // Summary:
            //     Two color (RG) render texture format, 16 bits per channel, fixed point, unsigned normalized
            { RenderTextureFormat.RG32, 32 },
            //
            // Summary:
            //     Four channel (RGBA) render texture format, 16 bit unsigned integer per channel.
            { RenderTextureFormat.RGBAUShort, 64 },
            //
            // Summary:
            //     Two channel (RG) render texture format, 8 bits per channel.
            { RenderTextureFormat.RG16, 16 },
            //
            // Summary:
            //     Color render texture format, 10 bit per channel, extended range.
            { RenderTextureFormat.BGRA10101010_XR, 40 },
            //
            // Summary:
            //     Color render texture format, 10 bit per channel, extended range.
            { RenderTextureFormat.BGR101010_XR, 30 },
            //
            // Summary:
            //     Single channel (R) render texture format, 16 bit integer.
            { RenderTextureFormat.R16, 16 }
        };
    }
}