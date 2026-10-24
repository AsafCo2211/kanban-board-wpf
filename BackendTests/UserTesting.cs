using System;
using System.Text.Json;
using Backend.ServiceLayer;

namespace BackendTests
{
    internal class UserTesting
    {
        private UserService userService = null!;
        private const string DefaultPassword = "Passw0rd";

        /// <summary>
        /// Instantiate the services once.
        /// </summary>
        public void Setup()
        {
            var factory = new ServiceFactory();
            userService = factory.Us;
            // Note: we rely on unique emails per test to avoid collisions
        }

        /// <summary>Requirement 6: Successful user registration</summary>
        public void Test_Register_Success()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_Success-----");
            string json = userService.Register(email, DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage == null)
                Console.WriteLine("Success: User registered");
            else
                Console.WriteLine("Fail: User registration failed");
        }

        /// <summary>Requirement 6: Duplicate email registration</summary>
        public void Test_Register_Duplicate()
        {
            string email = $"dup_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_Duplicate-----");
            userService.Register(email, DefaultPassword);
            string json = userService.Register(email, DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Duplicate registration rejected");
            else
                Console.WriteLine("Fail: Duplicate registration allowed");
        }

        /// <summary>Requirement 6: Invalid password format</summary>
        public void Test_Register_BadPassword()
        {
            string email = $"weak_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_BadPassword-----");
            string json = userService.Register(email, "pass"); // no uppercase or digit
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Weak password rejected");
            else
                Console.WriteLine("Fail: Weak password accepted");
        }

        /// <summary>Requirement 6: Invalid email format</summary>
        public void Test_Register_InvalidEmail()
        {
            Console.WriteLine("-----Running Test_Register_InvalidEmail-----");
            string json = userService.Register("invalid-email", DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Invalid email rejected");
            else
                Console.WriteLine("Fail: Invalid email accepted");
        }

        /// <summary>Requirement 6: Empty email rejected</summary>
        public void Test_Register_EmptyEmail()
        {
            Console.WriteLine("-----Running Test_Register_EmptyEmail-----");
            string json = userService.Register("", DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Empty email rejected");
            else
                Console.WriteLine("Fail: Empty email accepted");
        }

        /// <summary>Requirement 6: Null email rejected</summary>
        public void Test_Register_NullEmail()
        {
            Console.WriteLine("-----Running Test_Register_NullEmail-----");
            string json = userService.Register(null, DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Null email rejected");
            else
                Console.WriteLine("Fail: Null email accepted");
        }

        /// <summary>Requirement 6: Empty password rejected</summary>
        public void Test_Register_EmptyPassword()
        {
            string email = $"emptypass_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_EmptyPassword-----");
            string json = userService.Register(email, "");
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Empty password rejected");
            else
                Console.WriteLine("Fail: Empty password accepted");
        }

        /// <summary>Requirement 6: Null password rejected</summary>
        public void Test_Register_NullPassword()
        {
            string email = $"nullpass_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_NullPassword-----");
            string json = userService.Register(email, null);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Null password rejected");
            else
                Console.WriteLine("Fail: Null password accepted");
        }

        /// <summary>Requirement 7: Successful login</summary>
        public void Test_Login_Success()
        {
            string email = $"login_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Login_Success-----");
            userService.Register(email, DefaultPassword);
            string json = userService.Login(email, DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && (resp.ErrorMessage == null ||
                resp.ErrorMessage.Contains("already logged in", StringComparison.OrdinalIgnoreCase)))
                Console.WriteLine("Success: User login confirmed");
            else
                Console.WriteLine("Fail: Login failed");
        }

        /// <summary>Requirement 7: Login with wrong password</summary>
        public void Test_Login_BadPassword()
        {
            string email = $"wrongpass_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Login_BadPassword-----");
            userService.Register(email, DefaultPassword);
            string json = userService.Login(email, "Wrong123");
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Incorrect password rejected");
            else
                Console.WriteLine("Fail: Logged in with incorrect password");
        }

        /// <summary>Requirement 7: Login for non-existing user</summary>
        public void Test_Login_NoUser()
        {
            string email = $"nouser_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Login_NoUser-----");
            string json = userService.Login(email, DefaultPassword);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Non-existent user login rejected");
            else
                Console.WriteLine("Fail: Non-existent user logged in");
        }

        /// <summary>Requirement 7: Successful logout</summary>
        public void Test_Logout_Success()
        {
            string email = $"logout_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Logout_Success-----");
            userService.Register(email, DefaultPassword);
            string json = userService.Logout(email);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage == null)
                Console.WriteLine("Success: User logged out");
            else
                Console.WriteLine("Fail: Logout failed");
        }

        /// <summary>Requirement 7: Logout of user not logged in</summary>
        public void Test_Logout_NotLoggedIn()
        {
            string email = $"nonlogin_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Logout_NotLoggedIn-----");
            string json = userService.Logout(email);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Logout rejected for non-logged-in user");
            else
                Console.WriteLine("Fail: Logout succeeded without login");
        }

        /// <summary>Requirement 6: Password shorter than 6 characters rejected</summary>
        public void Test_Register_PasswordTooShort()
        {
            string email = $"short_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_PasswordTooShort-----");
            string json = userService.Register(email, "A1b");
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp?.ErrorMessage != null)
                Console.WriteLine("Success: Too-short password rejected");
            else
                Console.WriteLine("Fail: Too-short password accepted");
        }

        /// <summary>Requirement 6: Password longer than 20 characters rejected</summary>
        public void Test_Register_PasswordTooLong()
        {
            string email = $"long_{Guid.NewGuid():N}@kanban.com";
            string longPass = new string('A', 21) + "1a";
            Console.WriteLine("-----Running Test_Register_PasswordTooLong-----");
            string json = userService.Register(email, longPass);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp?.ErrorMessage != null)
                Console.WriteLine("Success: Too-long password rejected");
            else
                Console.WriteLine("Fail: Too-long password accepted");
        }

        /// <summary>Requirement 6: Password missing uppercase rejected</summary>
        public void Test_Register_PasswordMissingUppercase()
        {
            string email = $"noupper_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_PasswordMissingUppercase-----");
            string json = userService.Register(email, "lowercase1");
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp?.ErrorMessage != null)
                Console.WriteLine("Success: Missing-uppercase password rejected");
            else
                Console.WriteLine("Fail: Missing-uppercase password accepted");
        }

        /// <summary>Requirement 6: Password missing lowercase rejected</summary>
        public void Test_Register_PasswordMissingLowercase()
        {
            string email = $"nolower_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_PasswordMissingLowercase-----");
            string json = userService.Register(email, "UPPERCASE1");
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp?.ErrorMessage != null)
                Console.WriteLine("Success: Missing-lowercase password rejected");
            else
                Console.WriteLine("Fail: Missing-lowercase password accepted");
        }

        /// <summary>Requirement 6: Password missing digit rejected</summary>
        public void Test_Register_PasswordMissingDigit()
        {
            string email = $"nodigit_{Guid.NewGuid():N}@kanban.com";
            Console.WriteLine("-----Running Test_Register_PasswordMissingDigit-----");
            string json = userService.Register(email, "NoDigitsHere");
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp?.ErrorMessage != null)
                Console.WriteLine("Success: Missing-digit password rejected");
            else
                Console.WriteLine("Fail: Missing-digit password accepted");
        }


        /// <summary>
        /// Runs all user tests in sequence.
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("\n=== User Tests ===\n");
            Setup();
            Test_Register_Success();
            Test_Register_Duplicate();
            Test_Register_BadPassword();
            Test_Register_InvalidEmail();
            Test_Register_EmptyEmail();
            Test_Register_NullEmail();
            Test_Register_EmptyPassword();
            Test_Register_NullPassword();
            Test_Login_Success();
            Test_Login_BadPassword();
            Test_Login_NoUser();
            Test_Logout_Success();
            Test_Logout_NotLoggedIn();
            Test_Register_PasswordTooShort();
            Test_Register_PasswordTooLong();
            Test_Register_PasswordMissingUppercase();
            Test_Register_PasswordMissingLowercase();
            Test_Register_PasswordMissingDigit();
        }
    }
}
