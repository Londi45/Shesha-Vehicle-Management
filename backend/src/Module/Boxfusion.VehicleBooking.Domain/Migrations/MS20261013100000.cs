using FluentMigrator;
using System;

namespace Boxfusion.VehicleBooking.Domain.Migrations
{
    [Migration(20261013100000)]
    public class M20261013100000 : Migration
    {
        public override void Up()
        {
            Rename.Column("TatalCosts").OnTable("VehicleBooking_Bookings").To("TotalCosts");
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}