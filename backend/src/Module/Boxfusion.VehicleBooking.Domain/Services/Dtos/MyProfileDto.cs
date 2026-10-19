using System;

namespace Boxfusion.VehicleBooking.Domain.Services.Dtos
{
    public class MyProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string MobileNumber { get; set; }

        public string DriversLicenceNumber { get; set; }
        public DateTime? LicenceExpiryDate { get; set; }
    }
}