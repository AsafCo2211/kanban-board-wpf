using Backend.ServiceLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frontend.Model
{
    internal class UserModel : NotifiableModelObject
    {
        private string email;
        public string Email
        {
            get => email;
        }

        public UserModel(ServiceController cont, string email) : base(cont)
        {
            this.email = email;
        }
    }
}
