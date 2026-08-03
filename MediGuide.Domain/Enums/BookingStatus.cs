namespace MediGuide.Domain.Enums;

public enum BookingStatus
{
    PendingPayment = 0,
    Paid = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}