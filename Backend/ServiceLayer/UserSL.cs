using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.BuisnessLayer;    


namespace Backend.ServiceLayer
{
    public class UserSL
    {
        public string Email { get;}
        private Dictionary<string, BoardSL> Board_list { get;}

        public UserSL(string email) 
        {
            this.Email = email;
            this.Board_list = new Dictionary<string, BoardSL>();
        }

        internal UserSL(UserBL user)
        {
            this.Email = user.Email;
        }
    }
}
