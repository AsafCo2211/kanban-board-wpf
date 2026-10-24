using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using log4net;
using System.Reflection;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;

[assembly:InternalsVisibleTo("Backend.ServiceLayer")]

namespace Backend.BuisnessLayer
{
    internal class UserBL
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly UserDAL userDAL;

        private string email;
        private string password;

        public UserBL(string email, string password, UserController uc)
        {
            this.userDAL = new UserDAL(email, password, uc);
            Email = email;
            Password = password;
            Log.Info($"User {email} created and saved to db successfully.");
            userDAL.Save(); // Save the user to the database
        }

        public string Email { get => email;
            private set 
            {
                if (string.IsNullOrWhiteSpace(value) || !IsValidEmail(value))
                {
                    Log.Error("Email not in valid format.");
                    throw new ArgumentException("Email not in valid format.");
                }

                email = value;
            }
        }

        private string Password
        {
             set
            {
                if (string.IsNullOrWhiteSpace(value) || !IsValidPassword(value))
                {
                    Log.Error("Password doesn't match expected criteria.");
                    throw new Exception("Password doesn't match expected criteria.");
                }

                password = value;
                Log.Info($"Password for user {Email} updated in memory.");
            }
        }

        public UserDAL  UserDAL => userDAL;

        /// <summary>
        /// Constructs a UserBL from an existing UserDAL.
        /// </summary>
        /// <param name="newUser">The UserDAL</param>
        public UserBL(UserDAL newUser)
        {
            // no need to validate email and password here, as they are already validated in UserBL constructor
            if (newUser == null)
            {
                Log.Error("UserDAL cannot be null.");
                throw new ArgumentNullException(nameof(newUser));
            }

            userDAL = newUser;
            email = newUser.Email; // TODO: change to propety stter to make sur DB was not messed up with
            password = newUser.Password;// TODO: change to propety stter to make sur DB was not messed up with
            Log.Info($"UserBL loaded for {email}");
        }

        /// <summary>
        /// Checks if the given password matches the user's current password.
        /// </summary>
        /// <param name="pass">The password to check</param>
        /// <returns>True if match, false otherwise</returns>
        public bool Login(string pass)
        {
            return pass.Trim() == password.Trim();
        }

        /// <summary>
        /// Validates that an email matches RFC-like email pattern.
        /// </summary>
        private bool IsValidEmail(string email)
        {
            string pattern = @"^(?!\.)(?!.*\.\.)[A-Za-z0-9!#$%&'*+/=?^_`{|}~.-]+(?<!\.)@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }
        /// <summary>
        /// Validates that a password is 6–20 chars and contains upper, lower, and digit.
        /// </summary>
        private bool IsValidPassword(string password)
        {
            string rgx = @"^(?=.{6,20}$)(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$";
            return Regex.IsMatch(password, rgx);
        }
    }
}
