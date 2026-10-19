using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Integration
{
    /// <summary>A customer created through the real sign-up endpoint, logged in and ready to use.</summary>
    public sealed class TestCustomer
    {
        public const string Password = "ItTest@12345";

        public string Email { get; init; }
        public ApiClient Api { get; } = new ApiClient();

        /// <summary>Signs up a brand-new customer (emails start with "it-test-" so they are easy to clean up) and logs in.</summary>
        public static async Task<TestCustomer> CreateAsync(string firstName = "Test", string lastName = "Customer")
        {
            var email = $"it-test-{Guid.NewGuid():N}@example.com";
            var anonymous = new ApiClient();

            var registered = await anonymous.PostAsync("/api/services/app/Registration/Register", new
            {
                emailAddress = email,
                password = Password,
                confirmPassword = Password,
                firstName,
                lastName,
                mobileNumber = "0710000000",
                driversLicenceNumber = "IT-TEST-LICENCE",
                licenceExpiryDate = "2035-12-31"
            });
            registered.Success.ShouldBeTrue($"sign-up failed: {registered}");

            var customer = new TestCustomer { Email = email };
            var login = await anonymous.PostAsync("/api/TokenAuth/Authenticate", new { userNameOrEmailAddress = email, password = Password });
            login.Success.ShouldBeTrue($"login failed: {login}");
            customer.Api.Token = login.Result["accessToken"].GetValue<string>();
            return customer;
        }
    }

    /// <summary>
    /// Shared set-up for the booking tests: two customers (A and B) and a booking period far in the future
    /// (random day in 2040-2048) so test bookings never clash with real data or with each other.
    /// </summary>
    public sealed class BookingTestFixture : IAsyncLifetime
    {
        public TestCustomer CustomerA { get; private set; }
        public TestCustomer CustomerB { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public async Task InitializeAsync()
        {
            if (!TestEnvironment.BackendIsUp) return;
            CustomerA = await TestCustomer.CreateAsync("Alice", "Tester");
            CustomerB = await TestCustomer.CreateAsync("Bob", "Tester");
            Start = new DateTime(2040, 1, 1, 10, 0, 0).AddDays(new Random().Next(0, 3000));
            End = Start.AddDays(2);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public static string Iso(DateTime d) => d.ToString("yyyy-MM-ddTHH:mm:ss");

        /// <summary>Search for cars free in [start, end) and return the first one's id.</summary>
        public async Task<Guid> FindFreeVehicleAsync(ApiClient api, DateTime start, DateTime end)
        {
            var search = await api.GetAsync($"/api/services/app/Vehicles/GetAvailableVehiclesPaged?startDate={Iso(start)}&endDate={Iso(end)}");
            search.Success.ShouldBeTrue($"search failed: {search}");
            var first = search.Result["items"].AsArray().FirstOrDefault();
            first.ShouldNotBeNull("no free vehicle found for the test period - add an Active vehicle with a daily rate");
            return Guid.Parse(first["id"].GetValue<string>());
        }

        public async Task<Guid> CreateBookingAsync(TestCustomer customer, Guid vehicleId, DateTime start, DateTime end)
        {
            var created = await customer.Api.PostAsync("/api/services/app/Bookings/CreateBooking", new
            {
                vehicleId,
                startDate = Iso(start),
                endDate = Iso(end),
                notes = "integration test - safe to delete"
            });
            created.Success.ShouldBeTrue($"booking failed: {created}");
            return Guid.Parse(created.Result.GetValue<string>());
        }
    }
}
