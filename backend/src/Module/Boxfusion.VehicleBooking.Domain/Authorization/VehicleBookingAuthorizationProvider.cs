using System;
using System.Collections.Generic;
using System.Text;
using Abp.Authorization;
using Abp.Localization;


namespace Boxfusion.VehicleBooking.Domain.Authorization
{
    public class VehicleBookingAuthorizationProvider : AuthorizationProvider
    {

        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            context.CreatePermission(VehicleBookingPermissions.ManageBookings, new FixedLocalizableString("Manage bookings"));
            context.CreatePermission(VehicleBookingPermissions.CustomerArea, new FixedLocalizableString("Customer area"));
        }


    }
}
