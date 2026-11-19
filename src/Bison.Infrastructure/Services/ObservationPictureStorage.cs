namespace Bison.Infrastructure.Services;

public sealed record PictureUploadResult(string? PicturePath, string? ErrorMessage);

public sealed class ObservationPictureStorage
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly string _uploadDirectory;

    public ObservationPictureStorage(string webRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webRootPath);
        _uploadDirectory = Path.Combine(webRootPath, "uploads", "observations");
    }

    public async Task<PictureUploadResult> SaveAsync(Stream? picture, long pictureLength)
    {
        if (picture is null || pictureLength == 0)
        {
            return new PictureUploadResult(null, null);
        }

        if (pictureLength > MaxFileSize)
        {
            return new PictureUploadResult(null, "Picture must be 5 MB or smaller.");
        }

        var header = new byte[12];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var read = await picture.ReadAsync(header.AsMemory(bytesRead));
            if (read == 0)
            {
                break;
            }

            bytesRead += read;
        }

        var extension = GetImageExtension(header, bytesRead);
        if (extension is null)
        {
            return new PictureUploadResult(null, "Upload a JPEG, PNG, GIF, or WebP image.");
        }

        Directory.CreateDirectory(_uploadDirectory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_uploadDirectory, fileName);
        await using (var output = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await output.WriteAsync(header.AsMemory(0, bytesRead));
            await picture.CopyToAsync(output);
        }

        return new PictureUploadResult($"/uploads/observations/{fileName}", null);
    }

    // Removes an uploaded picture from disk. Only the file name is used,
    // so a path like "../../x" can never point outside the upload folder.
    public void Delete(string? picturePath)
    {
        if (string.IsNullOrWhiteSpace(picturePath))
        {
            return;
        }

        var fileName = Path.GetFileName(picturePath);
        var filePath = Path.Combine(_uploadDirectory, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string? GetImageExtension(byte[] header, int bytesRead)
    {
        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (bytesRead >= 8 &&
            header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (bytesRead >= 6 &&
            (header.AsSpan(0, 6).SequenceEqual("GIF87a"u8) ||
             header.AsSpan(0, 6).SequenceEqual("GIF89a"u8)))
        {
            return ".gif";
        }

        if (bytesRead >= 12 &&
            header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }
}
