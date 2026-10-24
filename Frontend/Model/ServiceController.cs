using Backend.ServiceLayer;
using Frontend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Frontend.Model
{
    class ServiceController
    {
        internal ServiceFactory serviceFactory;
        internal UserService US;
        internal BoardService BS;
        internal TaskService TS;

        public ServiceController() : this(new ServiceFactory()) { }

        public ServiceController(ServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
            US = serviceFactory.Us;
            BS = serviceFactory.Bs;
            TS = serviceFactory.Ts;
            LoadAllData();
        }

        public void LoadAllData()
        {
            var response = JsonSerializer.Deserialize<Response<string>>(serviceFactory.LoadAllData());
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new Exception(response.ErrorMessage);
            }
        }

        internal UserModel Login(string email, string pass)
        {
            var userJson = US.Login(email, pass);
            var response = JsonSerializer.Deserialize<Response<string>>(userJson);
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new Exception(response.ErrorMessage);
            }

            LoadAllData();
            return new UserModel(this, response.ReturnValue);
        }

        internal void Register(string email, string pass)
        {
            var response = JsonSerializer.Deserialize<Response<string>>(US.Register(email, pass));
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new Exception(response.ErrorMessage);
            }
            LoadAllData();
        }
        public UserModel GetUser(string email)
        {
            return new UserModel(this, US.GetUser(email).Email);
        }
        internal void CreateBoard(string boardName, string email)
        {
            var response = JsonSerializer.Deserialize<Response<string>>(BS.CreateBoard(boardName, email));
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new Exception(response.ErrorMessage);
            }
        }
        internal void DeleteBoard(string boardName, string email)
        {
            var response = JsonSerializer.Deserialize<Response<string>>(BS.DeleteBoard(boardName, email));
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new Exception(response.ErrorMessage);
            }
        }
        internal void Logout(string email)
        {
            var response = JsonSerializer.Deserialize<Response<string>>(US.Logout(email));
            if (!string.IsNullOrEmpty(response.ErrorMessage))
                throw new Exception(response.ErrorMessage);
        }
    }
}
