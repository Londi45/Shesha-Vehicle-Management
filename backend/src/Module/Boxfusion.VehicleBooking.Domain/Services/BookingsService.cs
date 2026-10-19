using Abp.Authorization;
using Abp.Domain.Repositories;
using NHibernate.Linq;
using Boxfusion.VehicleBooking.Domain.Authorization;
using Abp.UI;
using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Domain.Enums;
using Boxfusion.VehicleBooking.Domain.Notifications;
using Boxfusion.VehicleBooking.Domain.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Shesha;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Services
{

    [AbpAuthorize]
    public class BookingsAppService : SheshaAppServiceBase
    {
        private readonly IRepository<Booking, Guid> _bookingRepository;
        private readonly IRepository<Vehicle, Guid> _vehicleRepository;
        private readonly IRepository<Customer, Guid> _customerRepository;
        private readonly IBookingNotificationSender _bookingNotifications;

        public BookingsAppService(
            IRepository<Booking, Guid> bookingRepository,
            IRepository<Vehicle, Guid> vehicleRepository,
            IRepository<Customer, Guid> customerRepository,
            IBookingNotificationSender bookingNotifications)
        {
            _bookingRepository = bookingRepository;
            _vehicleRepository = vehicleRepository;
            _customerRepository = customerRepository;
            _bookingNotifications = bookingNotifications;
        }

        [HttpPost]
        public async Task<Guid> CreateBooking(BookingRequest bookingRequest)
        {
            BookingRules.ValidateBookingPeriod(bookingRequest.StartDate, bookingRequest.EndDate, DateTime.Now);

            var vehicle = await _vehicleRepository.GetAsync(bookingRequest.VehicleId);

            if (vehicle.Status != RefListVehicleStatuses.Active)
                throw new UserFriendlyException("This vehicle is not available for booking.");

            if (vehicle.DailyRate == null)
                throw new UserFriendlyException("This vehicle has no daily rate set.");

            Customer customer;

            if (await IsBookingAdminAsync())
            {
                // Admin: must choose which customer this booking is for
                if (!bookingRequest.CustomerId.HasValue)
                    throw new UserFriendlyException("Please choose a customer for this booking.");

                customer = await _customerRepository.GetAsync(bookingRequest.CustomerId.Value);
            }
            else
            {
                // Customer: always book for yourself — ignore any customerId that was sent
                customer = await GetCustomerInfoAsync();
            }

            if (!await IsVehicleAvailableAsync(bookingRequest.VehicleId, bookingRequest.StartDate, bookingRequest.EndDate))
                throw new UserFriendlyException("This vehicle is already booked for that time.");

            var booking = new Booking
            {
                IsBooked = true,
                Vehicle = vehicle,
                Customer = customer,
                StartDate = bookingRequest.StartDate,
                EndDate = bookingRequest.EndDate,
                Notes = bookingRequest.Notes
            };

            booking.TotalCosts = booking.CalculateTotalCosts();

            await _bookingRepository.InsertAsync(booking);
            await CurrentUnitOfWork.SaveChangesAsync();

            await TryNotifyAsync(() => _bookingNotifications.NotifyBookingConfirmedAsync(booking), booking);

            return booking.Id;
        }

        [HttpPost]
        public async Task<String> CancelBooking(Guid bookingId)
        {
            var booking = await _bookingRepository.GetAsync(bookingId);

            
            var isAdmin = await IsBookingAdminAsync();
            Guid? callerCustomerId = isAdmin ? null : (await GetCustomerInfoAsync()).Id;
            BookingRules.EnsureCanCancel(booking, isAdmin, callerCustomerId, DateTime.Now);

            booking.IsBooked = false;
            booking.CancelledDate = DateTime.Now;

            await _bookingRepository.UpdateAsync(booking);

            await TryNotifyAsync(() => _bookingNotifications.NotifyBookingCancelledAsync(booking), booking);

            return "Booking successfully cancelled.";
        }

        // A failed notification must never undo a booking or a cancellation, so log it and carry on.
        private async Task TryNotifyAsync(Func<Task> notify, Booking booking)
        {
            try
            {
                await notify();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not send notification for booking {booking.Id}: {ex.Message}", ex);
            }
        }



        private async Task<bool> IsVehicleAvailableAsync(Guid vehicleId, DateTime start, DateTime end)
        {
            var clashes = await _bookingRepository.GetAll()
                .Where(b => b.Vehicle.Id == vehicleId)
                .Where(BookingRules.ActiveOverlap(start, end))
                .CountAsync();

            return clashes == 0;
        }

        private async Task<bool> IsBookingAdminAsync()
        {
            return await PermissionChecker.IsGrantedAsync(VehicleBookingPermissions.ManageBookings);
        }


        private async Task<Customer> GetCustomerInfoAsync()
        {
            var userId = AbpSession.UserId;
            if (!userId.HasValue)
                throw new UserFriendlyException("The account is not logged in.");

            var customer = await _customerRepository
                .GetAll()
                .FirstOrDefaultAsync(c => c.User != null && c.User.Id == userId.Value);

            if (customer == null)
                throw new UserFriendlyException("The customer profile couldn't be retrieved.");

            return customer;
        }
    }
}