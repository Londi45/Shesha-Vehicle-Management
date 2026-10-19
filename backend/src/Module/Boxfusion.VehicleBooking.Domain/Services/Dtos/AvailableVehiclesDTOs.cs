using Boxfusion.VehicleBooking.Domain.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Services.Dtos
{
    public class AvailableVehiclesDTOs
    {
        public Guid Id { get; set; }
        public string RegistrationNumber { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int? Year { get; set; }
        public RefListVehicleTypes? VehicleType { get; set; }
        public string BranchName { get; set; }
        public decimal? DailyRate { get; set; }
        public decimal? EstimatedTotal { get; set; }




    }
}
