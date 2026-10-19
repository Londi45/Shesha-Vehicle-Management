using Abp.Domain.Entities.Auditing;
using Boxfusion.VehicleBooking.Domain.Domain.Enums;
using EasyNetQ;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Domain
{
    [Entity(TypeShortAlias = "VehicleBooking.Vehicle")]
    public class Vehicle : FullAuditedEntity<Guid>
    {
        public virtual string RegistrationNumber { get; set; }
        public virtual string Make { get; set; }
        public virtual string Model { get; set; }
        public virtual int? Year { get; set; }
        public virtual RefListVehicleTypes? VehicleType { get; set; }
        public virtual RefListVehicleStatuses? Status { get; set; }
        [NotNull]
        public virtual decimal? DailyRate { get; set; }
        public virtual StoredFile Photo { get; set; }

        public virtual Branch Branch { get; set; }

        [NotMapped]
        public virtual string VehicleModelNumber => $"{Model} {Year}";

        [NotMapped]
        [EntityDisplayName]
        public virtual string DisplayName => $"{RegistrationNumber} - {Make} {Model}";
    }
}
