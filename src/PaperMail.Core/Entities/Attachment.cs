namespace PaperMail.Core.Entities;

public sealed class Attachment
{
    public string FileName { get; }
    public long SizeBytes { get; }
    public string ContentType { get; }

    public Attachment(string fileName, long sizeBytes, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name required", nameof(fileName));
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        FileName = fileName.Trim();
        SizeBytes = sizeBytes;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
    }
}