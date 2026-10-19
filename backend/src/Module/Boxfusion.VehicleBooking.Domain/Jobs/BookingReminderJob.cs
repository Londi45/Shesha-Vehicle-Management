using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Notifications;
using NHibernate.Linq;
using Shesha.Scheduler;
using Shesha.Scheduler.Attributes;
using Shesha.Scheduler.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Jobs
{
    /// <summary>
    /// Every hour: finds active bookings that start within the next 24 hours and haven't been
    /// reminded yet, sends each customer a BookingReminder, and stamps ReminderSentDate.
    /// </summary>
    [ScheduledJob("7c1e5b2a-4d3f-4a8e-9b6c-2f0d8e1a3b45",
        startupMode: StartUpMode.Automatic,
        cronString: "0 * * * *",
        description: "Sends a reminder to customers whose booking starts within 24 hours.")]
    public class BookingReminderJob : ScheduledJobBase, ITransientDependency
    {
        private readonly IRepository<Booking, Guid> _bookingRepository;
        private readonly IBookingNotificationSender _bookingNotifications;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public BookingReminderJob(
            IRepository<Booking, Guid> bookingRepository,
            IBookingNotificationSender bookingNotifications,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _bookingRepository = bookingRepository;
            _bookingNotifications = bookingNotifications;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public override async Task DoExecuteAsync(CancellationToken cancellationToken)
        {
            Log.Info("BookingReminderJob: started");

            using (var uow = _unitOfWorkManager.Begin())
            {
                var dueBookings = await _bookingRepository.GetAll()
                    .Where(BookingRules.ReminderDue(DateTime.Now))
                    .ToListAsync(cancellationToken);

                var sent = 0;
                foreach (var booking in dueBookings)
                {
                    try
                    {
                        await _bookingNotifications.NotifyBookingReminderAsync(booking);
                        booking.ReminderSentDate = DateTime.Now;
                        await _bookingRepository.UpdateAsync(booking);
                        sent++;
                    }
                    catch (Exception ex)
                    {
                        // One bad booking must not stop the others; it will be retried next run.
                        Log.Error($"BookingReminderJob: reminder failed for booking {booking.Id}", ex);
                    }
                }

                await uow.CompleteAsync();
                Log.Info($"BookingReminderJob: {sent} of {dueBookings.Count} reminders sent");
            }
        }
    }
}
