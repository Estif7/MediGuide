using MediGuide.API.Hubs;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.API.Services;

public class NotificationService : INotificationService
{
    private readonly MediGuideDbContext _context;
    private readonly IHubContext<BookingHub> _hubContext;

    public NotificationService(MediGuideDbContext context, IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(string recipientUserId, string type, string message, Guid? bookingId = null)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Message = message,
            BookingId = bookingId,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var dto = new NotificationDto(
            notification.Id, notification.Type, notification.Message,
            notification.BookingId, notification.IsRead, notification.CreatedAt);

        await _hubContext.Clients.User(recipientUserId).SendAsync("NotificationReceived", dto);
    }

    public async Task<string?> GetUserIdForPatientAsync(Guid patientId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PatientId == patientId);
        return user?.Id;
    }

    public async Task<string?> GetUserIdForAgentAsync(Guid agentId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.AgentId == agentId);
        return user?.Id;
    }
}