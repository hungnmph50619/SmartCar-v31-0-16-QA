using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SmartCar.Dto.Security;

namespace SmartCar.UnitTests;

public class FileUploadSecurityTests
{
    [Fact]
    public async Task DecodableJpeg_IsAccepted()
    {
        await using var stream = CreateJpeg();

        var result = await FileUploadSecurity.InspectAsync(
            stream, "vehicle.jpg", "image/jpeg", stream.Length, FileUploadProfile.PublicVehicleImage);

        Assert.Equal(".jpg", result.Extension);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.True(result.MustReencode);
    }

    [Fact]
    public async Task FakeJpegHeaderAndTail_IsRejected()
    {
        byte[] bytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x01, 0x02, 0xFF, 0xD9 };
        await using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FileUploadSecurity.InspectAsync(
            stream, "vehicle.jpg", "image/jpeg", bytes.Length, FileUploadProfile.PublicVehicleImage));
    }

    [Fact]
    public async Task RenamedExecutable_IsRejected()
    {
        var bytes = Encoding.ASCII.GetBytes("MZ This is not a JPEG");
        await using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FileUploadSecurity.InspectAsync(
            stream, "cccd.jpg", "image/jpeg", bytes.Length, FileUploadProfile.CustomerIdentityImage));
    }

    [Fact]
    public async Task ExtensionAndContentMismatch_IsRejected()
    {
        await using var stream = CreateJpeg();

        await Assert.ThrowsAsync<InvalidOperationException>(() => FileUploadSecurity.InspectAsync(
            stream, "cccd.png", "image/png", stream.Length, FileUploadProfile.CustomerIdentityImage));
    }

    [Fact]
    public async Task JpegWithAppendedPayload_IsRejected()
    {
        await using var valid = CreateJpeg();
        await using var stream = new MemoryStream();
        valid.Position = 0;
        await valid.CopyToAsync(stream);
        await stream.WriteAsync(Encoding.ASCII.GetBytes("MZ"));
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => FileUploadSecurity.InspectAsync(
            stream, "photo.jpg", "image/jpeg", stream.Length, FileUploadProfile.CustomerIdentityImage));
    }

    [Fact]
    public async Task StructurallyCompletePdf_IsAcceptedForPartnerDocument()
    {
        var pdf = """
                  %PDF-1.4
                  1 0 obj
                  << /Type /Catalog >>
                  endobj
                  xref
                  0 2
                  0000000000 65535 f
                  0000000009 00000 n
                  trailer
                  << /Size 2 /Root 1 0 R >>
                  startxref
                  45
                  %%EOF
                  """;
        var bytes = Encoding.Latin1.GetBytes(pdf);
        await using var stream = new MemoryStream(bytes);

        var result = await FileUploadSecurity.InspectAsync(
            stream, "dang-ky-xe.pdf", "application/pdf", bytes.Length, FileUploadProfile.PartnerDocument);

        Assert.Equal(".pdf", result.Extension);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.False(result.MustReencode);
    }

    [Fact]
    public async Task HeaderOnlyPdf_IsRejected()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\nendobj\n%%EOF");
        await using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FileUploadSecurity.InspectAsync(
            stream, "fake.pdf", "application/pdf", bytes.Length, FileUploadProfile.PartnerDocument));
    }

    [Fact]
    public async Task SafeWriter_ReencodesImage()
    {
        await using var input = CreateJpeg();
        var inspected = await FileUploadSecurity.InspectAsync(
            input, "vehicle.jpg", "image/jpeg", input.Length, FileUploadProfile.PublicVehicleImage);

        await using var output = new MemoryStream();
        await FileUploadSecurity.WriteSafeContentAsync(input, output, inspected);
        output.Position = 0;

        using var decoded = Image.Load(output);
        Assert.Equal(2, decoded.Width);
        Assert.Equal(2, decoded.Height);
        Assert.True(output.Length > 0);
    }

    private static MemoryStream CreateJpeg()
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(2, 2);
        image.Save(stream, new JpegEncoder { Quality = 90 });
        stream.Position = 0;
        return stream;
    }
}
