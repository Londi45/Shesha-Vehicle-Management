using Abp.Domain.Entities.Auditing;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Shesha.Domain.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Domain
{


    public class Booking : FullAuditedEntity<Guid>
    {
        public virtual bool IsBooked { set; get; }
        public virtual Vehicle Vehicle { set; get; }

        public virtual Customer Customer { set; get; }

        public virtual DateTime StartDate { set; get; }

        public virtual DateTime EndDate { set; get; }

        public virtual decimal TotalCosts { get; set; }

        public virtual DateTime? CancelledDate { get; set; }
        public virtual string Notes { get; set; }

        /// <summary>When the "starts soon" reminder was sent; null = not sent yet (so it is only sent once).</summary>
        public virtual DateTime? ReminderSentDate { get; set; }


        public virtual decimal CalculateTotalCosts()
        {
            if (Vehicle == null) throw new InvalidOperationException("Vehicle is not set.");
            if (Vehicle.DailyRate == null) throw new InvalidOperationException("Vehicle has no daily rate.");

            return BookingRules.CalculateCost(StartDate, EndDate, Vehicle.DailyRate.Value);
        }



    }
}
