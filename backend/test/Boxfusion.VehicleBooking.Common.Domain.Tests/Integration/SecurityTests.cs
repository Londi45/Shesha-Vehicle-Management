using Shouldly;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Integration
{
    /// <summary>The locks we put on the API: not logged in -> 401; customers can't read everyone's data -> 403.</summary>
    [Trait("Category", "Integration")]
    public class SecurityTests : IClassFixture<BookingTestFixture>
    {
        private readonly BookingTestFixture _fx;
        public SecurityTests(BookingTestFixture fx) => _fx = fx;

        [IntegrationFact]
        public async Task Booking_api_requires_login()
        {
            var anonymous = new ApiClient();
            var result = await anonymous.PostAsync($"/api/services/app/Bookings/CancelBooking?bookingId={Guid.NewGuid()}");
            result.Status.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [IntegrationFact]
        public async Task Customer_cannot_list_all_customers()
        {
            var result = await _fx.CustomerA.Api.GetAsync("/api/dynamic/Boxfusion.VehicleBooking/Customer/Crud/GetAll");
            result.Status.ShouldBe(HttpStatusCode.Forbidden, result.ToString());
        }

        [IntegrationFact]
        public async Task Customer_cannot_list_all_bookings()
        {
            var result = await _fx.CustomerA.Api.GetAsync("/api/dynamic/Boxfusion.VehicleBooking/Booking/Crud/GetAll");
            result.Status.ShouldBe(HttpStatusCode.Forbidden, result.ToString());
        }

        [IntegrationFact]
        public async Task Customer_cannot_list_all_people_through_the_person_api()
        {
            var result = await _fx.CustomerA.Api.GetAsync("/api/dynamic/Shesha/Person/Crud/GetAll");
            result.Status.ShouldBe(HttpStatusCode.Forbidden, result.ToString());
        }
    }
}
