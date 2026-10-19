using NetTopologySuite.Algorithm;
using Shesha.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Domain
{
    public class Branch : Site
    {
        public virtual string BranchCode { get; set; }

    }
}
