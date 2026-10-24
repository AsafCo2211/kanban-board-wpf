using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.DTOs
{
    internal class UserDAL
    {
        public const string EmailColumnName = "email";
        public const string PasswordColumnName = "password";
        private string email;
        private string password;
        private bool isPersisted = false;
        private UserController controller;

        public UserDAL(string email, string password, UserController uc)
        {
            this.email = email;
            this.password = password;
            this.isPersisted = false; //need to check if user is persisted in the database
            this.controller = uc;
        }

        public string Email
        {
            get => email;
        }

        public  string Password // TODO: consider  removing this as it may expose password, but than we need to change the
                                // controller methods to accept password as a parameter instead of using the property
        {
            get => password;
            set
            {
                if (!isPersisted)
                {
                    throw new Exception($"User {email} is not persisted; cannot update password.");
                }

                if (!controller.UpdatePassword(email, value))
                {
                    throw new Exception($"Failed to update password for user {email} in DB.");
                }

                password = value;
            }
        }
        public bool IsPersisted { get => isPersisted; set { isPersisted = value; } }



        /// <summary>
        /// Saves the user to the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Save()
        {
            if (isPersisted) // If the user is already persisted, we throw an exception
            {
                throw new Exception("User is already persisted in the database.");
            }
            if(!controller.AddNewUser(this)) // Attempt to add the user to the database, if it fails, an exception is thrown
            {
                throw new Exception("Failed to save user to the database.");
            }

            isPersisted = true;
        }
    }
}
