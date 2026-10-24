using Backend.BuisnessLayer;
using Backend.ServiceLayer;
using NUnit.Framework;
using System;
using System.Text.Json;

namespace BackendTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private UserService _userService;
        private AuthenticationFacade _authFacade;
        private UserFacade _userFacade;

        [SetUp]
        public void SetUp()
        {
            // Arrange: fresh AuthenticationFacade + UserFacade per test and clear DB
            _authFacade = new AuthenticationFacade();
            _userFacade = new UserFacade(_authFacade);
            _userService = new UserService(_userFacade);
            _userService.DeleteAllUsers();
        }

        // Helper to deserialize a Response<T>
        private Response<T> Deserialize<T>(string json) =>
            JsonSerializer.Deserialize<Response<T>>(json);

        /// <summary>
        /// Req.6: Register with new email and strong password should succeed.
        /// </summary>
        [Test]
        public void Register_WithNewEmailAndStrongPassword_ShouldSucceed()
        {
            // Arrange
            var email = $"user{DateTime.Now.Ticks}@test.com";
            var password = "Valid123";

            // Act
            var resp = Deserialize<object>(_userService.Register(email, password));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Null);
        }

        /// <summary>
        /// Req.6: Register with invalid email should return error.
        /// </summary>
        [Test]
        public void Register_WithInvalidEmail_ShouldReturnError()
        {
            // Arrange
            var invalidEmail = "not-an-email";
            var password = "Valid123";

            // Act
            var resp = Deserialize<object>(_userService.Register(invalidEmail, password));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Not.Null);
        }

        /// <summary>
        /// Req.6: Register with invalid password should return error.
        /// </summary>
        [Test]
        public void Register_WithInvalidPassword_ShouldReturnError()
        {
            // Arrange
            var email = $"valid{DateTime.Now.Ticks}@test.com";
            var shortPassword = "short";

            // Act
            var resp = Deserialize<object>(_userService.Register(email, shortPassword));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Not.Null);
        }

        /// <summary>
        /// Req.6: Register duplicate email (case-insensitive) should return error.
        /// </summary>
        [Test]
        public void Register_WithDuplicateEmail_ShouldReturnError()
        {
            // Arrange
            var email = $"dup{DateTime.Now.Ticks}@test.com";
            _userService.Register(email, "Valid123");
            var duplicateEmail = email.ToUpperInvariant();

            // Act
            var resp = Deserialize<object>(_userService.Register(duplicateEmail, "Valid123"));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Not.Null);
        }

        /// <summary>
        /// Req.7: Login with valid credentials should return email.
        /// </summary>
        [Test]
        public void Login_WithValidCredentials_ShouldReturnEmail()
        {
            // Arrange
            var email = $"login{DateTime.Now.Ticks}@test.com";
            var password = "Valid123";
            _userService.Register(email, password);
            _userService.Logout(email);

            // Act
            var resp = Deserialize<string>(_userService.Login(email, password));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Null);
            Assert.That(resp.ReturnValue, Is.EqualTo(email));
        }

        /// <summary>
        /// Req.7: Login unregistered user should return error.
        /// </summary>
        [Test]
        public void Login_UnregisteredUser_ShouldReturnError()
        {
            // Arrange
            var email = "noone@test.com";
            var password = "Whatever1";

            // Act
            var resp = Deserialize<string>(_userService.Login(email, password));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Not.Null);
        }

        /// <summary>
        /// Req.7: Login with wrong password should return error.
        /// </summary>
        [Test]
        public void Login_WithWrongPassword_ShouldReturnError()
        {
            // Arrange
            var email = $"fail{DateTime.Now.Ticks}@test.com";
            var password = "Valid123";
            _userService.Register(email, password);

            // Act
            var resp = Deserialize<string>(_userService.Login(email, "WrongPass"));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Not.Null);
        }

        /// <summary>
        /// Req.7: Logout logged-in user should succeed.
        /// </summary>
        [Test]
        public void Logout_WhenLoggedIn_ShouldSucceed()
        {
            // Arrange
            var email = $"logout{DateTime.Now.Ticks}@test.com";
            var password = "Valid123";
            _userService.Register(email, password);

            // Act
            var resp = Deserialize<object>(_userService.Logout(email));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Null);
        }

        /// <summary>
        /// Req.7: Logout when not logged in should return error.
        /// </summary>
        [Test]
        public void Logout_WhenNotLoggedIn_ShouldReturnError()
        {
            // Arrange
            var email = "nobody@test.com";

            // Act
            var resp = Deserialize<object>(_userService.Logout(email));

            // Assert
            Assert.That(resp.ErrorMessage, Is.Not.Null);
        }

        /// <summary>
        /// Req.?: DeleteAllUsers should remove all users so login fails.
        /// </summary>
        [Test]
        public void DeleteAllUsers_ShouldRemoveAllUsers()
        {
            // Arrange
            var email = $"del{DateTime.Now.Ticks}@test.com";
            var password = "Valid123";
            _userService.Register(email, password);

            // Act
            var delResp = Deserialize<object>(_userService.DeleteAllUsers());

            // Assert
            Assert.That(delResp.ErrorMessage, Is.Null, "DeleteAllUsers failed");

            // Act
            var loginResp = Deserialize<string>(_userService.Login(email, password));

            // Assert
            Assert.That(loginResp.ErrorMessage, Is.Not.Null);
        }
    }
}
