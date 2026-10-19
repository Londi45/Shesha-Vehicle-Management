using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Integration
{
    /// <summary>Booking, My Bookings, cancelling and searching - the main customer journey, end to end.</summary>
    [Trait("Category", "Integration")]
    public class BookingAndSearchTests : IClassFixture<BookingTestFixture>
    {
        private readonly BookingTestFixture _fx;
        public BookingAndSearchTests(BookingTestFixture fx) => _fx = fx;

        private const string Search = "/api/services/app/Vehicles/GetAvailableVehiclesPaged";

        [IntegrationFact]
        public async Task Customer_books_a_car_sees_it_in_my_bookings_and_can_cancel_it()
        {
            var start = _fx.Start.AddDays(10);
            var vehicleId = await _fx.FindFreeVehicleAsync(_fx.CustomerA.Api, start, start.AddDays(2));
            var bookingId = await _fx.CreateBookingAsync(_fx.CustomerA, vehicleId, start, start.AddDays(2));

            var mine = await _fx.CustomerA.Api.GetAsync("/api/services/app/Customer/GetTCustomersBookings?past=false&maxResultCount=100");
            mine.Success.ShouldBeTrue(mine.ToString());
            mine.Result["items"].AsArray().Select(i => i["id"].GetValue<string>()).ShouldContain(bookingId.ToString());

            var cancel = await _fx.CustomerA.Api.PostAsync($"/api/services/app/Bookings/CancelBooking?bookingId={bookingId}");
            cancel.Success.ShouldBeTrue(cancel.ToString());
        }

        [IntegrationFact]
        public async Task Customer_cannot_cancel_someone_elses_booking()
        {
            var start = _fx.Start.AddDays(20);
            var vehicleId = await _fx.FindFreeVehicleAsync(_fx.CustomerA.Api, start, start.AddDays(2));
            var alicesBooking = await _fx.CreateBookingAsync(_fx.CustomerA, vehicleId, start, start.AddDays(2));

            var bobTries = await _fx.CustomerB.Api.PostAsync($"/api/services/app/Bookings/CancelBooking?bookingId={alicesBooking}");
            bobTries.Status.ShouldBe(HttpStatusCode.Forbidden, bobTries.ToString());

            // tidy up
            (await _fx.CustomerA.Api.PostAsync($"/api/services/app/Bookings/CancelBooking?bookingId={alicesBooking}")).Success.ShouldBeTrue();
        }

        [IntegrationFact]
        public async Task The_same_car_cannot_be_double_booked_and_disappears_from_search()
        {
            var start = _fx.Start.AddDays(30);
            var end = start.AddDays(3);
            var vehicleId = await _fx.FindFreeVehicleAsync(_fx.CustomerA.Api, start, end);
            var bookingId = await _fx.CreateBookingAsync(_fx.CustomerA, vehicleId, start, end);

            // overlapping booking by someone else is refused
            var clash = await _fx.CustomerB.Api.PostAsync("/api/services/app/Bookings/CreateBooking", new
            {
                vehicleId,
                startDate = BookingTestFixture.Iso(start.AddDays(1)),
                endDate = BookingTestFixture.Iso(end.AddDays(1))
            });
            clash.Success.ShouldBeFalse();
            clash.ErrorMessage.ShouldContain("already booked");

            // and the car is no longer offered for those dates
            var search = await _fx.CustomerB.Api.GetAsync($"{Search}?startDate={BookingTestFixture.Iso(start)}&endDate={BookingTestFixture.Iso(end)}");
            search.Result["items"].AsArray().Select(i => i["id"].GetValue<string>()).ShouldNotContain(vehicleId.ToString());

            (await _fx.CustomerA.Api.PostAsync($"/api/services/app/Bookings/CancelBooking?bookingId={bookingId}")).Success.ShouldBeTrue();
        }

        [IntegrationFact]
        public async Task Customer_always_books_for_themselves_even_if_another_customer_id_is_sent()
        {
            var start = _fx.Start.AddDays(40);
            var vehicleId = await _fx.FindFreeVehicleAsync(_fx.CustomerA.Api, start, start.AddDays(2));

            var created = await _fx.CustomerA.Api.PostAsync("/api/services/app/Bookings/CreateBooking", new
            {
                vehicleId,
                customerId = Guid.NewGuid(),          // someone else - must be ignored
                startDate = BookingTestFixture.Iso(start),
                endDate = BookingTestFixture.Iso(start.AddDays(2))
            });
            created.Success.ShouldBeTrue(created.ToString());
            var bookingId = created.Result.GetValue<string>();

            var mine = await _fx.CustomerA.Api.GetAsync("/api/services/app/Customer/GetTCustomersBookings?past=false&maxResultCount=100");
            mine.Result["items"].AsArray().Select(i => i["id"].GetValue<string>()).ShouldContain(bookingId);

            (await _fx.CustomerA.Api.PostAsync($"/api/services/app/Bookings/CancelBooking?bookingId={bookingId}")).Success.ShouldBeTrue();
        }

        [IntegrationFact]
        public async Task Booking_in_the_past_is_rejected()
        {
            var vehicleId = await _fx.FindFreeVehicleAsync(_fx.CustomerA.Api, _fx.Start, _fx.End);
            var result = await _fx.CustomerA.Api.PostAsync("/api/services/app/Bookings/CreateBooking", new
            {
                vehicleId,
                startDate = BookingTestFixture.Iso(DateTime.Now.AddDays(-3)),
                endDate = BookingTestFixture.Iso(DateTime.Now.AddDays(-1))
            });
            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("must be in the future");
        }

        [IntegrationFact]
        public async Task Search_without_filters_returns_available_cars()
        {
            var result = await _fx.CustomerA.Api.GetAsync(Search);
            result.Success.ShouldBeTrue(result.ToString());
            result.Result["items"].AsArray().Count.ShouldBeGreaterThan(0);
            // without dates there is no price estimate
            result.Result["items"][0]["estimatedTotal"].ShouldBeNull();
        }

        [IntegrationFact]
        public async Task Search_by_vehicle_type_returns_only_that_type()
        {
            var all = await _fx.CustomerA.Api.GetAsync(Search);
            var type = all.Result["items"][0]["vehicleType"].GetValue<int>();

            var filtered = await _fx.CustomerA.Api.GetAsync($"{Search}?vehicleType={type}");

            filtered.Result["items"].AsArray().ShouldAllBe(i => i["vehicleType"].GetValue<int>() == type);
        }

        [IntegrationFact]
        public async Task Search_by_branch_returns_only_that_branch()
        {
            var all = await _fx.CustomerA.Api.GetAsync(Search);
            var withBranch = all.Result["items"].AsArray().FirstOrDefault(i => i["branchName"] != null);
            if (withBranch == null) return; // no vehicles linked to a branch in this database

            var branchName = withBranch["branchName"].GetValue<string>();
            var branches = await _fx.CustomerA.Api.GetAsync("/api/dynamic/Boxfusion.VehicleBooking/Branch/Crud/GetAll?properties=id%20name&maxResultCount=100");
            var branchId = branches.Result["items"].AsArray().First(b => b["name"].GetValue<string>() == branchName)["id"].GetValue<string>();

            var filtered = await _fx.CustomerA.Api.GetAsync($"{Search}?branchId={branchId}");

            filtered.Result["items"].AsArray().Count.ShouldBeGreaterThan(0);
            filtered.Result["items"].AsArray().ShouldAllBe(i => i["branchName"].GetValue<string>() == branchName);
        }

        [IntegrationFact]
        public async Task Search_with_only_one_date_is_rejected()
        {
            var result = await _fx.CustomerA.Api.GetAsync($"{Search}?startDate={BookingTestFixture.Iso(_fx.Start)}");
            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("both a start and an end date");
        }
    }
}
