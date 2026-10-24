using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IntroSE.Kanban.Backend.BuisnessLayer;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using IntroSE.Kanban.Backend.ServiceLayer;
using log4net;

namespace Backend.BuisnessLayer
{
    internal class BoardBL
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private int MAXIMUM_TITLE_LENGTH = 50; // maximum length of a title
        // name of the Columns in the board
        private const string BACKLOG = "backlog"; //First column
        private const string IN_PROGRESS = "in progress"; //Second column
        private const string DONE = "done"; //Third column
        

        public Dictionary<string, ColumnBL> columns { get;} //manages the columns in a board
        private string board_name;
        long boardID;
        public Collaborators collaborators;
        private long taskIDGiver;
        private string boardOwner;
        private readonly TaskController taskController;
        private readonly ColumnController columnController;
        private BoardDAL boardDAL; // For database operations


        public BoardBL(string board_name, long board_ID, string boardOwner, TaskController taskController, ColumnController columnController, CollaboratorsController collaboratorsController, BoardController bc)
        {
            BoardOwner = boardOwner.ToLower();
            BoardName = board_name.ToLower();
            this.boardID = board_ID;

            this.boardDAL = new BoardDAL(boardID, BoardName, BoardOwner, bc);
            this.boardDAL.Save(); // Persist the board to the database
            this.collaborators = new Collaborators(board_ID, collaboratorsController); //Initialize collaborators
            this.collaborators.AddCollaborator(boardOwner.ToLower()); //Add the board owner as a collaborator
            this.taskController = taskController;
            this.columnController = columnController;
            this.columns = new Dictionary<string, ColumnBL>();
            this.columns[BACKLOG] = new ColumnBL(boardID, BACKLOG, taskController, columnController);
            this.columns[IN_PROGRESS] = new ColumnBL(boardID, IN_PROGRESS, taskController, columnController);
            this.columns[DONE] = new ColumnBL(boardID, DONE, taskController, columnController);
            this.taskIDGiver = 0;

            
            Log.Info($"Created a new board named: {board_name}");
        }

        public BoardBL(BoardDAL dal, TaskController taskController, ColumnController columnController, CollaboratorsController collabController, BoardController bc)
        {
            // Copy fields without saving:
            this.boardDAL = dal;
            this.boardID = dal.BoardID;
            this.BoardName = dal.BoardName;
            this.BoardOwner = dal.Owner;
            this.taskController = taskController;
            this.columnController = columnController;
            this.collaborators = new Collaborators(boardID, collabController);
            this.columns = new Dictionary<string, ColumnBL> {
                  { "backlog",     new ColumnBL(columnController.SelectColumn(boardID, "backlog"),     taskController, columnController) },
                  { "in progress", new ColumnBL(columnController.SelectColumn(boardID, "in progress"), taskController, columnController) },
                  { "done",        new ColumnBL(columnController.SelectColumn(boardID, "done"),        taskController, columnController) }
                };

            int taskSum = 0; // Initialize taskSum to 0
            foreach (var column in columns.Values)
            {
                taskSum += column.GetAllTasks().Count;
            }
            this.taskIDGiver = taskSum; // TODO: Initialize this properly based on existing tasks if needed

            Log.Info($"Loaded board '{BoardName}' from DB without persisting.");
        }
        public long BoardID { get => boardID; }
        public Collaborators Collaborators
        {
            get => collaborators;
            private set => collaborators = value;
        }
        public string BoardOwner
        {
            get => boardOwner;
            private set
            {
                if (boardDAL != null)
                {
                    boardDAL.Owner = value.ToLower(); // Ensure owner is stored in lowercase
                }

                this.boardOwner = value.ToLower();
            }
        }

        public string BoardName
        {
            get => board_name;
            set
            {
                if (value.Length < 1 || string.IsNullOrWhiteSpace(value))
                {
                    Log.Error("Invalid board name.");
                    throw new Exception("Invalid board name.");
                }

                if (value.Length > MAXIMUM_TITLE_LENGTH)
                {
                    Log.Error("Board name is too long, maximum length is 50 characters.");
                    throw new Exception("Board name is too long, maximum length is 50 characters.");
                }

                if (boardDAL != null)
                {
                    boardDAL.BoardName = value.ToLower(); // Ensure board name is stored in lowercase
                }

                board_name = value.ToLower();
            }
        }

        /// <summary>
        /// Creates a new Task in the given column.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="due_date"></param>
        public TaskBL CreateTask(int columnNum, string title, string description, DateTime due_date)
        {
            
            string column = GetColumnTitle(columnNum);
            TaskBL newTask = new TaskBL(this.boardID, taskIDGiver, title, description, due_date, taskController);
            //if we get here without throwing exception the task is persisted
            columns[column].AddTask(newTask);
            taskIDGiver = taskIDGiver + 1;
            Log.Info($"Created a new task in '{board_name}' and in column:{column} with TaskID:'{taskIDGiver}'"); // Logging new tasks with board name and taskID
            return newTask;
        }

        /// <summary>
        /// Deletes the task with the given taskID in the given column.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="taskID"></param>
        public void DeleteTask(int columnNum, long taskID)
        {
            string column = GetColumnTitle(columnNum);

            if (columns.ContainsKey(column))
            {
                if (columns[column].GetAllTasks().ContainsKey(taskID))
                {

                    columns[column].DeleteTask(this.boardID, taskID);
                    Log.Info($"Removed Task:'{taskID}' from {column}.");
                }
                else
                {
                    Log.Error($"No such task:'{taskID}' exists in {column}.");
                    throw new Exception($"No such task exists in {column}, please create one");
                }
            }
        }

        /// <summary>
        /// Updates(Advances) the task with the same title as the given one in the given column to the next column.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="task"></param>
        /// <returns>returns true if updated the given task in the given column, else throws an Exception</returns>
        public bool UpdateTask(string assignee, int columnNum, TaskBL task)
        {
            string currentColumn = GetColumnTitle(columnNum);

            if (task.Assignee != null && !task.Assignee.Equals(assignee))
            {
                Log.Error($"{assignee} is not the assigned member for {task.TaskID}");
                throw new Exception($"Cannot update {task.TaskID} as you are not the assignee.");
            }

            if (currentColumn.Equals("done"))
            {
                Log.Error("Cannot move task: already in Done column");
                throw new Exception("Cannot move task: already in done column");
            }

            string nextColumn = GetColumnTitle(columnNum + 1);

            if (!columns.ContainsKey(currentColumn) || !columns.ContainsKey(nextColumn))
                throw new Exception("Invalid column transition");

            if (!columns[currentColumn].GetAllTasks().ContainsKey(task.TaskID))
                throw new Exception($"Task {task.TaskID} not found in column {currentColumn}");

            if (!columns[nextColumn].CanAddTask())
                throw new Exception($"Cannot move task: column {nextColumn} is full");

            columns[currentColumn].DeleteTask(this.boardID, task.TaskID);
            TaskBL updatedTask = new TaskBL(this.boardID, task.TaskID, task.Title, task.Description, task.Due_date, taskController);
            columns[nextColumn].AddTask(updatedTask); // Move the actual task object
            updatedTask.Type = nextColumn; // Update the task type to the new column

            Log.Info($"Moved task {task.TaskID} from {currentColumn} to {nextColumn}");
            return true;
        }

        /// <summary>
        /// Limits the amount of tasks that can be in a given column by 'limit'.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool LimitTaskInColumn(int columnNum, int limit)
        {
            string column = GetColumnTitle(columnNum); //returns the column name based on the index

            if (columns[column].ColumnLimit > limit)
            {
                Log.Error("Task limit cannot be less than current number of tasks.");
                throw new Exception("Task limit cannot be less than current number of tasks.");
            }

            columns[column].ColumnLimit = limit; //sets the limit for the column
            Log.Info($"Created new task limit:{limit}");
            return true;
        }


        /// <summary>
        /// Retrieves the column with the specified name from the board.
        /// </summary>
        /// <param name="columnName">The name of the column to retrieve.</param>
        /// <returns>A dictionary containing the tasks in the specified column.</returns>
        /// <exception cref="Exception">Thrown if the column does not exist.</exception>
        public Dictionary<long, TaskBL> GetColumn(string columnName)
        {
            if (!columns.ContainsKey(columnName))
            {
                Log.Error($"Column named {columnName} does not not exist");
                throw new Exception($"Column '{columnName}' does not exist.");
            }
            Log.Info($"Returned column named {columnName}.");
            return columns[columnName].GetAllTasks();

        }


        /// <summary>
        /// Returns a given column to a List.
        /// </summary>
        /// <param name="columnNum"></param>
        /// <returns></returns>
        public List<TaskBL> GetColumnAsList(int columnNum)
        {
            if (columnNum < 0 || columnNum > 2)
            {
                Log.Error($"Invalid column index: {columnNum}");
                throw new Exception("Invalid column index.");
            }

            string asString = GetColumnTitle(columnNum);
            var column = GetColumn(asString); // Get the column by its name
            if (column == null)
            {
                Log.Error($"Column {asString} does not exist.");
                throw new Exception($"Column {asString} does not exist.");
            }
            return GetColumn(asString).Values.ToList();
        }


        /// <summary>
        /// Returns a column name based on its index.
        /// </summary>
        /// <param name="numOfColumn"></param>
        /// <returns></returns>
        public string GetColumnTitle(int numOfColumn)
        {
            if (numOfColumn < 0 || numOfColumn > 2)
                throw new Exception("Invalid column index.");

            string c = "";
            c = numOfColumn switch //makes sure that between enum/int use and real column names we won't get issue
            {
                0 => BACKLOG,
                1 => IN_PROGRESS,
                2 => DONE,
                _ => c
            };
            Log.Info($"Retrieved column titled{c}");
            return c;
        }


        /// <summary>
        /// Returns a column task limit based on column index.
        /// </summary>
        /// <param name="numOfColumn"></param>
        /// <returns></returns>
        public int GetColumnLimit(int numOfColumn)
        {
            if (numOfColumn < 0 || numOfColumn > 2)
                throw new Exception("Invalid column index.");

            string column = GetColumnTitle(numOfColumn);
            Log.Info($"Retrieved column limit of {numOfColumn}.");
            return columns[column].ColumnLimit; //returns -1 if no limit has been defined
        }

        /// <summary>
        /// Retrieves the ColumnBL object for a given column index.
        /// </summary>
        /// <param name="numOfColumn"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ColumnBL GetColumnBL(int numOfColumn)
        {
            if (numOfColumn < 0 || numOfColumn > 2)
                throw new Exception("Invalid column index.");

            string column = GetColumnTitle(numOfColumn);

            if (!columns.ContainsKey(column))
            {
                Log.Error($"Column {column} does not exist.");
                throw new Exception($"Column {column} does not exist.");
            }

            Log.Info($"Retrieved ColumnBL for column {column}.");
            return columns[column];
        }

        /// <summary>
        /// Changes board owner if met neccessary requirement.
        /// </summary>
        /// <param name="pastOwner"></param>
        /// <param name="newOwner"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool ChangeBoardOwner(string pastOwner, string newOwner)
        {
            if (pastOwner.Equals(boardOwner.ToLower()))
            {
                BoardOwner = newOwner.ToLower(); // Update the board owner in memory and database
                Log.Info($"Change {boardID} owner from {boardOwner} to {newOwner}.");

                return true;
            }

            Log.Error($"Change {boardID} owner from {boardOwner} to {newOwner} failed.");
            throw new Exception($"{pastOwner} is not the current owner of {BoardID}.");
        }

        /// <summary>
        /// Adds a new collaborator to the board.
        /// </summary>
        /// <param name="newCollaborator"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void AddCollaborator(string newCollaborator)
        {
            collaborators.AddCollaborator(newCollaborator);
        }

        /// <summary>
        /// Removes a collaborator from the board.
        /// </summary>
        /// <param name="removeCollaborator"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void RemoveCollaborator(string removeCollaborator)
        {
            if(removeCollaborator.ToLower().Equals(boardOwner))
            {
                Log.Error("Cannot remove the board owner as a collaborator.");
                throw new Exception("Board owner cannot leave the board.");
            }
            foreach (var column in columns.Values)
            {
                foreach (var task in column.GetAllTasks().Values)
                {
                    if (task.Assignee != null && task.Assignee.Equals(removeCollaborator))
                    {
                        task.Assignee = null;  // Unassign the task
                        Log.Info($"Task {task.TaskID} on board {BoardID} unassigned from {removeCollaborator}.");
                    }
                }
            }
            collaborators.RemoveCollaborator(removeCollaborator);
        }

        /// <summary>
        /// Checks if a user is a collaborator on the board.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool IsCollaborator(string email)
        {
            return collaborators.IsCollaborator(email);
        }

        /// <summary>
        /// Returns the task with the given taskID from any column in the board.
        /// </summary>
        /// <param name="taskID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TaskBL GetTask(long taskID)
        {
            foreach (var column in columns.Values)
            {
                if (column.GetAllTasks().ContainsKey(taskID))
                {
                    return column.GetAllTasks()[taskID];
                }
            }

            // If we reach here, the task was not found in any column
            Log.Error($"Task {taskID} not found in any column on board {boardID}.");
            throw new Exception($"Task {taskID} not found on board {boardID}.");
        }   

        /// <summary>
        /// Assigns a task to a new assignee.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="newAssignee"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void AssignTask(string email, TaskBL task, string newAssignee)
        {
            if (task.Assignee.Equals(email))
            {
                task.Assignee = newAssignee;
                Log.Info($"Task {task.TaskID} assigned to {newAssignee} by {email}.");
            }
            else
            {
                Log.Error($"Task {task.TaskID} is not assigned to {email}, cannot assign to {newAssignee}.");
                throw new Exception($"Task {task.TaskID} is not assigned to you, cannot assign to {newAssignee}.");
            }

        }

        /// <summary>
        /// Rtrieves all tasks in the in progress column.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<TaskBL> GetAllInProgressTasks()
        {
            return columns[IN_PROGRESS].GetAllTasks().Values.ToList();
        }

        /// <summary>
        /// Adds a column to the board from a ColumnDAL object.
        /// </summary>
        /// <param name="dal"></param>
        public void AddColumnFromDAL(ColumnDAL dal)
        {
            if (!columns.ContainsKey(dal.Type))
            {
                ColumnBL colBL = new ColumnBL(dal, taskController, columnController);
                columns[dal.Type] = colBL;
                Log.Info($"Added column '{dal.Type}' (limit: {dal.ColumnLimit}) to board {dal.BoardID} from DB.");
            }
        }

        /// <summary>
        /// Adds a task to the board from a TaskDAL object.
        /// </summary>
        /// <param name="dal"></param>
        public void AddTaskFromDAL(TaskDAL dal)
        {
            var taskBL = new TaskBL(dal,taskController);
            if (dal.TaskID > taskIDGiver) // TODO: updated it to work better
                taskIDGiver = dal.TaskID; // Update taskIDGiver if this task has a higher ID
            columns[dal.Type].AddTask(taskBL);
            Log.Info($"Added task {dal.TaskID} to column '{dal.Type}' on board {dal.BoardID} from DB.");
        }

        /// <summary>
        /// Adds a collaborator to the board from a CollaboratorDAL object.
        /// </summary>
        /// <param name="dal"></param>
        public void AddCollaboratorFromDAL(CollaboratorDAL dal)
        {
            if (!collaborators.IsCollaborator(dal.Email.ToLower()))
            {
                collaborators.AddColaboratorWithoutPersisting(dal.Email.ToLower());
                //Comes from DB so no need to persist when adding
                Log.Info($"Added collaborator '{dal.Email}' to board {dal.BoardID} from DB.");
            }
        }

        /// <summary>
        /// Updates task Giver to the highest taskID here after loading from db
        /// </summary>
        public void UpdateTaskIDGiver()
        {
            taskIDGiver = columns
                .SelectMany(c => c.Value.GetAllTasks().Keys)
                .DefaultIfEmpty(-1)  // So that Max() will be -1 if no tasks
                .Max() + 1;
        }

        /// <summary>
        /// Deletes the board and all its associated columns and collaborators from the database and memory.
        /// </summary>
        public void Delete()
        {
            // Delete all columns
            foreach (var col in columns.Values)
            {
                col.Delete();  // Each ColumnBL handles its DB delete + in-memory clear
                columns.Remove(col.Type);
                //deletes all tasks in every column as well
            }

            // Delete all collaborators
            collaborators.DeleteCollaboratorsForBoard(boardID);  

            // Delete the board itself
            boardDAL.Delete();

            Log.Info($"Board {boardID} deleted, including columns and collaborators.");
        }

        /// <summary>
        ///     Checks if a task with the given taskID is done.
        /// </summary>
        /// <param name="taskID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool IsTaskDone(long taskID)
        {
            foreach (var col in columns)
            {
                if (col.Value.GetAllTasks().ContainsKey(taskID))
                {
                    return col.Key.Equals("done");
                }
            }
            throw new Exception($"Task {taskID} not found on this board.");
        }
    }
}
