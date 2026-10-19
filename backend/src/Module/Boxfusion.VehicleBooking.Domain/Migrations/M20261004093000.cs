using FluentMigrator;
using System;

namespace Boxfusion.VehicleBooking.Domain.Migrations
{
    [Migration(20261004093000)]
    public class M20261004093000 : Migration
    {
        public override void Up()
        {
            Rename.Column("DriversLicenceNumber").OnTable("Core_Persons")
                .To("VehicleBooking_DriversLicenceNumber");

            Rename.Column("LicenceExpiryDate").OnTable("Core_Persons")
                .To("VehicleBooking_LicenceExpiryDate");

            // make sure both allow empty values
            Alter.Column("VehicleBooking_DriversLicenceNumber").OnTable("Core_Persons")
                .AsString().Nullable();
            Alter.Column("VehicleBooking_LicenceExpiryDate").OnTable("Core_Persons")
                .AsDateTime().Nullable();
        }

        public override void Down() => throw new NotImplementedException();
    }
}