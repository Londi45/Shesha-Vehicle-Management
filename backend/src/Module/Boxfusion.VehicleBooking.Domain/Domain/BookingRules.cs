using Abp.Authorization;
using Abp.UI;
using System;
using System.Linq.Expressions;

namespace Boxfusion.VehicleBooking.Domain.Domain
{
    /// <summary>
    /// The booking business rules in one place, so the services and the unit tests use the exact same logic.
    /// Everything here is pure: "now" is passed in, nothing touches the database.
    /// </summary>
    public static class BookingRules
    {
        /// <summary>Rules for a new booking's period. Throws a friendly error when a rule is broken.</summary>
        public static void ValidateBookingPeriod(DateTime start, DateTime end, DateTime now)
        {
            if (start <= now || end <= now)
                throw new UserFriendlyException("Start Date and End Date must be in the future.");

            if (start >= end)
                throw new UserFriendlyException("The End Date must be after the Start Date.");

            if ((end - start).TotalDays < 1)
                throw new UserFriendlyException("A booking must be at least one full day.");
        }

        /// <summary>Price for a booking: whole days x daily rate (a part day is not charged).</summary>
        public static decimal CalculateCost(DateTime start, DateTime end, decimal dailyRate)
        {
            return (end - start).Days * dailyRate;
        }

        /// <summary>
        /// "Active booking that overlaps [start, end)". Two periods overlap when each starts before the other ends,
        /// so back-to-back bookings (one ends exactly when the next starts) do NOT overlap.
        /// Used inside database queries, and compiled to a plain function in the unit tests.
        /// </summary>
        public static Expression<Func<Booking, bool>> ActiveOverlap(DateTime start, DateTime end)
        {
            return b => b.IsBooked && b.StartDate < end && start < b.EndDate;
        }

        /// <summary>
        /// Search period: dates are optional (both or neither). Without dates the search means "free right now".
        /// </summary>
        public static (bool HasDates, DateTime Start, DateTime End) ResolveSearchPeriod(DateTime? start, DateTime? end, DateTime now)
        {
            if (start.HasValue != end.HasValue)
                throw new UserFriendlyException("Please choose both a start and an end date, or neither.");

            if (!start.HasValue)
                return (false, now, now);

            if (start.Value < now)
                throw new UserFriendlyException("Start Date cannot be in the past.");

            if (start.Value >= end.Value)
                throw new UserFriendlyException("Start Date cannot be after End Date.");

            return (true, start.Value, end.Value);
        }

        /// <summary>Estimated price shown in search results; only possible when dates were chosen.</summary>
        public static decimal? EstimateTotal(decimal? dailyRate, bool hasDates, DateTime start, DateTime end)
        {
            if (!hasDates || dailyRate == null)
                return null;

            return dailyRate * Math.Max(1, (end - start).Days);
        }

        /// <summary>
        /// Who may cancel: admins any booking, customers only their own; and only while it is still booked and has not started.
        /// The ownership check comes first so strangers learn nothing about someone else's booking.
        /// </summary>
        public static void EnsureCanCancel(Booking booking, bool isAdmin, Guid? callerCustomerId, DateTime now)
        {
            if (!isAdmin && booking.Customer?.Id != callerCustomerId)
                throw new AbpAuthorizationException("You can only cancel your own bookings.");

            if (!booking.IsBooked)
                throw new UserFriendlyException("This booking is already cancelled.");

            if (booking.StartDate <= now)
                throw new UserFriendlyException("You can only cancel a booking before it starts.");
        }

        /// <summary>Bookings that need their "starts soon" reminder: active, not reminded yet, starting within the next 24 hours.</summary>
        public static Expression<Func<Booking, bool>> ReminderDue(DateTime now)
        {
            var windowEnd = now.AddHours(24);
            return b => b.IsBooked
                     && b.ReminderSentDate == null
                     && b.StartDate > now
                     && b.StartDate <= windowEnd;
        }
    }
}
