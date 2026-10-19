using FluentMigrator;
using System;

namespace Boxfusion.VehicleBooking.Domain.Migrations
{
    [Migration(20261004011100)]
    public class M20261004011100 : Migration
    {
        public override void Up()
        {
            Alter.Table("Core_Persons").AddColumn("DriversLicenceNumber").AsAnsiString().Nullable()
                .AddColumn("LicenceExpiryDate").AsDateTime().Nullable();

        }

        public override void Down() => throw new NotImplementedException();
       

    }
}
