using FluentMigrator;
using System;

namespace Boxfusion.VehicleBooking.Domain.Migrations
{
    /// <summary>
    /// Remembers when a booking's reminder was sent, so the reminder job never sends it twice.
    /// </summary>
    [Migration(20261012150000)]
    public class M20261012150000 : Migration
    {
        public override void Up()
        {
            Alter.Table("VehicleBooking_Bookings")
                .AddColumn("ReminderSentDate").AsDateTime().Nullable();
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}
