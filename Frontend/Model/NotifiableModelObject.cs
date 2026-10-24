using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frontend.Model
{
    internal abstract class NotifiableModelObject : Notifiable
    {
        public ServiceController Controller {get; private set; }

        protected NotifiableModelObject(ServiceController cont)
        {
            Controller = cont;
        }
    }
}
