using Abp.Notifications;

namespace Boxfusion.VehicleBooking.Domain.Notifications
{
    public class BookingNotificationModel : NotificationData
    {
        public string CustomerName { get; set; }
        public string VehicleName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string TotalCost { get; set; }
    }
}