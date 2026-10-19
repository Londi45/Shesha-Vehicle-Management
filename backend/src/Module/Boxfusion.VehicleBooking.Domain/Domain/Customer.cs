using Shesha.Domain;
using Shesha.Domain.Attributes;
using System;

namespace Boxfusion.VehicleBooking.Domain.Domain
{
    [Entity(TypeShortAlias = "VehicleBooking.Customer")]
    public class Customer : Person
    {
        public virtual string DriversLicenceNumber { get; set; }
        public virtual DateTime? LicenceExpiryDate { get; set; }
    }
}