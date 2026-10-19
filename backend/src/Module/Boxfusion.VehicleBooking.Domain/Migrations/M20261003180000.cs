using FluentMigrator;
using Shesha.FluentMigrator;
using System;


[Migration(20261003180000)]
public class M20261003180000 : Migration
{
    public override void Up()
    {
        Create.Table("VehicleBooking_Vehicles")
            .WithIdAsGuid()
            .WithFullAuditColumns()
            .WithColumn("RegistrationNumber").AsString().Nullable()
            .WithColumn("Make").AsString().Nullable()
            .WithColumn("Model").AsString().Nullable()
            .WithColumn("Year").AsInt32().Nullable()
            .WithColumn("VehicleTypeLkp").AsInt64().Nullable()
            .WithColumn("StatusLkp").AsInt64().Nullable()
            .WithColumn("DailyRate").AsDecimal(18, 2).Nullable()
            .WithColumn("PhotoId").AsGuid().Nullable()
    .ForeignKey("FK_VehicleBooking_Vehicles_PhotoId", "frwk", "stored_files", "id");
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }

}