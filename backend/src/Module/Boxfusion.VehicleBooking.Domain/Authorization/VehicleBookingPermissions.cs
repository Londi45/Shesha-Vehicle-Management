using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Authorization
{
    public class VehicleBookingPermissions
    {
        public const string ManageBookings = "app:ManageBookings";

        /// <summary>Customer-only pages (My Bookings, My Profile). Held by the "booking-customer" role.</summary>
        public const string CustomerArea = "app:CustomerArea";

        /// <summary>Name of the role every customer gets (also given automatically on sign-up).</summary>
        public const string CustomerRoleName = "booking-customer";
    }
}
