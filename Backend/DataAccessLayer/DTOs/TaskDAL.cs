using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]



namespace IntroSE.Kanban.Backend.DataAccessLayer.DTOs
{
    internal class TaskDAL
    {
        public const string TaskIDColumnName = "taskID";
        public const string BoardIDColumnName = "boardID";
        public const string TypeColumnName = "type";
        public const string TitleColumnName = "title";
        public const string DescriptionColumnName = "description";
        public const string DueDateColumnName = "dueDate";
        public const string CreationTimeColumnName = "creationTime";
        public const string AssigneeColumnName = "assignee";

        private long taskID;
        private long boardID;
        private string type;
        private string title;
        private string description;
        private DateTime dueDate;
        private DateTime creationTime;
        private string assignee;
        private TaskController controller;
        private bool isPersisted = false;

        public TaskDAL(long taskID, long boardID, string type, string title, string description, DateTime dueDate, DateTime creationTime, string assignee, TaskController tc)
        {
            this.taskID = taskID;
            this.boardID = boardID;
            this.type = type;
            this.title = title;
            this.description = description;
            this.dueDate = dueDate;
            this.creationTime = creationTime;
            this.assignee = assignee;
            this.controller = tc;
        }
        public bool IsPersisted { get => isPersisted; set { isPersisted = value; } }
        public long TaskID => taskID;
        public long BoardID => boardID;
        public string Type
        {
            get => type;
            set
            {
                if (!isPersisted || !controller.UpdateType(boardID, taskID, value))
                {
                    throw new ArgumentException($"Task {taskID} in board - {boardID} is not persisted or failed to update type.");
                }

                type = value;
            }
        }

        public string Title
        {
            get => title; 
            set
            {
                if (!isPersisted || !controller.UpdateTitle(boardID, taskID, value))
                {
                    throw new ArgumentException($"Task {taskID} in board - {boardID} is not persisted or failed to update title.");
                }

                title = value;
            }
        }

        public string Description 
        {
            get => description; 
            set
            {
                if (!isPersisted || !controller.UpdateDescription(boardID, taskID, value))
                {
                    throw new ArgumentException($"Task {taskID} in board - {boardID} is not persisted or failed to update description.");
                }

                description = value;
            }
        }

        public DateTime DueDate 
        {
            get => dueDate; 
            set
            {
                if (!isPersisted || !controller.UpdateDueTime(boardID, taskID, value))
                {
                    throw new ArgumentException($"Task {taskID} in board - {boardID} is not persisted or failed to update due date.");
                }

                dueDate = value;
            }
        }

        public DateTime CreationTime { get => creationTime; }

        public string Assignee 
        {
            get => assignee; 
            set
            {
                if (!isPersisted || !controller.UpdateAssignee(boardID,taskID, value))
                {
                    throw new ArgumentException($"Task {taskID} in board - {boardID} is not persisted or failed to update assignee.");
                }

                assignee = value;
            }
        } 

        /// <summary>
        /// Saves the task to the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Save()
        {
            if (isPersisted)
            {
                throw new Exception("Task is already persisted.");
            }

            if (!controller.AddTask(this))
            {
                throw new Exception("Failed to save task to the database.");
            }

            isPersisted = true;
        }

        /// <summary>
        /// Deletes the task from the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Delete()
        {
            if (!isPersisted)
            {
                throw new Exception($"Task {taskID} in board {boardID} is not persisted; cannot delete.");
            }

            if (!controller.DeleteTask(boardID, taskID))
            {
                throw new Exception($"Failed to delete task {taskID} in board {boardID} from DB.");
            }

            isPersisted = false;
        }
    }
}
