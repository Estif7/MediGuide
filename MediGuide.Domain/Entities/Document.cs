using MediGuide.Domain.Common;

namespace MediGuide.Domain.Entities;

public class Document : BaseEntity
{
    public Guid BookingId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;   // local path or blob URL
    public long FileSizeBytes { get; set; }
    public string UploadedBy { get; set; } = "Patient";       // Patient or Agent

    // Navigation
    public Booking Booking { get; set; } = null!;
}