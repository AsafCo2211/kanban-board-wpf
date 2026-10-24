using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Backend.ServiceLayer;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;

namespace Backend.BuisnessLayer
{   internal class TaskBL
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly long taskID;
        private readonly long boardID;
        private readonly DateTime creationTime;

        private string type = "backlog"; // Default type is "backlog"
        private string title;
        private string description;
        private DateTime due_date;
        private string assignee;
        private TaskDAL taskDAL; // For database operations
        private TaskController taskController; // For task operations

        // Magic numbers
        private int MIN_TITLE_LENGTH = 1;
        private int MAX_TITLE_LENGTH = 50;
        private int MAX_DESC_LENGTH = 300;
        

        public TaskBL(long boardID, long taskID, string title, string description, DateTime due_date, TaskController taskController) { 

            if (boardID < 0 || taskID < 0)
            {
                Log.Error("Board ID and Task ID must be non-negative.");
                throw new Exception("Board ID and Task ID must be non-negative.");
            }

            this.creationTime = DateTime.Now;
            Title = title;
            Description = description;
            Due_date = due_date;
            this.boardID = boardID;
            this.taskID = taskID;
            this.assignee = null;
            this.taskController = taskController;
            taskDAL = new TaskDAL(taskID, boardID, type, title, description, due_date, DateTime.Now, null, taskController);
            taskDAL.Save();
            Log.Info($"Created a new Task with id {taskID}");
        }

        public TaskBL(TaskDAL taskDAL, TaskController taskController)
        {
            //loading from db
            this.taskDAL = taskDAL;
            taskDAL.IsPersisted = true;
            this.boardID = taskDAL.BoardID;
            this.taskID = taskDAL.TaskID;
            //taskDAL = new TaskDAL(taskID, boardID, type, title, description, due_date, DateTime.Now, null, taskController);           
            this.title = taskDAL.Title;
            this.description = taskDAL.Description;
            this.due_date = taskDAL.DueDate;
            this.assignee = taskDAL.Assignee;
            this.creationTime = taskDAL.CreationTime;
            this.taskController = taskController;
            this.type = taskDAL.Type; // Load type from DAL
            
        }

        public long TaskID { get => taskID;}

        public DateTime CreationTime => creationTime;

        public string Title
        {
            get => title;
            set
            {
                ValidTitle(value);
                if (title != value)
                {
                    if (taskDAL != null)
                    {
                        taskDAL.Title = value; // Update the title in the DAL
                    }

                    title = value;
                    Log.Info($"Task {taskID} title updated to '{value}'");
                }
            }
        }

        public string Description
        {
            get => description;
            set
            {
                ValidDesc(value);
                if (description != value)
                {
                    if (taskDAL != null)
                    {
                        taskDAL.Description = value; // Update the title in the DAL
                    }

                    description = value;
                    Log.Info($"Task {taskID} description updated");
                }
            }
        }
        public DateTime Due_date
        {
            get => due_date;
            set
            {
                ValidDueDate(value);
                if (due_date != value)
                {
                    if (taskDAL != null)
                    {
                        taskDAL.DueDate = value; // Update the due date in the DAL
                    }

                    due_date = value;
                    Log.Info($"Task {taskID} due date updated to {value}");
                }
            }
        }

        public string Assignee
        {
            get => assignee;
            set
            {
                if (assignee != value)
                {
                    taskDAL.Assignee = value; // Update the title in the DAL
                    assignee = value;
                    Log.Info($"Task {taskID} assigned to {value}");
                }
            }
        }

        public string Type
        {
            get => type;
            set
            {
                if (type != value)
                {
                    if (taskDAL != null)
                    {
                        taskDAL.Type = value; // Update the type in the DAL
                    }
                    type = value;
                    Log.Info($"Task {taskID} type updated to '{value}'");
                }
            }
        }


        /// <summary>
        /// Checks if a given title is matching the given criteria of between the minimum and maximum length and appropriate characters.
        /// </summary>
        /// <param name="title"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length < MIN_TITLE_LENGTH || title.Length > MAX_TITLE_LENGTH)
            {
                Log.Error($"Invalid title '{title}'.");
                throw new ArgumentException("Invalid title, must be between 1-50 characters and not white spaces.");
            }
        }


        /// <summary>
        /// Checks if a given description is up to the standards of the given criteria.
        /// </summary>
        /// <param name="description"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidDesc(string description)
        {
            if (description == null || description.Length > MAX_DESC_LENGTH)
            {
                Log.Error($"Invalid description: {description}");
                throw new ArgumentException("Description must be non-empty and at most 300 characters.");
            }
        }

        /// <summary>
        /// Checks if the given date is not in past time as it violates a legitimate due date.
        /// </summary>
        /// <param name="date2"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidDueDate(DateTime date2)
        {
            if (date2 < creationTime)
            {
                Log.Error($"Invalid due date {date2} for task {TaskID}, cannot be before creation time {creationTime}.");
                throw new Exception("cannot change creation");
            }

            if (date2 < DateTime.Now)
            {
                Log.Error($"Invalid due date {date2} for task {TaskID}, cannot be before creation time {DateTime.Now}.");
                throw new Exception("Due date cannot be in the past.");
            }
        }

        /// <summary>
        /// Converts the TaskBL object to a TaskDAL object for database operations.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public TaskDAL ToDAL(long boardID, string type)
        {
            return new TaskDAL(
                this.TaskID,
                boardID,
                type,
                this.Title,
                this.Description,
                this.Due_date,
                this.CreationTime,
                this.Assignee,
                taskController
                );
        }

        /// <summary>
        /// Deletes the task from the database and updates the in-memory state.
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            try
            {
                taskDAL.Delete();

                if (!taskDAL.IsPersisted)
                {
                    Log.Info($"Task {TaskID} on board {boardID} deleted successfully.");
                    return true;
                }
                else
                {
                    Log.Error($"Task {TaskID} on board {boardID} deletion may have failed — DAL still marked as persisted.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to delete task {TaskID} on board {boardID}.", ex);
                return false;
            }
        }


    }
}
