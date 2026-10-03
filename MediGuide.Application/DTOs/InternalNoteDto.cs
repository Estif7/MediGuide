using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record InternalNoteDto(
    Guid Id,
    Guid BookingId,
    Guid AgentId,
    string AgentName,
    string Content,
    DateTime CreatedAt
);

public record CreateInternalNoteDto(
    [Required, StringLength(4000)] string Content
);
