using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Backend.BuisnessLayer;
using Backend.ServiceLayer;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;

namespace IntroSE.Kanban.Backend.BuisnessLayer
{
    internal class ColumnBL
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ColumnController controller;
        private readonly TaskController taskController;
        private readonly long boardID;
        public string Type {  get;}
        public Dictionary<long, TaskBL> tasks;
        public int columnLimit; // limit for the column, -1 means no limit
        private ColumnDAL dal; // For database operations
        public Dictionary<long, TaskBL> Tasks { get => tasks; }
        public int ColumnLimit { get => columnLimit;
            set
            {
                if (value >= -1)
                {
                    if (value < tasks.Count)
                    {
                        Log.Error("Invalid limit, new limit should be higher than current tasks count.");
                        throw new Exception("Invalid limit, new limit should be higher than current tasks count.");
                    }
                    if(value == 0)
                    {
                        Log.Error("Invalid limit, new limit should be -1 or a positive integer.");
                        throw new Exception("Invalid limit, new limit should be -1 or a positive integer.");
                    }

                    if (dal != null)
                    {
                        dal.ColumnLimit = value; // Update the DAL object with the new limit, if doesn't work will throw an exception before returning
                    }

                    columnLimit = value;
                    Log.Info($"Column limit set to {columnLimit} for column {Type} on board {boardID}.");
                }
                else
                {
                    Log.Error("Invalid limit, should be -1 or a non-negative integer.");
                    throw new Exception("Invalid limit, should be -1 or a positive integer.");
                }
            }
        }

        public ColumnBL(long boardID, string type, TaskController taskController, ColumnController columnController)
        {
            
            this.boardID = boardID;
            Type = type;
            this.tasks = new Dictionary<long, TaskBL>();
            this.columnLimit = -1; // default no limit
            this.taskController = taskController;
            this.controller = columnController;
            dal = new ColumnDAL(boardID, type, -1, columnController);
            dal.Save();

            Log.Info($"Created column {type} for board {boardID}, loaded {tasks.Count} tasks."); 
        }

        public ColumnBL(ColumnDAL dal, TaskController taskController, ColumnController columnController)
        { //constructor for loading from DB
            this.dal = dal;
            this.boardID = dal.BoardID;
            Type = dal.Type;
            this.columnLimit = dal.ColumnLimit;
            this.tasks = new Dictionary<long, TaskBL>();
            this.taskController = taskController;
            this.controller = columnController;
        }

        /// <summary>
        /// Adds a task to the column.
        /// </summary>
        /// <param name="task"></param>
        public void AddTask(TaskBL task)
        {
            if (columnLimit != -1 && tasks.Count >= columnLimit)
            {
                Log.Error($"Column has reached its task limit.");
                throw new Exception("Column has reached its task limit");
            }

            tasks.Add(task.TaskID, task);
            Log.Info($"Task {task.TaskID} added to column {Type} on board {boardID}.");
        }

        /// <summary>
        /// Deletes a task from the column.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public bool DeleteTask(long boardID, long taskId)
        {
            if (!tasks.ContainsKey(taskId))
            {
                Log.Error($"Task {taskId} not found in column {Type} for board {boardID}.");
                throw new Exception("Task not found in this column.");
            }

            TaskBL taskbl = GetTask(taskId);
            bool deleted = taskbl.Delete(); // This will call the DAL's Delete method

            if (deleted)
            {
                tasks.Remove(taskId);
                Log.Info($"Task {taskId} removed from column {Type} on board {boardID}.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a task from the column by its ID.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public TaskBL GetTask(long taskId)
        {
            return tasks.ContainsKey(taskId) ? tasks[taskId] : null;
        }

        /// <summary>
        /// Retrieves all tasks in the column as a Dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<long, TaskBL> GetAllTasks()
        {
            return tasks;
        }

        /// <summary>
        /// Checks if a task can be added to the column based on the limit.
        /// </summary>
        /// <returns></returns>
        public bool CanAddTask()
        {
            return columnLimit == -1 || tasks.Count < columnLimit;
        }

        /// <summary>
        /// Deletes the column and all its tasks from the database.
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            // Delete all tasks in this column first
            foreach (var task in tasks.Values.ToList())  // ToList to avoid collection modification during iteration
            {
                task.Delete();  // Assuming TaskBL has a Delete() that calls its DAL
            }

            // Now delete the column itself
            dal.Delete();

            if (!dal.IsPersisted)
            {
                Log.Info($"Column '{Type}' on board {boardID} deleted successfully.");
                return true;
            }
            else
            {
                Log.Error($"Column '{Type}' on board {boardID} deletion may have failed — DAL still marked as persisted.");
                return false;
            }

        }
    }
}
