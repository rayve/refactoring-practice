using System;
using AutoFixture;
using Bogus;
using FluentAssertions;
using Moq;
using Xunit;

namespace LegacyApp.UnitTests
{
    public class UserServiceTests
    {
        private readonly UserService _sut;
        
        private readonly IFixture _fixture = new Fixture();
        private readonly Faker _fake = new Faker();

        private readonly Mock<Func<DateTime>> _stubDateTimeNow = new Mock<Func<DateTime>>();
        private readonly Mock<IClientRepository> _mockClientRepository = new Mock<IClientRepository>();
        private readonly Mock<IUserCreditService> _mockUserCreditService = new Mock<IUserCreditService>();
        private readonly Mock<Action<User>> _mockUserDataAccess = new Mock<Action<User>>();

        public UserServiceTests()
        {
            _sut = new UserService(
                _stubDateTimeNow.Object,
                _mockClientRepository.Object,
                _mockUserCreditService.Object,
                _mockUserDataAccess.Object);
        }

        [Fact]
        public void AddUser_ShouldCreateUser_WhenAllParametersAreValid()
        {
            // Arrange
            const int clientId = 1;
            var firstName = _fake.Name.FirstName();
            var lastName = _fake.Name.LastName();
            var email = _fake.Internet.Email();
            var dateOfBirth = new DateTime(1993, 1, 1);
            var client = _fixture.Build<Client>()
                .With(c => c.Id, clientId)
                .Create();

            _stubDateTimeNow.Setup(f => f.Invoke())
                .Returns(new DateTime(2021, 2, 16));
            
            _mockClientRepository.Setup(r => r.GetById(clientId))
                .Returns(client);
            
            _mockUserCreditService.Setup(s => s.GetCreditLimit(firstName, lastName, dateOfBirth))
                .Returns(600);

            // Act
            var result = _sut.AddUser(firstName, lastName, email, dateOfBirth, clientId);

            // Assert
            result.Should().BeTrue();
            _mockUserDataAccess.Verify(a => a.Invoke(It.IsAny<User>()), Times.Once);
        }

        [Theory]
        [InlineData("", "lastname", "first.last@gmail.com", 1993)]
        [InlineData("firstname", "", "first.last@gmail.com", 1993)]
        [InlineData("firstname", "lastname", "invalidemail", 1993)]
        [InlineData("firstname", "lastname", "first.last@gmail.com", 2002)]
        public void AddUser_ShouldNotCreateUser_WhenInputDetailsAreInvalid(
            string firstName, string lastName, string email, int yearOfBirth)
        {
            // Arrange
            const int clientId = 1;
            var dateOfBirth = new DateTime(yearOfBirth, 1, 1);
            var client = _fixture.Build<Client>()
                .With(c => c.Id, () => clientId)
                .Create();
        
            _stubDateTimeNow.Setup(f => f.Invoke())
                .Returns(new DateTime(2021, 2, 16));
            
            _mockClientRepository.Setup(r => r.GetById(clientId))
                .Returns(client);
            
            _mockUserCreditService.Setup(s => s.GetCreditLimit(firstName, lastName, dateOfBirth))
                .Returns(600);
            
            // Act
            var result = _sut.AddUser(firstName, lastName, email, dateOfBirth, 1);
        
            // Assert
            result.Should().BeFalse();
        }
        
        [Theory]
        [InlineData("RandomClientName", true, 600, 600)]
        [InlineData("ImportantClient", true, 600, 1200)]
        [InlineData("VeryImportantClient", false, 0, 0)]
        public void AddUser_ShouldCreateUserWithCorrectCreditLimit_WhenNameIndicatesDifferentClassification(
            string clientName, bool hasCreditLimit, int initialCreditLimit, int finalCreditLimit)
        {
            // Arrange
            const int clientId = 1;
            const string firstName = "Nick";
            const string lastName = "Chapsas";
            var dateOfBirth = new DateTime(1993, 10, 10);
            var client = _fixture.Build<Client>()
                .With(c => c.Id, clientId)
                .With(c => c.Name, clientName)
                .Create();
        
            _stubDateTimeNow.Setup(f => f.Invoke())
                .Returns(new DateTime(2021, 2, 16));
            
            _mockClientRepository.Setup(r => r.GetById(clientId))
                .Returns(client);
            
            _mockUserCreditService.Setup(s => s.GetCreditLimit(firstName, lastName, dateOfBirth))
                .Returns(initialCreditLimit);
        
            // Act
            var result = _sut.AddUser(firstName, lastName, "nick.chapsas@gmail.com", dateOfBirth, 1);
        
            // Assert
            result.Should().BeTrue();
            _mockUserDataAccess
                .Verify(da => da.Invoke(
                    It.Is<User>(user => user.HasCreditLimit == hasCreditLimit && user.CreditLimit == finalCreditLimit)
                    ));
        }
        
        [Fact]
        public void AddUser_ShouldNotCreateUser_WhenUserHasCreditLimitAndCreditLimitIsLessThan500()
        {
            // Arrange
            const int clientId = 1;
            const string firstName = "Nick";
            const string lastName = "Chapsas";
            var dateOfBirth = new DateTime(1993, 10, 10);
            var client = _fixture.Build<Client>()
                .With(c => c.Id, () => clientId)
                .Create();
        
            _stubDateTimeNow.Setup(f => f.Invoke())
                .Returns(new DateTime(2021, 2, 16));
            
            _mockClientRepository.Setup(r => r.GetById(clientId))
                .Returns(client);
            
            _mockUserCreditService.Setup(s => s.GetCreditLimit(firstName, lastName, dateOfBirth))
                .Returns(499);
        
            // Act
            var result = _sut.AddUser(firstName, lastName, "nick.chapsas@gmail.com", dateOfBirth, 1);
        
            // Assert
            result.Should().BeFalse();
        }
    }
}
