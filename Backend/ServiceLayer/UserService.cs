using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.BuisnessLayer;

namespace Backend.ServiceLayer
{
    public class UserService
    {
        private readonly UserFacade userFacade;

        /// <summary>
        /// Constructor for UserService.
        /// </summary>
        internal UserService(UserFacade userFacade)
        {
            this.userFacade = userFacade;
        }

        /// <summary>
        /// User registration method.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="birthday"></param>
        /// <returns>
        /// Returns a Response object in json format whether the registration was successful or not.
        /// </returns>
        public string Register(string email, string password)
        {
            UserSL newUser;
            try
            {
                newUser = new UserSL(userFacade.Register(email, password));
            }
            catch (Exception err)
            {
                return JsonSerializer.Serialize(new Response<string>(err.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// User login method.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>
        /// Returns a Response object in json format whether the login was successful or not.
        /// </returns>
        public string Login(string email, string password)
        {
            UserSL logUser;
            try
            {
                UserBL user = userFacade.Login(email, password);
                logUser = new UserSL(user);

            }
            catch (Exception err)
            {
                return JsonSerializer.Serialize(new Response<string>(err.Message));
            }
            return JsonSerializer.Serialize(new Response<string>(email, null));
        }

        /// <summary>
        /// User logout method.
        /// </summary>
        /// <param name="email"></param>
        /// <returns>
        /// void
        /// </returns>
        public string Logout(string email)
        {

            try
            {
                userFacade.Logout(email);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Loads all user data from the database.
        /// </summary>
        /// <returns></returns>
        public string LoadAllUserData()
        {
            try
            {
                userFacade.LoadAllUsers();
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Deletes all users from the database.
        /// </summary>
        /// <returns></returns>
        public string DeleteAllUsers()
        {
            try
            {
                userFacade.DeleteAllUsers();
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }
        /// <summary>
        /// Gets a user by email from the database.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public UserSL GetUser(string email)
        {
            return new UserSL(userFacade.GetUser(email));
        }
    }
}
