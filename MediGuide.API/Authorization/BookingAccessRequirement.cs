using MediGuide.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace MediGuide.API.Authorization;

public sealed class BookingAccessRequirement : IAuthorizationRequirement
{
}

public sealed class BookingAccessHandler : AuthorizationHandler<BookingAccessRequirement, Booking>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BookingAccessRequirement requirement,
        Booking booking)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (Guid.TryParse(context.User.FindFirst("patientId")?.Value, out var patientId)
            && booking.PatientId == patientId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (Guid.TryParse(context.User.FindFirst("agentId")?.Value, out var agentId)
            && booking.AgentId == agentId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
