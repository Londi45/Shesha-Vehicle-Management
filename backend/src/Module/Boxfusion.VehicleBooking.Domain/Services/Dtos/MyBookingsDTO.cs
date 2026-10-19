using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Services.Dtos
{
    public class MyBookingsDTO
    {
        public Guid Id { get; set; }
        public virtual string Make { get; set; } = string.Empty;
        public virtual string Model { get; set; } = string.Empty;

        public virtual string RegistrationNumber { get; set; } = string.Empty;

        public virtual string DisplayName => $"{RegistrationNumber} - {Make} {Model}";

        public virtual RefListVehicleTypes? VehicleType { get; set; }
        public virtual string VehicleTypeName { get; set; } = string.Empty;

        public virtual Branch Branch { get; set; }

        public virtual Decimal TotalCosts { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsBooked { get; set; }
        public bool CanCancel { get; set; }

        public virtual string Status { get; set; }

    }
}
