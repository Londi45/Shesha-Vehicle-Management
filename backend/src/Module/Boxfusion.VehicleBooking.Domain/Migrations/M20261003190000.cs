using FluentMigrator;
using Shesha.FluentMigrator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boxfusion.VehicleBooking.Domain.Migrations
{
    [Migration(20261003190000)]
    public class M20261003190000 : Migration
    {
        public override void Up()
        {
            Alter.Table("Core_Sites")
                .AddColumn("VehicleBooking_BranchCode").AsString().Nullable();

            Alter.Table("VehicleBooking_Vehicles")
                .AddForeignKeyColumn("BranchId", "Core_Sites").Nullable();

         
        }

        public override void Down() => throw new NotImplementedException();
    }

}
