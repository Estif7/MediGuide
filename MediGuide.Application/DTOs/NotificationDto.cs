namespace MediGuide.Application.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Message,
    Guid? BookingId,
    bool IsRead,
    DateTime CreatedAt
);