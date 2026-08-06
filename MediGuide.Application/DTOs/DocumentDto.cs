namespace MediGuide.Application.DTOs;

public record DocumentDto(
    Guid Id,
    Guid BookingId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string UploadedBy,
    DateTime CreatedAt
);