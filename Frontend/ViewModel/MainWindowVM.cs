using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Frontend.Model;
using log4net;
using log4net.Config;

namespace Frontend.ViewModel
{
    class MainWindowVM : Notifiable
    {
        private ServiceController ServiceController;
        private string email;
        private string errorMessage;
        private string password;
        public string Email
        {
            get => email;
            set
            {
                if (email != value)
                {
                    email = value;
                    RaisePropertyChanged(nameof(Email));
                }
            }
        }
        public string Password
        {
            get => password;
            set
            {
                if (password != value)
                {
                    password = value;
                    RaisePropertyChanged(nameof(Password));
                }
            }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                errorMessage = value;
                RaisePropertyChanged(nameof(ErrorMessage));
            }
        }


        internal MainWindowVM()
        {
            ServiceController = new ServiceController();

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            var logConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            XmlConfigurator.Configure(logRepository, new FileInfo(logConfigPath));
            ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            email = string.Empty;
            password = string.Empty;
            errorMessage = "Please enter your email and password.";
        }

        public UserModel Login()
        {
            try
            {
                return ServiceController.Login(Email, Password);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
        }

        public UserModel Register()
        {
            try
            {
                ServiceController.Register(Email, Password);
                return ServiceController.GetUser(Email); //gets the logged-in user from memory
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
        }
    }
}
