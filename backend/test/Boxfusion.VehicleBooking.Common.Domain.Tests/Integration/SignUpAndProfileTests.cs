using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Integration
{
    /// <summary>Sign-up (public) and My Profile (only ever "me").</summary>
    [Trait("Category", "Integration")]
    public class SignUpAndProfileTests
    {
        private static object SignUpBody(string email, string password = TestCustomer.Password, string confirm = TestCustomer.Password,
                                         string firstName = "Sign", string lastName = "Up") => new
        {
            emailAddress = email,
            password,
            confirmPassword = confirm,
            firstName,
            lastName,
            mobileNumber = "0710000000"
        };

        private static string NewEmail() => $"it-test-{Guid.NewGuid():N}@example.com";

        [IntegrationFact]
        public async Task Sign_up_creates_a_login_linked_to_a_customer_profile()
        {
            var customer = await TestCustomer.CreateAsync("Naledi", "Signup");

            var profile = await customer.Api.GetAsync("/api/services/app/Customer/GetProfile");

            profile.Success.ShouldBeTrue(profile.ToString());
            profile.Result["firstName"].GetValue<string>().ShouldBe("Naledi");
            profile.Result["lastName"].GetValue<string>().ShouldBe("Signup");
            profile.Result["emailAddress"].GetValue<string>().ShouldBe(customer.Email);
        }

        [IntegrationFact]
        public async Task Sign_up_with_an_email_that_is_already_used_is_rejected()
        {
            var customer = await TestCustomer.CreateAsync();

            var again = await new ApiClient().PostAsync("/api/services/app/Registration/Register", SignUpBody(customer.Email.ToUpperInvariant()));

            again.Success.ShouldBeFalse();
            again.ErrorMessage.ShouldContain("already exists");
        }

        [IntegrationFact]
        public async Task Sign_up_with_passwords_that_do_not_match_is_rejected()
        {
            var result = await new ApiClient().PostAsync("/api/services/app/Registration/Register",
                SignUpBody(NewEmail(), confirm: "Different@12345"));

            result.Success.ShouldBeFalse(result.ToString());
        }

        [IntegrationFact]
        public async Task Sign_up_without_names_is_rejected()
        {
            var result = await new ApiClient().PostAsync("/api/services/app/Registration/Register",
                SignUpBody(NewEmail(), firstName: "", lastName: ""));

            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("First name and last name are required");
        }

        [IntegrationFact]
        public async Task Profile_update_saves_contact_details_but_never_the_licence()
        {
            var customer = await TestCustomer.CreateAsync();

            var update = await customer.Api.PutAsync("/api/services/app/Customer/UpdateMyProfile", new
            {
                firstName = "  Updated  ",
                lastName = "Name",
                emailAddress = customer.Email,
                mobileNumber = "0829999999",
                driversLicenceNumber = "HACKED-LICENCE"   // must be ignored: staff-only field
            });
            update.Success.ShouldBeTrue(update.ToString());

            var profile = await customer.Api.GetAsync("/api/services/app/Customer/GetProfile");
            profile.Result["firstName"].GetValue<string>().ShouldBe("Updated");          // trimmed
            profile.Result["mobileNumber"].GetValue<string>().ShouldBe("0829999999");
            profile.Result["driversLicenceNumber"].GetValue<string>().ShouldBe("IT-TEST-LICENCE");
        }

        [IntegrationFact]
        public async Task Profile_update_with_an_empty_name_is_rejected()
        {
            var customer = await TestCustomer.CreateAsync();

            var update = await customer.Api.PutAsync("/api/services/app/Customer/UpdateMyProfile", new { firstName = "", lastName = "Name" });

            update.Success.ShouldBeFalse();
            update.ErrorMessage.ShouldContain("First name and last name are required");
        }
    }
}
