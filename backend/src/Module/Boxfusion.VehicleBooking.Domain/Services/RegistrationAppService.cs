using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Boxfusion.VehicleBooking.Domain.Authorization;
using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Shesha;
using Shesha.Authorization.Users;
using Shesha.Domain;
using System;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Services
{
    /// <summary>
    /// Public sign-up: creates a login and a linked Customer in one transaction.
    /// Open to anonymous callers, so it only ever creates plain customers: the one role given is the fixed customer role.
    /// </summary>
    [AbpAllowAnonymous]
    public class RegistrationAppService : SheshaAppServiceBase
    {
        private readonly UserManager _userManager;
        private readonly IRepository<Customer, Guid> _customerRepository;
        private readonly IRepository<ShaRole, Guid> _roleRepository;
        private readonly IRepository<ShaRoleAppointedPerson, Guid> _roleAppointmentRepository;

        public RegistrationAppService(
            UserManager userManager,
            IRepository<Customer, Guid> customerRepository,
            IRepository<ShaRole, Guid> roleRepository,
            IRepository<ShaRoleAppointedPerson, Guid> roleAppointmentRepository)
        {
            _userManager = userManager;
            _customerRepository = customerRepository;
            _roleRepository = roleRepository;
            _roleAppointmentRepository = roleAppointmentRepository;
        }

        [HttpPost]
        public async Task<Guid> Register(RegisterCustomerDto input)
        {
            // 1. Check the input
            var email = input.EmailAddress?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                throw new UserFriendlyException("Email address is required.");
            if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName))
                throw new UserFriendlyException("First name and last name are required.");

            // 2. One account per email
            if (await _userManager.FindByNameAsync(email) != null)
                throw new UserFriendlyException("An account with this email address already exists.");

            // 3. Create the login (Shesha hashes the password and checks the password rules)
            var user = await _userManager.CreateUserAsync(
                email,                  // username
                true,                   // create a local password
                input.Password,
                input.ConfirmPassword,
                input.FirstName.Trim(),
                input.LastName.Trim(),
                input.MobileNumber?.Trim(),
                email,                  // email address
                false,                  // don't force a password change
                null);                  // default password-reset methods

            // 4. Create the customer and link it to the login
            var customer = new Customer
            {
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                EmailAddress1 = email,
                MobileNumber1 = input.MobileNumber?.Trim(),
                DriversLicenceNumber = input.DriversLicenceNumber?.Trim(),
                LicenceExpiryDate = input.LicenceExpiryDate,
                User = user
            };

            await _customerRepository.InsertAsync(customer);
            await CurrentUnitOfWork.SaveChangesAsync();

            // 5. Give the new customer the customer role (unlocks My Bookings / My Profile).
            //    Skipped quietly if the role hasn't been set up yet - sign-up must still work.
            var customerRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == VehicleBookingPermissions.CustomerRoleName);
            if (customerRole != null)
            {
                await _roleAppointmentRepository.InsertAsync(new ShaRoleAppointedPerson { Role = customerRole, Person = customer });
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            else
            {
                Logger.Warn($"Role '{VehicleBookingPermissions.CustomerRoleName}' not found - new customer {customer.Id} was not given the customer role.");
            }

            return customer.Id;
        }
    }
}
