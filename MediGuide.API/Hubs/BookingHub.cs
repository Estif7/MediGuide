using System.Collections.Concurrent;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MediGuide.API.Hubs;

[Authorize]
public class BookingHub : Hub
{
    private readonly MediGuideDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    // Tracks which connections are currently viewing which booking.
    // bookingId -> set of connectionIds currently in that booking's group.
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> ActiveViewers = new();

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

        var viewers = ActiveViewers.GetOrAdd(bookingId, _ => new ConcurrentDictionary<string, byte>());
        viewers[Context.ConnectionId] = 0;
    }

    public async Task LeaveBooking(Guid bookingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(bookingId));
        RemoveViewer(bookingId, Context.ConnectionId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up this connection from every booking it was viewing.
        foreach (var bookingId in ActiveViewers.Keys)
        {
            RemoveViewer(bookingId, Context.ConnectionId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    private static void RemoveViewer(Guid bookingId, string connectionId)
    {
        if (ActiveViewers.TryGetValue(bookingId, out var viewers))
        {
            viewers.TryRemove(connectionId, out _);
            if (viewers.IsEmpty)
                ActiveViewers.TryRemove(bookingId, out _);
        }
    }

    /// <summary>
    /// True if any connection is currently viewing this booking.
    /// Does not distinguish which user — callers combine this with
    /// "is the sender excluded" logic themselves where needed.
    /// </summary>
    public static bool HasActiveViewers(Guid bookingId) =>
        ActiveViewers.TryGetValue(bookingId, out var viewers) && !viewers.IsEmpty;

    public static string GroupName(Guid bookingId) => $"booking-{bookingId}";
}