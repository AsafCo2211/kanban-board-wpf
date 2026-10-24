using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;

namespace Backend.BuisnessLayer
{
    internal class AuthenticationFacade
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly HashSet<string> currentLoggedIn = new HashSet<string>(); // manages current logged in users
        private readonly HashSet<string> registeredUsers = new HashSet<string>(); // manages all registered users

        /// <summary>
        /// Logs in a given user by his email.
        /// Happens after checking the log-in logic in userfacade.
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool Login(string mail)
        {
            if (currentLoggedIn.Contains(mail.ToLower()))
            {
                Log.Error($"User {mail} already logged in");
                throw new Exception("User already logged in");
            }
            currentLoggedIn.Add(mail.ToLower());
            return true;
        }

        /// <summary>
        /// Checks if a given user is already logged in or not.
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public bool IsLoggedIn(string mail)
        {
            bool isLogged = currentLoggedIn.Contains(mail.ToLower());
            return isLogged;
        }

        /// <summary>
        /// Logs out a user out of the system.
        /// </summary>
        /// <param name="email"></param>
        public string Logout(string email)
        {
            if (currentLoggedIn.Remove(email.ToLower()))
            {
                Log.Info($"User {email} logged out.");
                return $"User {email} logged out successfully";
            }
            Log.Error($"User {email} is not logged in.");
            throw new Exception($"User {email} is not logged in");
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="mail"></param>
        /// <exception cref="Exception"></exception>
        public void Register(string mail)
        {
            if (registeredUsers.Contains(mail.ToLower())) // to lower as email is case-insensitive
            {
                Log.Error($"User {mail} is already registered.");
                throw new Exception("User already registered");
            }
            registeredUsers.Add(mail.ToLower()); // to lower as email is case-insensitive
        }

        /// <summary>
        /// Checks if a given user is already registered in the system or not.S
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public bool IsUserExist(string mail)
        {
            return registeredUsers.Contains(mail.ToLower());
        }

        /// <summary>
        /// Adds a user to the registered users list silently, without any checks.
        /// used for loading users from the database into memory.
        /// </summary>
        /// <param name="email"></param>
        public void AddRegisteredUserSilently(string email)
        {
            email = email.ToLower();
            if (!registeredUsers.Contains(email))
            {
                registeredUsers.Add(email);
            }

            // Do NOT log the user in – just register them in memory
            Log.Info($"Silently registered user {email} into memory (loaded from DB).");
        }

        /// <summary>
        /// Clears all current logged in users and registered users. For testing
        /// </summary>
        public void ClearAll()
        {
            currentLoggedIn.Clear();
            registeredUsers.Clear();
        }
    }
}
