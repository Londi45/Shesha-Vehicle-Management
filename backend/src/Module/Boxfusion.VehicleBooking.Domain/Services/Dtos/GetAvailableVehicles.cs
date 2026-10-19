using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Services.Dtos
{
    public class GetAvailableVehicles
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? BranchId { get; set; }
        public RefListVehicleTypes? VehicleType { get; set; }




    }
}
