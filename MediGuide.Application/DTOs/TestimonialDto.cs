using System.ComponentModel.DataAnnotations;

namespace MediGuide.Application.DTOs;

public record TestimonialDto(
    Guid Id,
    string PatientName,
    int Rating,
    string Comment,
    DateTime CreatedAt
);

public record AdminTestimonialDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string PatientEmail,
    int Rating,
    string Comment,
    bool IsApproved,
    Guid? BookingId,
    DateTime CreatedAt
);

public record CreateTestimonialDto(
    [Range(1, 5)] int Rating,
    [Required, StringLength(2000)] string Comment,
    Guid? BookingId = null
);
