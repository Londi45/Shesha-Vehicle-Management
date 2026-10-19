using Abp.Dependency;
using Boxfusion.VehicleBooking.Domain.Domain;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Notifications
{
    /// <summary>
    /// Sends booking-related notifications (confirmed, cancelled, reminder) to the booking's customer.
    /// </summary>
    public interface IBookingNotificationSender : ITransientDependency
    {
        Task NotifyBookingConfirmedAsync(Booking booking);
        Task NotifyBookingCancelledAsync(Booking booking);
        Task NotifyBookingReminderAsync(Booking booking);
    }
}
