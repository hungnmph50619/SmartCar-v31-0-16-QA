using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace SmartCar.Dto.Security
{
    public enum FileUploadProfile
    {
        PublicVehicleImage,
        CustomerIdentityImage,
        PartnerDocument,
        ReservationEvidence
    }

    public sealed record FileInspectionResult(
        string Extension,
        string ContentType,
        long MaximumSize,
        bool MustReencode);

    public static class FileUploadSecurity
    {
        private const long MaximumDecodedPixels = 50_000_000;
        private const int MaximumDimension = 12_000;

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png",
            [".webp"] = "image/webp", [".pdf"] = "application/pdf",
            [".mp4"] = "video/mp4", [".mov"] = "video/quicktime"
        };

        public static async Task<FileInspectionResult> InspectAsync(
            Stream stream, string fileName, string? declaredContentType, long length,
            FileUploadProfile profile, CancellationToken cancellationToken = default)
        {
            if (stream is null || !stream.CanRead || !stream.CanSeek)
                throw new InvalidOperationException("Không đọc hoặc kiểm tra được tệp tải lên.");
            if (length <= 0 || stream.Length <= 0)
                throw new InvalidOperationException("Tệp tải lên trống.");

            var safeName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Any(char.IsControl))
                throw new InvalidOperationException("Tên tệp không hợp lệ.");

            var ext = Path.GetExtension(safeName).ToLowerInvariant();
            var allowed = AllowedExtensions(profile);
            if (!allowed.Contains(ext))
                throw new InvalidOperationException(AllowedMessage(profile));

            var max = MaximumSize(profile, ext);
            if (length > max || stream.Length > max)
                throw new InvalidOperationException($"Tệp không được vượt quá {max / 1024 / 1024} MB.");

            var originalPosition = stream.Position;
            stream.Position = 0;
            var header = new byte[16];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            var actual = Detect(header, read);
            if (actual is null || !ExtensionMatches(ext, actual.Value.Extension))
                throw new InvalidOperationException("Nội dung thật của tệp không khớp với phần mở rộng.");

            await ValidateTailAsync(stream, actual.Value.Extension, cancellationToken);

            if (IsImage(actual.Value.Extension))
                ValidateDecodableImage(stream);
            else if (actual.Value.Extension == ".pdf")
                await ValidatePdfStructureAsync(stream, cancellationToken);
            else if (actual.Value.Extension is ".mp4" or ".mov")
                await ValidateIsoMediaStructureAsync(stream, cancellationToken);

            stream.Position = originalPosition;

            var expectedType = ContentTypes[actual.Value.Extension];
            var declared = declaredContentType?.Split(';', 2)[0].Trim();
            if (!string.IsNullOrWhiteSpace(declared) &&
                !string.Equals(declared, "application/octet-stream", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(declared, expectedType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Content-Type khai báo không khớp với nội dung thật của tệp.");

            return new FileInspectionResult(
                ext == ".jpeg" ? ".jpg" : ext,
                expectedType,
                max,
                MustReencode: IsImage(actual.Value.Extension));
        }

        /// <summary>
        /// Writes an inspected file to storage. Raster images are decoded and encoded again,
        /// removing appended payloads and common metadata. Other validated files are copied.
        /// </summary>
        public static async Task WriteSafeContentAsync(
            Stream source,
            Stream destination,
            FileInspectionResult inspection,
            CancellationToken cancellationToken = default)
        {
            if (!source.CanRead || !source.CanSeek || !destination.CanWrite)
                throw new InvalidOperationException("Không thể chuẩn hóa tệp tải lên.");

            source.Position = 0;
            if (!inspection.MustReencode)
            {
                await source.CopyToAsync(destination, 81920, cancellationToken);
                return;
            }

            ValidateImageHeader(source);
            source.Position = 0;
            using var image = Image.Load(source);
            ValidateImageDimensions(image.Width, image.Height);
            StripMetadata(image);

            IImageEncoder encoder = inspection.Extension switch
            {
                ".jpg" => new JpegEncoder { Quality = 90 },
                ".png" => new PngEncoder(),
                ".webp" => new WebpEncoder { Quality = 90 },
                _ => throw new InvalidOperationException("Định dạng ảnh không hỗ trợ chuẩn hóa.")
            };

            await image.SaveAsync(destination, encoder, cancellationToken);
        }

        private static void ValidateDecodableImage(Stream stream)
        {
            try
            {
                ValidateImageHeader(stream);
                stream.Position = 0;
                using var image = Image.Load(stream);
                ValidateImageDimensions(image.Width, image.Height);
                if (image.Frames.Count != 1)
                    throw new InvalidOperationException("Không chấp nhận ảnh động hoặc ảnh có nhiều khung hình.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException("Tệp ảnh bị hỏng hoặc không thể giải mã.", ex);
            }
        }

        private static void ValidateImageHeader(Stream stream)
        {
            stream.Position = 0;
            var info = Image.Identify(stream)
                ?? throw new InvalidOperationException("Không đọc được thông tin kích thước ảnh.");
            ValidateImageDimensions(info.Width, info.Height);
        }

        private static void ValidateImageDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0 ||
                width > MaximumDimension || height > MaximumDimension ||
                (long)width * height > MaximumDecodedPixels)
                throw new InvalidOperationException("Kích thước hoặc số điểm ảnh vượt giới hạn an toàn.");
        }

        private static void StripMetadata(Image image)
        {
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.CicpProfile = null;
        }

        private static async Task ValidatePdfStructureAsync(Stream stream, CancellationToken cancellationToken)
        {
            stream.Position = 0;
            if (stream.Length > 15L * 1024 * 1024)
                throw new InvalidOperationException("PDF vượt giới hạn kiểm tra.");

            var bytes = new byte[checked((int)stream.Length)];
            var total = 0;
            while (total < bytes.Length)
            {
                var read = await stream.ReadAsync(bytes.AsMemory(total, bytes.Length - total), cancellationToken);
                if (read == 0) break;
                total += read;
            }

            var text = Encoding.Latin1.GetString(bytes, 0, total);
            var eofIndex = text.LastIndexOf("%%EOF", StringComparison.Ordinal);
            if (eofIndex < 0 || text[(eofIndex + 5)..].Any(ch => !char.IsWhiteSpace(ch) && ch != '\0'))
                throw new InvalidOperationException("PDF bị thiếu dấu kết thúc hoặc có dữ liệu nối thêm.");

            var hasObjects = text.Contains(" obj", StringComparison.Ordinal) &&
                             text.Contains("endobj", StringComparison.Ordinal);
            var hasCrossReference = text.Contains("startxref", StringComparison.Ordinal) &&
                                    (text.Contains("xref", StringComparison.Ordinal) ||
                                     text.Contains("/Type/XRef", StringComparison.OrdinalIgnoreCase) ||
                                     text.Contains("/Type /XRef", StringComparison.OrdinalIgnoreCase));
            if (!hasObjects || !hasCrossReference)
                throw new InvalidOperationException("Cấu trúc PDF không hợp lệ hoặc không đầy đủ.");
        }

        private static async Task ValidateIsoMediaStructureAsync(Stream stream, CancellationToken cancellationToken)
        {
            stream.Position = 0;
            var scanLength = (int)Math.Min(stream.Length, 4L * 1024 * 1024);
            var bytes = new byte[scanLength];
            var total = 0;
            while (total < bytes.Length)
            {
                var read = await stream.ReadAsync(bytes.AsMemory(total, bytes.Length - total), cancellationToken);
                if (read == 0) break;
                total += read;
            }

            if (total < 16 || Encoding.ASCII.GetString(bytes, 4, 4) != "ftyp")
                throw new InvalidOperationException("Container video không hợp lệ.");

            var ascii = Encoding.ASCII.GetString(bytes, 0, total);
            if (!ascii.Contains("moov", StringComparison.Ordinal) && !ascii.Contains("mdat", StringComparison.Ordinal))
                throw new InvalidOperationException("Video thiếu cấu trúc media cần thiết.");
        }

        private static HashSet<string> AllowedExtensions(FileUploadProfile profile) => profile switch
        {
            FileUploadProfile.PublicVehicleImage or FileUploadProfile.CustomerIdentityImage
                => new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" },
            FileUploadProfile.PartnerDocument
                => new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" },
            _ => new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".mp4", ".mov" }
        };

        private static long MaximumSize(FileUploadProfile profile, string extension) => profile switch
        {
            FileUploadProfile.PublicVehicleImage => 8L * 1024 * 1024,
            FileUploadProfile.CustomerIdentityImage => 8L * 1024 * 1024,
            FileUploadProfile.PartnerDocument => 10L * 1024 * 1024,
            _ when extension is ".mp4" or ".mov" => 30L * 1024 * 1024,
            _ => 15L * 1024 * 1024
        };

        private static string AllowedMessage(FileUploadProfile profile) => profile switch
        {
            FileUploadProfile.PublicVehicleImage or FileUploadProfile.CustomerIdentityImage => "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.",
            FileUploadProfile.PartnerDocument => "Chỉ chấp nhận JPG, PNG, WEBP hoặc PDF.",
            _ => "Chỉ chấp nhận JPG, PNG, WEBP, PDF, MP4 hoặc MOV."
        };

        private static bool IsImage(string extension)
            => extension is ".jpg" or ".png" or ".webp";

        private static (string Extension, string ContentType)? Detect(byte[] h, int read)
        {
            if (read >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return (".jpg", "image/jpeg");
            if (read >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return (".png", "image/png");
            if (read >= 12 && Encoding.ASCII.GetString(h, 0, 4) == "RIFF" && Encoding.ASCII.GetString(h, 8, 4) == "WEBP") return (".webp", "image/webp");
            if (read >= 5 && Encoding.ASCII.GetString(h, 0, 5) == "%PDF-") return (".pdf", "application/pdf");
            if (read >= 12 && Encoding.ASCII.GetString(h, 4, 4) == "ftyp")
            {
                var brand = Encoding.ASCII.GetString(h, 8, 4);
                return brand.StartsWith("qt", StringComparison.OrdinalIgnoreCase) ? (".mov", "video/quicktime") : (".mp4", "video/mp4");
            }
            return null;
        }

        private static bool ExtensionMatches(string declared, string actual)
            => string.Equals(declared, actual, StringComparison.OrdinalIgnoreCase)
               || (declared is ".jpeg" && actual is ".jpg");

        private static async Task ValidateTailAsync(Stream stream, string actualExtension, CancellationToken cancellationToken)
        {
            if (stream.Length < 4)
                throw new InvalidOperationException("Tệp tải lên không hoàn chỉnh.");

            var tailLength = (int)Math.Min(4096, stream.Length);
            var tail = new byte[tailLength];
            stream.Position = stream.Length - tailLength;

            var totalRead = 0;
            while (totalRead < tailLength)
            {
                var bytesRead = await stream.ReadAsync(
                    tail.AsMemory(totalRead, tailLength - totalRead),
                    cancellationToken);
                if (bytesRead == 0)
                    break;

                totalRead += bytesRead;
            }

            if (actualExtension == ".jpg" &&
                !(totalRead >= 2 && tail[totalRead - 2] == 0xFF && tail[totalRead - 1] == 0xD9))
            {
                throw new InvalidOperationException("Ảnh JPEG bị thiếu dấu kết thúc hoặc có dữ liệu nối thêm không hợp lệ.");
            }

            if (actualExtension == ".png")
            {
                byte[] iend = { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
                if (!EndsWithBytes(tail, totalRead, iend))
                    throw new InvalidOperationException("Ảnh PNG bị thiếu khối kết thúc hoặc có dữ liệu nối thêm không hợp lệ.");
            }

            if (actualExtension == ".pdf" &&
                !Encoding.ASCII.GetString(tail, 0, totalRead).Contains("%%EOF", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Tệp PDF không hoàn chỉnh.");
            }
        }

        private static bool EndsWithBytes(byte[] buffer, int length, byte[] suffix)
        {
            if (length < suffix.Length)
                return false;

            var offset = length - suffix.Length;
            for (var i = 0; i < suffix.Length; i++)
            {
                if (buffer[offset + i] != suffix[i])
                    return false;
            }

            return true;
        }
    }
}
