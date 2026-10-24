using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.ServiceLayer;

namespace Frontend.Model
{
    class TaskModel : Notifiable
    {
        public string Title { get; set; }
        public string Assignee { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreationTime { get; set; } 

        internal TaskModel(TaskSL taskSL)
        {
            Title = taskSL.Title;
            Assignee = taskSL.Assignee; 
            DueDate = taskSL.DueDate;
            CreationTime = taskSL.CreationTime;
        }
    }
}
