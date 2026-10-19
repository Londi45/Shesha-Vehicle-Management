using FluentMigrator;
using Shesha.FluentMigrator;
using System;

namespace Boxfusion.VehicleBooking.Domain.Migrations
{
    [Migration(20261004100000)]
    public class M20261004100000 : Migration
    {
        public override void Up()
        {
            Create.Table("VehicleBooking_Bookings")
                .WithIdAsGuid()
                .WithFullAuditColumns()
                .WithColumn("IsBooked").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithForeignKeyColumn("VehicleId", "VehicleBooking_Vehicles").Nullable()
                .WithForeignKeyColumn("CustomerId", "Core_Persons").Nullable()
                .WithColumn("StartDate").AsDateTime().NotNullable()
                .WithColumn("EndDate").AsDateTime().NotNullable()
                .WithColumn("TatalCosts").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("CancelledDate").AsDateTime().Nullable()
                .WithColumn("Notes").AsString().Nullable();
        }

        public override void Down() => throw new NotImplementedException();
    }
}