using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Backend.BuisnessLayer;

namespace Backend.ServiceLayer
{
    public enum StateOfTask
    {
        BackLog = 0,
        InProgress = 1,
        Done = 2
    }

    public class TaskSL
    {
        [JsonInclude]
        public long Id { get; private set; }

        [JsonInclude]
        public DateTime CreationTime { get; private set; }

        [JsonInclude]
        public string Title { get; private set; }

        [JsonInclude]
        public string Description { get; private set; }

        [JsonInclude]
        public DateTime DueDate { get; private set; }

        [JsonInclude]
        public string Assignee { get; set; } // Email of the user assigned to the task

        // Used ONLY for real tasks
        internal TaskSL(TaskBL task)
        {
            this.Id = task.TaskID;
            this.CreationTime = task.CreationTime;
            this.Title = task.Title;
            this.Description = task.Description;
            this.DueDate = task.Due_date;
            this.Assignee = task.Assignee;
        }

        // Used ONLY in test: Test_DeleteTask_NotExist
        public TaskSL(string title, string description, DateTime dueDate)
        {
            this.Id = -999; // only for tests
            this.CreationTime = DateTime.Now;
            this.Title = title;
            this.Description = description;
            this.DueDate = dueDate;
            this.Assignee = null;
        }

        // Needed for deserialization
        public TaskSL() { }
    }
}
