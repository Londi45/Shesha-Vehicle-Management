using Abp.Authorization;
using Abp.UI;
using Boxfusion.VehicleBooking.Domain.Domain;
using Shouldly;
using System;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Unit
{
    /// <summary>
    /// Unit tests for the booking business rules. No database, no backend: "now" is a fixed date,
    /// so the results never depend on when the tests run.
    /// </summary>
    public class BookingRulesTests
    {
        private static readonly DateTime Now = new DateTime(2030, 1, 10, 12, 0, 0);

        private static Booking BookingFor(Guid customerId, DateTime start, DateTime end, bool isBooked = true) => new Booking
        {
            Customer = new Customer { Id = customerId },
            StartDate = start,
            EndDate = end,
            IsBooked = isBooked
        };

        // ---------- New booking period ----------

        [Fact]
        public void ValidateBookingPeriod_future_period_of_two_days_is_accepted()
        {
            Should.NotThrow(() => BookingRules.ValidateBookingPeriod(Now.AddDays(1), Now.AddDays(3), Now));
        }

        [Fact]
        public void ValidateBookingPeriod_exactly_one_day_is_accepted()
        {
            Should.NotThrow(() => BookingRules.ValidateBookingPeriod(Now.AddDays(1), Now.AddDays(2), Now));
        }

        [Fact]
        public void ValidateBookingPeriod_start_in_the_past_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ValidateBookingPeriod(Now.AddDays(-1), Now.AddDays(2), Now))
                .Message.ShouldContain("must be in the future");
        }

        [Fact]
        public void ValidateBookingPeriod_end_before_start_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ValidateBookingPeriod(Now.AddDays(3), Now.AddDays(2), Now))
                .Message.ShouldContain("End Date must be after the Start Date");
        }

        [Fact]
        public void ValidateBookingPeriod_less_than_one_day_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ValidateBookingPeriod(Now.AddDays(1), Now.AddDays(1).AddHours(5), Now))
                .Message.ShouldContain("at least one full day");
        }

        // ---------- Cost ----------

        [Fact]
        public void CalculateCost_is_whole_days_times_daily_rate()
        {
            BookingRules.CalculateCost(Now, Now.AddDays(3), 500m).ShouldBe(1500m);
        }

        [Fact]
        public void CalculateCost_does_not_charge_a_part_day()
        {
            // 1.5 days -> 1 charged day. Documents current behaviour (see polish list).
            BookingRules.CalculateCost(Now, Now.AddHours(36), 500m).ShouldBe(500m);
        }

        [Fact]
        public void Booking_CalculateTotalCosts_uses_the_vehicle_daily_rate()
        {
            var booking = new Booking { StartDate = Now, EndDate = Now.AddDays(2), Vehicle = new Vehicle { DailyRate = 950m } };
            booking.CalculateTotalCosts().ShouldBe(1900m);
        }

        [Fact]
        public void Booking_CalculateTotalCosts_without_a_vehicle_throws()
        {
            Should.Throw<InvalidOperationException>(() => new Booking { StartDate = Now, EndDate = Now.AddDays(2) }.CalculateTotalCosts());
        }

        // ---------- Double-booking (overlap) ----------

        [Theory]
        [InlineData(1, 5, 3, 7, true)]   // partly overlaps
        [InlineData(1, 10, 3, 5, true)]  // new booking inside an existing one
        [InlineData(3, 5, 1, 10, true)]  // existing booking inside the new one
        [InlineData(1, 3, 3, 5, false)]  // back-to-back: one ends exactly when the other starts
        [InlineData(1, 3, 5, 7, false)]  // completely before
        public void ActiveOverlap_detects_clashing_periods(int existingStart, int existingEnd, int newStart, int newEnd, bool expected)
        {
            var existing = BookingFor(Guid.NewGuid(), Now.AddDays(existingStart), Now.AddDays(existingEnd));
            var overlaps = BookingRules.ActiveOverlap(Now.AddDays(newStart), Now.AddDays(newEnd)).Compile();

            overlaps(existing).ShouldBe(expected);
        }

        [Fact]
        public void ActiveOverlap_ignores_cancelled_bookings()
        {
            var cancelled = BookingFor(Guid.NewGuid(), Now.AddDays(1), Now.AddDays(5), isBooked: false);
            BookingRules.ActiveOverlap(Now.AddDays(2), Now.AddDays(3)).Compile()(cancelled).ShouldBeFalse();
        }

        // ---------- Search period ----------

        [Fact]
        public void ResolveSearchPeriod_without_dates_means_right_now()
        {
            var (hasDates, start, end) = BookingRules.ResolveSearchPeriod(null, null, Now);

            hasDates.ShouldBeFalse();
            start.ShouldBe(Now);
            end.ShouldBe(Now);
        }

        [Fact]
        public void ResolveSearchPeriod_with_valid_dates_keeps_them()
        {
            var (hasDates, start, end) = BookingRules.ResolveSearchPeriod(Now.AddDays(1), Now.AddDays(4), Now);

            hasDates.ShouldBeTrue();
            start.ShouldBe(Now.AddDays(1));
            end.ShouldBe(Now.AddDays(4));
        }

        [Fact]
        public void ResolveSearchPeriod_with_only_a_start_date_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ResolveSearchPeriod(Now.AddDays(1), null, Now))
                .Message.ShouldContain("both a start and an end date");
        }

        [Fact]
        public void ResolveSearchPeriod_with_only_an_end_date_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ResolveSearchPeriod(null, Now.AddDays(1), Now));
        }

        [Fact]
        public void ResolveSearchPeriod_with_start_in_the_past_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ResolveSearchPeriod(Now.AddDays(-1), Now.AddDays(2), Now))
                .Message.ShouldContain("cannot be in the past");
        }

        [Fact]
        public void ResolveSearchPeriod_with_start_after_end_is_rejected()
        {
            Should.Throw<UserFriendlyException>(() => BookingRules.ResolveSearchPeriod(Now.AddDays(3), Now.AddDays(2), Now));
        }

        // ---------- Estimated total ----------

        [Fact]
        public void EstimateTotal_is_empty_without_dates()
        {
            BookingRules.EstimateTotal(500m, hasDates: false, Now, Now).ShouldBeNull();
        }

        [Fact]
        public void EstimateTotal_is_days_times_rate_with_dates()
        {
            BookingRules.EstimateTotal(100m, hasDates: true, Now, Now.AddDays(2)).ShouldBe(200m);
        }

        [Fact]
        public void EstimateTotal_charges_at_least_one_day()
        {
            BookingRules.EstimateTotal(100m, hasDates: true, Now, Now.AddHours(5)).ShouldBe(100m);
        }

        // ---------- Cancelling ----------

        [Fact]
        public void EnsureCanCancel_customer_can_cancel_own_future_booking()
        {
            var me = Guid.NewGuid();
            Should.NotThrow(() => BookingRules.EnsureCanCancel(BookingFor(me, Now.AddDays(2), Now.AddDays(4)), isAdmin: false, me, Now));
        }

        [Fact]
        public void EnsureCanCancel_customer_cannot_cancel_someone_elses_booking()
        {
            var someoneElse = BookingFor(Guid.NewGuid(), Now.AddDays(2), Now.AddDays(4));
            Should.Throw<AbpAuthorizationException>(() => BookingRules.EnsureCanCancel(someoneElse, isAdmin: false, Guid.NewGuid(), Now));
        }

        [Fact]
        public void EnsureCanCancel_admin_can_cancel_anyones_booking()
        {
            var someoneElse = BookingFor(Guid.NewGuid(), Now.AddDays(2), Now.AddDays(4));
            Should.NotThrow(() => BookingRules.EnsureCanCancel(someoneElse, isAdmin: true, callerCustomerId: null, Now));
        }

        [Fact]
        public void EnsureCanCancel_already_cancelled_booking_is_rejected()
        {
            var me = Guid.NewGuid();
            Should.Throw<UserFriendlyException>(() => BookingRules.EnsureCanCancel(BookingFor(me, Now.AddDays(2), Now.AddDays(4), isBooked: false), false, me, Now))
                .Message.ShouldContain("already cancelled");
        }

        [Fact]
        public void EnsureCanCancel_booking_that_has_started_is_rejected()
        {
            var me = Guid.NewGuid();
            Should.Throw<UserFriendlyException>(() => BookingRules.EnsureCanCancel(BookingFor(me, Now.AddHours(-1), Now.AddDays(2)), false, me, Now))
                .Message.ShouldContain("before it starts");
        }

        [Fact]
        public void EnsureCanCancel_checks_ownership_before_revealing_anything_else()
        {
            // Someone else's booking that is ALSO already cancelled: a stranger must get "not yours", not "already cancelled".
            var someoneElsesCancelled = BookingFor(Guid.NewGuid(), Now.AddDays(2), Now.AddDays(4), isBooked: false);
            Should.Throw<AbpAuthorizationException>(() => BookingRules.EnsureCanCancel(someoneElsesCancelled, false, Guid.NewGuid(), Now));
        }

        // ---------- Reminder window ----------

        [Theory]
        [InlineData(12, true, false, true)]   // starts in 12h, booked, not reminded -> due
        [InlineData(24, true, false, true)]   // exactly 24h -> due
        [InlineData(30, true, false, false)]  // starts in 30h -> too early
        [InlineData(-1, true, false, false)]  // already started -> not due
        [InlineData(12, false, false, false)] // cancelled -> not due
        [InlineData(12, true, true, false)]   // already reminded -> never twice
        public void ReminderDue_only_for_active_unreminded_bookings_starting_within_24_hours(int startsInHours, bool isBooked, bool alreadyReminded, bool expected)
        {
            var booking = BookingFor(Guid.NewGuid(), Now.AddHours(startsInHours), Now.AddHours(startsInHours + 48), isBooked);
            booking.ReminderSentDate = alreadyReminded ? Now.AddHours(-2) : null;

            BookingRules.ReminderDue(Now).Compile()(booking).ShouldBe(expected);
        }
    }
}
