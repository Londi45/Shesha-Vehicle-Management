using Shesha.Domain.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Domain.Enums
{

    [ReferenceList("VehicleType")]
    public enum RefListVehicleTypes
    {
        [Description("Sedan")]
        Sedan = 1,

        [Description("Hatchback")]
        Hatchback = 2,

        [Description("SUV")]
        Suv = 3,

        [Description("Bakkie")]
        Bakkie = 4,

        [Description("Van")]
        Van = 5



    }
}
