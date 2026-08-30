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

    // bookingId -> (userId -> set of connectionIds for that user viewing that booking)
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>> ActiveViewers = new();

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

        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            var bookingViewers = ActiveViewers.GetOrAdd(bookingId, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>());
            var userConnections = bookingViewers.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            userConnections[Context.ConnectionId] = 0;
        }
    }

    public async Task LeaveBooking(Guid bookingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(bookingId));
        RemoveViewer(bookingId, Context.UserIdentifier, Context.ConnectionId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            foreach (var bookingId in ActiveViewers.Keys)
            {
                RemoveViewer(bookingId, userId, Context.ConnectionId);
            }
        }
        return base.OnDisconnectedAsync(exception);
    }

    private static void RemoveViewer(Guid bookingId, string? userId, string connectionId)
    {
        if (userId is null) return;

        if (ActiveViewers.TryGetValue(bookingId, out var bookingViewers)
            && bookingViewers.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
            if (connections.IsEmpty)
            {
                bookingViewers.TryRemove(userId, out _);
                if (bookingViewers.IsEmpty)
                    ActiveViewers.TryRemove(bookingId, out _);
            }
        }
    }

    /// <summary>
    /// True if the specific given user currently has at least one connection
    /// actively viewing this booking (accounts for multiple tabs correctly).
    /// </summary>
    public static bool IsUserViewingBooking(Guid bookingId, string userId) =>
        ActiveViewers.TryGetValue(bookingId, out var bookingViewers)
        && bookingViewers.TryGetValue(userId, out var connections)
        && !connections.IsEmpty;

    public static string GroupName(Guid bookingId) => $"booking-{bookingId}";
}