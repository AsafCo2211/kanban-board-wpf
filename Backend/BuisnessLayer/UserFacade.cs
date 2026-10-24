using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;

namespace Backend.BuisnessLayer
{
    
    internal class UserFacade
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, UserBL> usersData; // manages users data
        private readonly AuthenticationFacade authFacade;
        private readonly UserController controller;

        public UserFacade(AuthenticationFacade authFacade)
        {
            this.usersData = new Dictionary<string, UserBL>();
            this.authFacade = authFacade;
            this.controller = new UserController();
        }

        /// <summary>
        /// Registers a new user in the system with the given email and password.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public UserBL Register(string email, string pass)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Invalid email provided.");

            if (usersData.ContainsKey(email.ToLower()))
                throw new Exception("User already exists.");

            UserBL user = new UserBL(email, pass, controller); 
            authFacade.Register(email.ToLower());              
            usersData[email.ToLower()] = user;              
            Log.Info($"User {email} registered successfully.");
            authFacade.Login(email.ToLower());

            return user;
        }


        /// <summary>
        /// Logs in a user based on successful credentials given.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public UserBL Login(string email, string pass)
         {
            if (!usersData.ContainsKey(email.ToLower()))
            {
                Log.Error($"Login failed: No such user {email}.");
                throw new Exception("User not found.");
            }

            UserBL user = usersData[email.ToLower()];
            if (user.Login(pass))
            {
                authFacade.Login(email.ToLower());
                return user;
            }
            Log.Error($"Login failed for {email}: Incorrect password.");
            throw new Exception("Email or password is incorrect.");
        }


        /// <summary>
        /// Logs out a user out of the system.
        /// </summary>
        /// <param name="email"></param>
        public string Logout(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new Exception("Email must not be empty.");

            return authFacade.Logout(email.ToLower());
        }
        /// <summary>
        /// Deletes all user from the system
        /// </summary>
        /// <returns></returns>
        public bool DeleteAllUsers() //This and LoadAll are only places controller is used in BL
        {
            authFacade.ClearAll(); // clears logged in users and registered users
            usersData.Clear();                  // clears RAM
            return controller.DeleteAll();      // clears DB
        }

        /// <summary>
        /// Loads all users from the database into memory. This and DeleteAll are only places controller is used in BL
        /// </summary>
        public void LoadAllUsers()
        {
            var allUsers = controller.LoadAll();

            usersData.Clear();  // Reset in-memory data
            foreach (var dal in allUsers)
            {
                var user = new UserBL(dal);
                usersData[dal.Email.ToLower()] = user;
                authFacade.AddRegisteredUserSilently(dal.Email); // Register user in memory without logging in
            }
            Log.Info("All users loaded into memory.");
        }
        /// <summary>
        /// Gets a user by their email from the in-memory data.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public UserBL GetUser(string email)
        {
            return usersData[email.ToLower()];
        }


    }
}
