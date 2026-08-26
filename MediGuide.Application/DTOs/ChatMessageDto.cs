using System.ComponentModel.DataAnnotations;

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
    [Required, StringLength(4000)] string Content
);
