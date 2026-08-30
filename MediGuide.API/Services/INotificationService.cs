namespace MediGuide.API.Services;

public interface INotificationService
{
    Task NotifyAsync(string recipientUserId, string type, string message, Guid? bookingId = null);
    Task<string?> GetUserIdForPatientAsync(Guid patientId);
    Task<string?> GetUserIdForAgentAsync(Guid agentId);
}