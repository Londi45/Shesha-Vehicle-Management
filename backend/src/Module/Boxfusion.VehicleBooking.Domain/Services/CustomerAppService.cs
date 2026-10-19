using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Services.Dtos;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NHibernate.Engine;
using NHibernate.Linq;
using Shesha;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Services
{
    [AbpAuthorize]
    public class CustomerAppService : SheshaAppServiceBase
    {
        private readonly IRepository<Customer, Guid> _customerRepository;
        private readonly IRepository<Booking, Guid> _bookingRepository;

        public CustomerAppService(IRepository<Customer, Guid> customerRepository, IRepository<Booking, Guid> bookingRepository)
        {
            _customerRepository = customerRepository;
            _bookingRepository = bookingRepository;
        }

        [HttpGet]
        public async Task<PagedResultDto<MyBookingsDTO>> GetTCustomersBookings(
            [FromQuery] bool? past = false,
            [FromQuery] int skipCount = 0,
            [FromQuery] int maxResultCount = 10)
        {
            Logger.Info($"Get Customer requests: {JsonConvert.SerializeObject(past)}");

            System.Diagnostics.Debug.WriteLine($"Get Customer requests: {JsonConvert.SerializeObject(past)} by Id {JsonConvert.SerializeObject(AbpSession.UserId)}");


            var customer = await GetCustomerInfoAsynch();
            var now = DateTime.Now;

            var query = _bookingRepository.GetAll().Where(b => b.Customer.Id == customer.Id);
            // past = history (finished or cancelled); otherwise upcoming and currently active bookings only
            query = past == true
                ? query.Where(b => !b.IsBooked || b.EndDate < now)
                : query.Where(b => b.IsBooked && b.EndDate >= now);

            var totalCount = await query.CountAsync();

            var bookings = await query
                .OrderByDescending(b => b.StartDate)
                .Skip(Math.Max(0, skipCount))
                .Take(maxResultCount > 0 ? maxResultCount : 10)
                .Fetch(b => b.Vehicle)
                .ToListAsync();

            var items = bookings.Select(b => new MyBookingsDTO
            {
                Id = b.Id,
                Make = b.Vehicle?.Make ?? string.Empty,
                Model = b.Vehicle?.Model ?? string.Empty,
                RegistrationNumber = b.Vehicle?.RegistrationNumber ?? string.Empty,
                VehicleType = b.Vehicle?.VehicleType,
                VehicleTypeName = b.Vehicle?.VehicleType?.ToString(),
                TotalCosts = b.TotalCosts,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                IsBooked = b.IsBooked,
                Status = !b.IsBooked ? "Cancelled" : (b.EndDate < now ? "Completed" : (b.StartDate > now ? "Upcoming" : "In progress")),
                CanCancel = b.IsBooked && b.StartDate > now
            }).ToList();
            Logger.Info($"Objects returned by GetTCustomersBookings: {JsonConvert.SerializeObject(items)}");

            System.Diagnostics.Debug.WriteLine($"Objects returned by GetTCustomersBookings: {Newtonsoft.Json.JsonConvert.SerializeObject(items)}");
            return new PagedResultDto<MyBookingsDTO>(totalCount, items);
        }

        private async Task<Customer> GetCustomerInfoAsynch()
        {
            var userId = AbpSession.UserId;
            if (!userId.HasValue) throw new UserFriendlyException("User is not logged in.");

            var customer = await _customerRepository.GetAll()
                .FirstOrDefaultAsync(c => c.User != null && c.User.Id == userId.Value);

            if (customer == null) throw new UserFriendlyException("No customer profile is linked to this user.");


            return customer;
        }


        [HttpGet]
        public async Task<MyProfileDto> GetProfile()
        {
            var customer = await GetCustomerInfoAsynch();

            return new MyProfileDto
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                EmailAddress = customer.EmailAddress1,
                MobileNumber = customer.MobileNumber1,
                DriversLicenceNumber = customer.DriversLicenceNumber,
                LicenceExpiryDate = customer.LicenceExpiryDate
            };


        }


        [HttpPut]
        public async Task<MyProfileDto> UpdateMyProfile(MyProfileDto input)
        {
            if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName))
                throw new UserFriendlyException("First name and last name are required.");

            var customer = await GetCustomerInfoAsynch();

            customer.FirstName = input.FirstName.Trim();
            customer.LastName = input.LastName.Trim();
            customer.EmailAddress1 = input.EmailAddress?.Trim();
            customer.MobileNumber1 = input.MobileNumber?.Trim();
            // Licence number and expiry are deliberately NOT copied: customers can view them, only staff change them.

            await _customerRepository.UpdateAsync(customer);

            return await GetProfile();
        }
    }
}
