namespace Papermail.Core.Entities;

/// <summary>
/// Represents an email attachment with file information.
/// This is an immutable value object that describes file metadata.
/// </summary>
public sealed class Attachment
{
    /// <summary>
    /// Gets the file name of the attachment.
    /// </summary>
    public string FileName { get; }
    
    /// <summary>
    /// Gets the size of the attachment in bytes.
    /// </summary>
    public long SizeBytes { get; }
    
    /// <summary>
    /// Gets the MIME content type of the attachment.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attachment"/> class.
    /// </summary>
    /// <param name="fileName">The file name of the attachment.</param>
    /// <param name="sizeBytes">The size of the attachment in bytes.</param>
    /// <param name="contentType">The MIME content type of the attachment.</param>
    /// <exception cref="ArgumentException">Thrown when the file name is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the size is negative.</exception>
    public Attachment(string fileName, long sizeBytes, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name required", nameof(fileName));
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        FileName = fileName.Trim();
        SizeBytes = sizeBytes;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
    }
}