using Shesha.Domain.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Domain.Enums
{
    [ReferenceList( "VehicleStatuses")]
    public enum RefListVehicleStatuses : long
    {
        [Description("Active")]
        Active = 1,

        [Description("In maintenance")]
        InMaintenance = 2,

        [Description("Retired")]
        Retired = 3
    }
}
