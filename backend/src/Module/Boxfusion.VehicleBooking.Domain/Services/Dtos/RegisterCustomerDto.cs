using System;

namespace Boxfusion.VehicleBooking.Domain.Services.Dtos
{
    public class RegisterCustomerDto
    {
        // Login details
        public string EmailAddress { get; set; }        // also used as the username
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        // Customer details
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
        public string DriversLicenceNumber { get; set; }
        public DateTime? LicenceExpiryDate { get; set; }
    }
}