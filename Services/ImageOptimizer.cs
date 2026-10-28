using SkiaSharp;

namespace Foras_Khadra.Services
{
    /// <summary>صورة بعد التصغير وإعادة الترميز.</summary>
    public sealed record OptimizedImage(byte[] Bytes, string Extension, int Width, int Height);

    /// <summary>
    /// تصغير الصور وإعادة ترميزها قبل الحفظ. الاستضافة ترسل كل ملف بسرعة منخفضة، فصورة بميغابايتات لبطاقة
    /// عرضها مئات البكسلات تبطئ الصفحة كلها. الصور المعتمة تُحفظ JPEG (تدعمها كل بطاقات المشاركة)،
    /// وذات الشفافية (الشعارات غالبًا) WebP.
    /// </summary>
    public static class ImageOptimizer
    {
        /// <summary>أكبر ضلع للشعارات (تُعرض في مربعات حتى 300 بكسل).</summary>
        public const int LogoMaxEdge = 600;
        /// <summary>أكبر ضلع لصور الفرص والمقالات والنشاطات والفريق.</summary>
        public const int PhotoMaxEdge = 1200;
        public const int Quality = 80;
        /// <summary>PNG (ونحوه من الصيغ غير المضغوطة) أصغر من هذا وبأبعاد ضمن الحد يُترك كما هو.</summary>
        public const long OptimizeOverBytes = 150 * 1024;
        /// <summary>
        /// JPEG/WebP بأبعاد ضمن الحد يُعاد ترميزه فقط إن تجاوز هذا الحجم وصغر بوضوح؛ وإلا تُترك الصورة كما هي
        /// حتى لا تفقد جودتها مع كل تشغيل للأداة.
        /// </summary>
        public const long ReencodeOverBytes = 500 * 1024;
        /// <summary>حد الأبعاد قبل فك البكسلات حتى لا تستهلك صورة ضخمة ذاكرة الخادم.</summary>
        public const long MaxPixels = 50_000_000;

        /// <summary>أكبر ضلع بحسب مجلد الحفظ: الشعارات أصغر من الصور.</summary>
        public static int MaxEdgeFor(string subfolder) =>
            subfolder.TrimStart('/', '\\').StartsWith("logos", StringComparison.OrdinalIgnoreCase) ? LogoMaxEdge : PhotoMaxEdge;

        /// <summary>
        /// يصغّر الصورة إلى maxEdge ويعيد ترميزها إن كان ذلك مجديًا. يعيد null إذا كانت الصورة صغيرة أصلًا،
        /// أو GIF (قد يكون متحركًا)، أو تعذّر فكّها — وعندها تُحفظ كما وصلت.
        /// </summary>
        public static OptimizedImage? Optimize(byte[] source, int maxEdge)
        {
            if (source.Length < 12) return null;
            if (source[0] == 'G' && source[1] == 'I' && source[2] == 'F') return null;

            try
            {
                using var data = SKData.CreateCopy(source);
                using var codec = SKCodec.Create(data);
                if (codec == null) return null;

                var (w, h) = (codec.Info.Width, codec.Info.Height);
                bool needsResize = Math.Max(w, h) > maxEdge;
                bool alreadyLossy = codec.EncodedFormat is SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp;
                if (!needsResize && source.Length <= (alreadyLossy ? ReencodeOverBytes : OptimizeOverBytes)) return null;

                var format = codec.Info.IsOpaque ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Webp;
                var result = Transcode(codec, maxEdge, format, Quality);
                if (result == null) return null;

                // بلا تصغير: لا نستبدل الملف إلا إن صغر فعلًا (وبوضوح إن كان مضغوطًا أصلًا)
                if (!needsResize && result.Bytes.Length >= source.Length * (alreadyLossy ? 0.7 : 1.0)) return null;
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>يعيد ترميز الصورة دائمًا بالصيغة والجودة المعطاتين بعد تصغيرها إلى maxEdge. null إن تعذّر فكّها.</summary>
        public static OptimizedImage? Transcode(byte[] source, int maxEdge, SKEncodedImageFormat format, int quality)
        {
            try
            {
                using var data = SKData.CreateCopy(source);
                using var codec = SKCodec.Create(data);
                return codec == null ? null : Transcode(codec, maxEdge, format, quality);
            }
            catch
            {
                return null;
            }
        }

        private static OptimizedImage? Transcode(SKCodec codec, int maxEdge, SKEncodedImageFormat format, int quality)
        {
            var (w, h) = (codec.Info.Width, codec.Info.Height);
            if (w <= 0 || h <= 0 || (long)w * h > MaxPixels) return null;

            var scale = Math.Min(1f, (float)maxEdge / Math.Max(w, h));
            var targetW = Math.Max(1, (int)Math.Round(w * scale));
            var targetH = Math.Max(1, (int)Math.Round(h * scale));

            // JPEG يُفك مصغّرًا مباشرة إن أمكن (ذاكرة أقل)، ثم يُستكمل التصغير بالبكسلات
            var decodeSize = codec.GetScaledDimensions(scale);
            var decodeInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var decoded = new SKBitmap(decodeInfo);
            if (codec.GetPixels(decodeInfo, decoded.GetPixels()) != SKCodecResult.Success) return null;

            SKBitmap working = decoded;
            SKBitmap? resized = null, oriented = null;
            try
            {
                if (decoded.Width != targetW || decoded.Height != targetH)
                {
                    resized = decoded.Resize(new SKImageInfo(targetW, targetH, SKColorType.Rgba8888, SKAlphaType.Premul),
                                             new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                    if (resized == null) return null;
                    working = resized;
                }

                // اتجاه EXIF (صور الهواتف): يُطبَّق على البكسلات لأن الصيغة الجديدة لا تحمل الوسم
                oriented = ApplyOrientation(working, codec.EncodedOrigin);
                if (oriented != null) working = oriented;

                // JPEG لا يحمل شفافية: الخلفية بيضاء
                if (format == SKEncodedImageFormat.Jpeg && !working.Info.IsOpaque)
                {
                    using var flat = new SKBitmap(new SKImageInfo(working.Width, working.Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
                    using (var canvas = new SKCanvas(flat))
                    {
                        canvas.Clear(SKColors.White);
                        canvas.DrawBitmap(working, 0, 0);
                    }
                    return Encode(flat, format, quality);
                }

                return Encode(working, format, quality);
            }
            finally
            {
                oriented?.Dispose();
                resized?.Dispose();
            }
        }

        private static OptimizedImage? Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(format, quality);
            if (encoded == null || encoded.Size == 0) return null;

            var ext = format switch
            {
                SKEncodedImageFormat.Jpeg => ".jpg",
                SKEncodedImageFormat.Png => ".png",
                _ => ".webp"
            };
            return new OptimizedImage(encoded.ToArray(), ext, bitmap.Width, bitmap.Height);
        }

        /// <summary>صورة جديدة مدوّرة بحسب وسم الاتجاه، أو null إن لم يلزم تدوير.</summary>
        private static SKBitmap? ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
        {
            if (origin == SKEncodedOrigin.TopLeft || origin == SKEncodedOrigin.Default) return null;

            bool swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var (w, h) = swap ? (source.Height, source.Width) : (source.Width, source.Height);
            var rotated = new SKBitmap(new SKImageInfo(w, h, source.ColorType, source.AlphaType));
            using var canvas = new SKCanvas(rotated);
            canvas.Clear(SKColors.Transparent);

            // التحويلات تُطبَّق على الرسم بترتيب عكس كتابتها: الأخير أولًا
            switch (origin)
            {
                case SKEncodedOrigin.TopRight: canvas.Scale(-1, 1, w / 2f, 0); break;                   // انعكاس أفقي
                case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180, w / 2f, h / 2f); break;      // دوران 180
                case SKEncodedOrigin.BottomLeft: canvas.Scale(1, -1, 0, h / 2f); break;                  // انعكاس رأسي
                case SKEncodedOrigin.RightTop: canvas.Translate(w, 0); canvas.RotateDegrees(90); break;  // دوران 90 مع عقارب الساعة
                case SKEncodedOrigin.LeftBottom: canvas.Translate(0, h); canvas.RotateDegrees(-90); break; // دوران 90 عكس عقارب الساعة
                case SKEncodedOrigin.LeftTop: canvas.Scale(-1, 1, w / 2f, 0); canvas.Translate(w, 0); canvas.RotateDegrees(90); break;   // تبديل المحورين
                case SKEncodedOrigin.RightBottom: canvas.Scale(1, -1, 0, h / 2f); canvas.Translate(w, 0); canvas.RotateDegrees(90); break; // تبديل مع انعكاس
            }

            canvas.DrawBitmap(source, 0, 0);
            return rotated;
        }
    }
}
