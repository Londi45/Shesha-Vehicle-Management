using Abp.Domain.Repositories;
using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Notifications;
using NSubstitute;
using Shesha.Domain;
using Shesha.EntityReferences;
using Shesha.Notifications;
using Shesha.Notifications.Dto;
using Shesha.Notifications.MessageParticipants;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Unit
{
    /// <summary>
    /// Unit tests for BookingNotificationSender using fakes (NSubstitute) instead of a real database or email server.
    /// We check WHAT would be sent, not that an email arrives.
    /// </summary>
    public class BookingNotificationSenderTests
    {
        private readonly INotificationSender _postOffice = Substitute.For<INotificationSender>();
        private readonly IRepository<NotificationTypeConfig, Guid> _types = Substitute.For<IRepository<NotificationTypeConfig, Guid>>();
        private readonly IRepository<NotificationChannelConfig, Guid> _channels = Substitute.For<IRepository<NotificationChannelConfig, Guid>>();

        private BookingNotificationSender CreateSender(NotificationTypeConfig typeToReturn)
        {
            _types.FirstOrDefaultAsync(Arg.Any<Expression<Func<NotificationTypeConfig, bool>>>()).Returns(typeToReturn);
            _channels.FirstOrDefaultAsync(Arg.Any<Expression<Func<NotificationChannelConfig, bool>>>()).Returns((NotificationChannelConfig)null);
            return new BookingNotificationSender(_postOffice, _types, _channels);
        }

        private static Booking SampleBooking(bool withCustomer = true) => new Booking
        {
            Id = Guid.NewGuid(),
            Customer = withCustomer ? new Customer { Id = Guid.NewGuid(), FirstName = "Thabo" } : null,
            Vehicle = new Vehicle { RegistrationNumber = "GP 456-789", Make = "Toyota", Model = "Fortuner" },
            StartDate = new DateTime(2030, 11, 14, 9, 0, 0),
            EndDate = new DateTime(2030, 11, 16, 9, 0, 0),
            TotalCosts = 1900m
        };

        [Fact]
        public async Task Confirmed_notification_is_sent_to_the_customer_with_the_booking_details()
        {
            BookingNotificationModel sentData = null;
            await _postOffice.SendNotificationAsync(
                Arg.Any<NotificationTypeConfig>(), Arg.Any<IMessageSender>(), Arg.Any<IMessageReceiver>(),
                Arg.Do<BookingNotificationModel>(d => sentData = d),
                Arg.Any<List<NotificationAttachmentDto>>(), Arg.Any<string>(), Arg.Any<GenericEntityReference>(),
                Arg.Any<NotificationChannelConfig>(), Arg.Any<string>());

            var sender = CreateSender(new NotificationTypeConfig { Name = "BookingConfirmed" });

            await sender.NotifyBookingConfirmedAsync(SampleBooking());

            await _postOffice.Received(1).SendNotificationAsync(
                Arg.Is<NotificationTypeConfig>(t => t.Name == "BookingConfirmed"),
                Arg.Any<IMessageSender>(),
                Arg.Any<PersonMessageParticipant>(),
                Arg.Any<BookingNotificationModel>(),
                Arg.Any<List<NotificationAttachmentDto>>(), Arg.Any<string>(), Arg.Any<GenericEntityReference>(),
                Arg.Any<NotificationChannelConfig>(), Arg.Any<string>());

            sentData.ShouldNotBeNull();
            sentData.CustomerName.ShouldBe("Thabo");
            sentData.VehicleName.ShouldBe("GP 456-789 - Toyota Fortuner");
            sentData.StartDate.ShouldBe("14 Nov 2030, 09:00");
            sentData.TotalCost.ShouldBe("R 1,900.00");
        }

        [Fact]
        public async Task Nothing_is_sent_when_the_booking_has_no_customer()
        {
            var sender = CreateSender(new NotificationTypeConfig { Name = "BookingConfirmed" });

            await sender.NotifyBookingConfirmedAsync(SampleBooking(withCustomer: false));

            await _postOffice.DidNotReceiveWithAnyArgs().SendNotificationAsync<BookingNotificationModel>(
                default, (IMessageSender)default, (IMessageReceiver)default, default, default, default, default, default, default);
        }

        [Fact]
        public async Task Nothing_is_sent_when_the_notification_type_has_not_been_set_up()
        {
            var sender = CreateSender(typeToReturn: null);

            await sender.NotifyBookingCancelledAsync(SampleBooking());

            await _postOffice.DidNotReceiveWithAnyArgs().SendNotificationAsync<BookingNotificationModel>(
                default, (IMessageSender)default, (IMessageReceiver)default, default, default, default, default, default, default);
        }
    }
}
