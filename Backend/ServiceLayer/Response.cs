using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.ServiceLayer
{
    public class Response<T>
    {

        public string? ErrorMessage { get;set; }
        public T? ReturnValue {  get;set; }
        
        public Response(){ } // Default null constructor

        public Response(string ErrorMessage)
        {
            this.ErrorMessage = ErrorMessage;
        }

        public Response(T ReturnValue)
        {
            this.ReturnValue = ReturnValue;
            this.ErrorMessage = null;
        }
        public Response(T value,string ErrorMessage)
        {
            this.ReturnValue = value;
            this.ErrorMessage = ErrorMessage;
        }
    }
}
