namespace MediGuide.Application.DTOs;

public record ChatMessageDto(
    Guid Id,
    Guid BookingId,
    string SenderId,
    string SenderRole,
    string Content,
    bool IsRead,
    DateTime CreatedAt
);

public record CreateChatMessageDto(
    string Content
);