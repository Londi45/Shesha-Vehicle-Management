using Abp.Domain.Repositories;
using Boxfusion.VehicleBooking.Domain.Domain;
using Castle.Core.Logging;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.EntityReferences;
using Shesha.Notifications;
using Shesha.Notifications.MessageParticipants;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Notifications
{
    public class BookingNotificationSender : IBookingNotificationSender
    {
        private readonly INotificationSender _notificationSender;
        private readonly IRepository<NotificationTypeConfig, Guid> _notificationTypeRepo;
        private readonly IRepository<NotificationChannelConfig, Guid> _channelRepo;

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public BookingNotificationSender(
            INotificationSender notificationSender,
            IRepository<NotificationTypeConfig, Guid> notificationTypeRepo,
            IRepository<NotificationChannelConfig, Guid> channelRepo)
        {
            _notificationSender = notificationSender;
            _notificationTypeRepo = notificationTypeRepo;
            _channelRepo = channelRepo;
        }

        public Task NotifyBookingConfirmedAsync(Booking booking) => SendAsync("BookingConfirmed", booking);

        public Task NotifyBookingCancelledAsync(Booking booking) => SendAsync("BookingCancelled", booking);

        public Task NotifyBookingReminderAsync(Booking booking) => SendAsync("BookingReminder", booking);

        private async Task SendAsync(string typeName, Booking booking)
        {
            if (booking?.Customer == null)
            {
                Logger.Warn($"Notification '{typeName}' skipped: booking {booking?.Id} has no customer.");
                return;
            }

            // Created by migration M20261012140000; skip quietly if it hasn't run yet.
            var type = await _notificationTypeRepo.FirstOrDefaultAsync(t => t.Name == typeName);
            if (type == null)
            {
                Logger.Warn($"Notification type '{typeName}' not found - has the migration run?");
                return;
            }

            // Email only for now (no SMS provider is configured).
            var email = await _channelRepo.FirstOrDefaultAsync(c => c.Name == "Email");

            var culture = CultureInfo.GetCultureInfo("en-ZA");
            var data = new BookingNotificationModel
            {
                CustomerName = booking.Customer.FirstName,
                VehicleName = booking.Vehicle?.DisplayName ?? "your vehicle",
                StartDate = booking.StartDate.ToString("d MMM yyyy, HH:mm", culture),
                EndDate = booking.EndDate.ToString("d MMM yyyy, HH:mm", culture),
                // Plain spaces only: the en-ZA currency format uses a non-breaking space, which the template HTML-escapes to "&#160;".
                TotalCost = "R " + booking.TotalCosts.ToString("#,##0.00", CultureInfo.InvariantCulture)
            };

            await _notificationSender.SendNotificationAsync(
                type,
                null,                                              // sent by the system, not a person
                new PersonMessageParticipant(booking.Customer),    // Customer is a Person
                data,
                null,                                              // no attachments
                null,                                              // no cc
                // Links the message to this booking for audit. Built explicitly (id, class, display name) so it
                // doesn't need Shesha's entity registry - which also makes the sender unit-testable.
                new GenericEntityReference(booking.Id.ToString(), typeof(Booking).FullName, $"Booking: {data.VehicleName}"),
                email);
        }
    }
}
