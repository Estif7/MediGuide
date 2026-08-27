using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MediGuide.API.Hubs;

[Authorize]
public class BookingHub : Hub
{
    private readonly MediGuideDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public BookingHub(MediGuideDbContext context, IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    public async Task JoinBooking(Guid bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking is null)
            throw new HubException("Booking not found.");

        var authResult = await _authorizationService.AuthorizeAsync(Context.User!, booking, "BookingAccess");
        if (!authResult.Succeeded)
            throw new HubException("Not authorized for this booking.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(bookingId));
    }

    public async Task LeaveBooking(Guid bookingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(bookingId));
    }

    public static string GroupName(Guid bookingId) => $"booking-{bookingId}";
}