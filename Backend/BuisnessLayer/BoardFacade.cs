using Backend.ServiceLayer;
using IntroSE.Kanban.Backend.BuisnessLayer;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using IntroSE.Kanban.Backend.ServiceLayer;
using log4net;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace Backend.BuisnessLayer
{
    internal class BoardFacade
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, HashSet<long>> usersBoardsID; // holds all the board ID's of each user based on their email
        private readonly Dictionary<long, BoardBL> boards; // manages all the boards based on their ID's
        private readonly AuthenticationFacade authFacade;
        private readonly BoardController boardController;
        private readonly ColumnController columnController;
        private readonly TaskController taskController;
        private readonly CollaboratorsController collabController;
        private long allTimeBoardCounter;

        public BoardFacade(AuthenticationFacade authFacade)
        {
            this.usersBoardsID = new Dictionary<string, HashSet<long>>(); //email as key and values is a Hashset of boardId's where user is collab in
            this.boards = new Dictionary<long, BoardBL>();
            this.authFacade = authFacade;

            this.allTimeBoardCounter = 0;

            this.boardController = new BoardController();
            this.taskController = new TaskController();
            this.columnController = new ColumnController(taskController);
            this.collabController = new CollaboratorsController();  
        }

        /// <summary>
        /// Creates a new board for the specified user (if the user is registered, logged in, and no other board with the same name exists).
        /// Upon creation, the board is assigned a unique ID and associated under the user’s email in usersBoardsID.
        /// </summary>
        /// <param name="email">The user creating the board.</param>
        /// <param name="boardName">The name of the board to be created (must be unique per user).</param>
        /// <param name="limitTasks">Optional limit on the number of tasks in the board. Default is -1 (no limit).</param>
        /// <exception cref="Exception">Thrown if user is invalid, not registered, not logged in, or board name already exists.</exception>
        public BoardBL CreateBoard(string inputmail, string boardName)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            boardName = boardName.ToLower(); // normalize board name to lowercase

            ValidateActiveUser(email);
            BoardExists(email, boardName); 

            long boardID = allTimeBoardCounter++; // use the current counter as the board ID
            BoardBL newBoard = new BoardBL(boardName, boardID, email, taskController, columnController, collabController, boardController);
            boards[newBoard.BoardID] = newBoard;

            if (!usersBoardsID.ContainsKey(email))
                usersBoardsID[email] = new HashSet<long>();

            usersBoardsID[email].Add(boardID);

            //allTimeBoardCounter += 1; // increment the counter for the next board
            Log.Info($"Board '{boardName}' created successfully.");
            return newBoard;
        }


        /// <summary>
        /// Validates that the given user exists and is currently logged in.
        /// </summary>
        /// <param name="email">The user to validate.</param>
        /// <exception cref="Exception">Thrown if the user is invalid, not registered, or not logged in.</exception>
        private void ValidateActiveUser(string email)
        {
            if (email == null || string.IsNullOrWhiteSpace(email))
            {
                Log.Error("Invalid email inserted");
                throw new Exception("Invalid user.");
            }

            /*if (!authFacade.IsUserExist(email))
            {
                Log.Error($"No such user with {email} exists.");
                throw new Exception("User does not exist.");
            }*/

            if (!authFacade.IsLoggedIn(email))
            {
                Log.Error($"{email} is not logged in.");
                throw new Exception("User is not logged in.");
            }
        }

        /// <summary>
        /// Checks if a board with the specified name already exists for the given user.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <returns></returns>
        private void BoardExists(string email, string boardName)
        {
            if (string.IsNullOrWhiteSpace(boardName))
            {
                Log.Error("Invalid board name.");
                throw new Exception("Invalid board name.");
            }

            if (boards.Count != 0 && boards.Values.Any(b => b.BoardName.Equals(boardName.ToLower()) && b.IsCollaborator(email.ToLower())))
            {
                Log.Error($"Board with name '{boardName}' already exists for user '{email}'.");
                throw new Exception("Board with this name already exists.");
            }
        }


        /// <summary>
        /// Deletes the specified board (if it exists and the caller is its owner).
        /// All users (owner and collaborators) who had this board will have its ID removed from their board‐sets.
        /// </summary>
        /// <param name="email">The user who owns the board.</param>
        /// <param name="boardID">The ID of the board to delete.</param>
        /// <exception cref="Exception">
        /// Thrown if the user does not exist in the dictionary or the board ID is not found.
        /// </exception>
        public void DeleteBoard(string inputmail, string boardName)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase

            ValidateActiveUser(email);
            
            BoardBL board = GetBoard(email, boardName.ToLower());
            string boardOwner = board.BoardOwner;
            // Check ownership
            if (!boardOwner.Equals(email))
            {
                Log.Error($"User '{email}' is not the owner of board '{boardName}'.");
                throw new Exception("not the owner");
            }

            board.Delete(); // delete the board from the DB

            foreach (var userBoards in usersBoardsID.Values)
                userBoards.Remove(board.BoardID);

            boards.Remove(board.BoardID);

            Log.Info($"Deleted board '{boardName}' successfully.");
        }


        /// <summary>
        /// Returns every TaskBL in the “InProgress” column for all boards that <paramref name="email"/> participates in.
        /// </summary>
        /// <param name="email">The user whose “InProgress” tasks should be collected.</param>
        /// <returns>
        /// A List of all TaskBL objects currently in the “InProgress” column on every board where <paramref name="email"/> is a collaborator or owner.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if the user is not registered, not logged in, or if <paramref name="email"/> has no boards.
        /// </exception>
        public List<TaskBL> GetAllInProgressTasks(string inputmail)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase

            ValidateActiveUser(email);
               
            var tasks = usersBoardsID[email].SelectMany(id => boards[id].GetAllInProgressTasks()).ToList();

            Log.Info($"Returned in-progress tasks for user '{email}'.");
            return tasks;
        }


        /// <summary>
        /// Returns the board with the specified name for the given user, if it exists and the user is a collaborator or owner.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <returns></returns>
        public BoardBL GetBoard(string inputmail, string boardName)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            if (!usersBoardsID.ContainsKey(email)) //makes sure we don't enter a unavailable key to usersBoardsID
            { 
                Log.Info($"Board '{boardName}' does not exist for user '{email}' or user is not a collaborator.");
                throw new Exception("does not exist");
            }

            foreach (var boardID in usersBoardsID[email])
            {
                if (boards[boardID].BoardName.Equals(boardName.ToLower()))
                {
                    if (boards[boardID].IsCollaborator(email))
                    {
                        Log.Info($"Retrieved board '{boardName}' for user '{email}'.");
                        return boards[boardID];
                    }
                }  
            }

            // If we reach here, the board does not exist for the user or they are not a collaborator
            Log.Info($"Board '{boardName}' does not exist for user '{email}' or user is not a collaborator.");
            throw new Exception("does not exist");
        }

        /// <summary>
        /// Returns the limit of tasks in column <paramref name="columnNum"/> for board <paramref name="boardName"/>, if <paramref name="email"/> is a collaborator or owner.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <param name="columnNum"></param>
        /// <returns></returns>
        public int GetColumnLimit(string inputmail, string boardName, int columnNum)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());

            Log.Info($"Retrieved column limit for column {columnNum} on board '{boardName}' for user '{email}'.");

            return board.GetColumnLimit(columnNum);
        }

        /// <summary>
        /// Returns the title (e.g., "Backlog", "InProgress", "Done") 
        /// of column <paramref name="columnNum"/> for board <paramref name="boardID"/>,
        /// if <paramref name="email"/> is a collaborator or owner.
        /// </summary>
        /// <param name="email">The user requesting the column name.</param>
        /// <param name="boardID">The ID of the board.</param>
        /// <param name="columnNum">Zero‐based index (0–2) of the column.</param>
        /// <returns>The string name of the specified column.</returns>
        /// <exception cref="Exception">
        /// Thrown if <paramref name="email"/> is invalid, or if the board/column doesn’t exist.
        /// </exception>
        public string GetColumnName(string inputmail, string boardName, int columnNum)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());

            Log.Info($"Retrieved column name for column {columnNum} on board '{boardName}' for user '{email}'.");
            return board.GetColumnTitle(columnNum);
        }


        /// <summary>
        /// Retrieves all TaskBL objects in column <paramref name="columnNum"/> 
        /// of board <paramref name="boardID"/>, as a List.
        /// </summary>
        /// <param name="email">The user requesting the column’s tasks.</param>
        /// <param name="boardID">The ID of the board.</param>
        /// <param name="columnNum">Zero‐based index (0=Backlog, 1=InProgress, 2=Done).</param>
        /// <returns>A List&lt;TaskBL&gt; containing each task currently in that column.</returns>
        /// <exception cref="Exception">
        /// Thrown if <paramref name="email"/> is not a valid collaborator, or if board/column is invalid.
        /// </exception>
        public List<TaskBL> GetColumnAsList(string inputmail, string boardName, int columnNum)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());

            Log.Info($"Retrieved column {columnNum} tasks for board '{boardName}' for user '{email}'.");    
            return board.GetColumnAsList(columnNum);
        }

        /// <summary>
        /// Changes the owner of a board to a new user.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardID"></param>
        /// <param name="newOwner"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool ChangeBoardOwner(string inputmail, string boardName, string newOwner)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());

            board.ChangeBoardOwner(email, newOwner.ToLower());
            usersBoardsID[newOwner.ToLower()].Add(board.BoardID);

            Log.Info($"Board {board.BoardID} owner changed to {newOwner}.");
            return true;
        }

        /// <summary>
        /// Checks if a given user is a collaborator on a specific board.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardID"></param>
        /// <param name="collaboratorEmail"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool IsCollaborator(long boardID, string email)
        {
            email = email.ToLower(); // normalize email to lowercase
            var board = GetBoardByID(boardID);

            return board.IsCollaborator(email);
        }

        /// <summary>
        /// Limits the number of tasks in a specific column of a board.
        /// Takes any limit as long as its bigger than the number of tasks in that column or -1 for no limit.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <param name="columnNum"></param>
        /// <param name="newLimit"></param>
        /// <exception cref="Exception"></exception>
        public void LimitTaskInColumn(string inputmail, string boardName, int columnNum, int newLimit)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());

            board.LimitTaskInColumn(columnNum, newLimit);
            Log.Info($"Set limit {newLimit} on column {columnNum} of board '{boardName}'.");
        }

        /// <summary>
        /// Adds a new task to the backlog column of the specified board.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="email"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="due_date"></param>
        public TaskBL CreateTask(string boardName, string inputmail, string title, string description, DateTime due_date)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());

            var task = board.CreateTask(0, title, description, due_date);
            
            Log.Info($"Created task '{task.TaskID}' on board '{boardName}'.");
            return task;
        }

        /// <summary>
        /// Deletes a task from the specified column of the board.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="columnNum"></param>
        /// <param name="email"></param>
        /// <param name="taskID"></param>
        public void DeleteTask(string boardName, int columnNum, string inputmail, long taskID)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());
          
            board.DeleteTask(columnNum, taskID);
            Log.Info($"Deleted task '{taskID}' from board '{boardName}', column {columnNum}.");
        }

        /// <summary>
        /// Edits the title of a task in the specified board.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="taskID"></param>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        public void EditTaskTitle(string title, long taskID, string inputmail, string boardName)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());
            var task = board.GetTask(taskID);

            if (board.IsTaskDone(taskID) || (task.Assignee != null && task.Assignee.Equals(email)))
            {
                task.Title = title;
                Log.Info($"Edited task '{taskID}' title to '{title}' on board '{boardName}'.");
            }
            else
            {
                Log.Error($"User {email} is not authorized to edit title of task {taskID}.");
                throw new Exception($"Cannot edit title, task {task.TaskID} as {email} is not the assignee.");
            }
        }

        /// <summary>
        /// Edits the description of a task in the specified board.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="taskID"></param>
        /// <param name="inputmail"></param>
        /// <param name="boardName"></param>
        public void EditTaskDescription(string description, long taskID, string inputmail, string boardName)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());
            var task = board.GetTask(taskID);

            if (board.IsTaskDone(taskID) || (task.Assignee != null && task.Assignee.Equals(email)))
            {
                task.Description = description;
                Log.Info($"Edited task '{taskID}' description to '{description}' on board '{boardName}'.");
            }
            else
            {
                Log.Error($"User {email} is not authorized to edit description of task {taskID}.");
                throw new Exception($"Cannot edit title, task {task.TaskID} as {email} is not the assignee.");
            }
        }

        /// <summary>
        /// Edits the due date of a task in the specified board.
        /// </summary>
        /// <param name="dueDate"></param>
        /// <param name="taskID"></param>
        /// <param name="inputmail"></param>
        /// <param name="boardName"></param>
        public void EditTaskDueDate(DateTime dueDate, long taskID, string inputmail, string boardName)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());
            var task = board.GetTask(taskID);

            // Disallow edits if task is done
            if (board.IsTaskDone(taskID))
            {
                Log.Error($"Attempt to edit due date of done task {taskID} by {email} on board {boardName}.");
                throw new Exception("Cannot modify a task that is already done.");
            }

            // Disallow edits if not assigned or assigned to someone else
            if (task.Assignee == null || !task.Assignee.Equals(email))
            {
                Log.Error($"User {email} tried to edit due date of task {taskID}, but is not the assignee.");
                throw new Exception($"Cannot edit due date, task {task.TaskID} can only be modified by its assignee.");
            }

            // Perform update
            task.Due_date = dueDate;
            Log.Info($"Edited task '{taskID}' due date to '{dueDate}' on board '{boardName}'.");
        }

        /// <summary>
        /// Advances a task to the next column in the specified board.
        /// </summary>
        /// <param name="inputmail"></param>
        /// <param name="boardName"></param>
        /// <param name="columnOrdinal"></param>
        /// <param name="taskID"></param>
        public void AdvanceTaskColumn(string inputmail, string boardName, int columnOrdinal, int taskID)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            var board = GetBoard(email, boardName.ToLower());
            TaskBL taskToAdvance = board.GetTask(taskID);
            board.UpdateTask(email, columnOrdinal, taskToAdvance);
            Log.Info($"Advanced task '{taskID}' to next column on board '{boardName}'.");
        }

        /// <summary>
        /// Assigns a task to a user by their email in the specified board and column.
        /// </summary>
        /// <param name="inputmail"></param>
        /// <param name="boardName"></param>                                                                                                                                                                                                                                                                                                                                                                        
        /// <param name="columnOrdinal"></param>
        /// <param name="taskID"></param>
        /// <param name="emailAssignee"></param>
        /// <exception cref="Exception"></exception>
        public void AssignTask(string inputmail, string boardName, int columnOrdinal, int taskID, string emailAssignee)
        {
            string email = inputmail.ToLower();
            string assignee = emailAssignee.ToLower();

            ValidateActiveUser(email);

            BoardBL board = GetBoard(email, boardName.ToLower());

            if (!IsCollaborator(board.BoardID, assignee))
            {
                Log.Error($"{assignee} is not a member of the board.");
                throw new Exception($"{assignee} is not a member of the board.");
            }

            ColumnBL column = board.GetColumnBL(columnOrdinal);

            TaskBL taskToAssign = column.GetTask(taskID);
            if (taskToAssign == null)
            {
                Log.Error($"Task with ID {taskID} does not exist in column {columnOrdinal} of board '{boardName}'.");
                throw new Exception("Task does not exist in this column.");
            }


            if (taskToAssign.Assignee == null)
            {
                taskToAssign.Assignee = assignee;
            }
            else if (taskToAssign.Assignee.Equals(email))
            {
                taskToAssign.Assignee = assignee;
            }
            else
            {
                Log.Error($"Task {taskID} is already assigned to someone else.");
                throw new Exception("Task is already assigned to someone else. Only the current assignee can reassign.");
            }
        }

        /// <summary>
        /// Adds a user as a collaborator to the specified board by its boardID.
        /// </summary>
        /// <param name="inputmail"></param>
        /// <param name="boardID"></param>
        /// <exception cref="Exception"></exception>
        public void JoinBoard(string inputmail, int boardID)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase

            ValidateActiveUser(email);

            if (!boards.ContainsKey(boardID))
            {
                Log.Error($"Board with ID {boardID} does not exist.");
                throw new Exception("Board does not exist.");
            }
            if (!usersBoardsID.ContainsKey(email))
                usersBoardsID[email] = new HashSet<long>();

            if (usersBoardsID[email].Contains(boardID))
            {
                Log.Error($"User {email} already joined board {boardID}.");
                throw new Exception("User already joined this board.");
            }

            //BoardExists(email, boards[boardID].BoardName);
            boards[boardID].AddCollaborator(email);
            usersBoardsID[email].Add(boardID); // add the user to the board's ID set
            Log.Info($"User {email} joined board '{boardID}' successfully.");
        }

        /// <summary>
        /// Removes the user from the collaborator of the specified board by its boardID.
        /// </summary>
        /// <param name="inputmail"></param>
        /// <param name="boardID"></param>
        /// <exception cref="Exception"></exception>
        public void LeaveBoard(string inputmail, int boardID)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            if (!boards.ContainsKey(boardID))
            {
                Log.Error($"Board with ID {boardID} does not exist.");
                throw new Exception("Board does not exist.");
            }

            boards[boardID].RemoveCollaborator(email);
            usersBoardsID[email].Remove(boardID);

            Log.Info($"User {email} left board '{boardID}' successfully.");
        }

        /// <summary>
        /// Returns a list of all board IDs that the user with the specified email is a collaborator or owner of.
        /// </summary>
        /// <param name="inputmail"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<long> GetUserBoards(string inputmail)
        {
            string email = inputmail.ToLower(); // normalize email to lowercase
            ValidateActiveUser(email);

            return usersBoardsID.ContainsKey(email) ? usersBoardsID[email].ToList() : new List<long>();
        }

        /// <summary>
        /// Returns the name of the board with the specified ID.
        /// </summary>
        /// <param name="boardID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetBoardName(int boardID)
        {
            if(boards.ContainsKey(boardID))
            {
                Log.Info($"Retrieved name for board ID {boardID}.");
                return boards[boardID].BoardName;
            }
            else
            {
                Log.Error($"Board with ID {boardID} does not exist.");
                throw new Exception("Board does not exist.");
            }
        }

        /// <summary>
        /// Deletes all boards from the system for testing
        /// </summary>
        /// <returns></returns>
        public bool DeleteAllBoards()
        {
            usersBoardsID.Clear();                  // clears RAM
            boards.Clear();                        // clears RAM
            return boardController.DeleteAllData();      // clears DB from boards, tasks, columns and collabs
        }

        /// <summary>
        /// Loads all boards, columns, tasks and collabs into memory from the database.
        /// </summary>
        public void LoadAllBoards()
        {
            var boardDALs = boardController.LoadAllBoards();
            var columnDALs = columnController.LoadAllColumns();
            var taskDALs = taskController.LoadAllTasks();
            var collabDALs = collabController.LoadAllCollaborators();

            boards.Clear();
            usersBoardsID.Clear();

            foreach (var dal in boardDALs)
            {
                var boardBL = new BoardBL(dal, taskController, columnController, collabController, boardController);

                if(boardBL.BoardID + 1 > allTimeBoardCounter)
                {
                    allTimeBoardCounter = boardBL.BoardID + 1; // TODO: maybe change this to use another one
                }

                // Link columns
                var boardColumns = columnDALs.Where(c => c.BoardID == dal.BoardID);
                foreach (var col in boardColumns)
                {
                    boardBL.AddColumnFromDAL(col);  
                }

                // Link tasks
                var boardTasks = taskDALs.Where(t => t.BoardID == dal.BoardID);
                foreach (var task in boardTasks)
                {
                    boardBL.AddTaskFromDAL(task);  
                }
                if (boardTasks.Any())
                {
                    boardBL.UpdateTaskIDGiver(); //updates taskIDGiver after loading from db
                }
                // Link collaborators
                var boardCollabs = collabDALs.Where(c => c.BoardID == dal.BoardID);
                foreach (var collab in boardCollabs)
                {
                    boardBL.AddCollaboratorFromDAL(collab);

                    string collabEmail = collab.Email.ToLower();
                    if (!usersBoardsID.ContainsKey(collabEmail))
                        usersBoardsID[collabEmail] = new HashSet<long>();

                    usersBoardsID[collabEmail].Add(dal.BoardID);
                }

                boards[dal.BoardID] = boardBL;
                if (!usersBoardsID.ContainsKey(dal.Owner))
                    usersBoardsID[dal.Owner] = new HashSet<long>();

                usersBoardsID[dal.Owner].Add(dal.BoardID);
            }

            Log.Info($"allTimeBoardCounter set to {allTimeBoardCounter}.");
            Log.Info("All boards, columns, tasks, and collaborators loaded into memory.");
        }

        /// <summary>
        /// Retrieves the BoardBL with the specified board ID.
        /// </summary>
        /// <param name="boardID">The unique ID of the board.</param>
        /// <returns>The BoardBL instance if found.</returns>
        /// <exception cref="Exception">Thrown if the board does not exist.</exception>
        private BoardBL GetBoardByID(long boardID)
        {
            if (!boards.ContainsKey(boardID))
            {
                Log.Error($"Board with ID '{boardID}' does not exist.");
                throw new Exception("Board does not exist.");
            }

            return boards[boardID];
        }

        /// <summary>
        /// Retrieves the BoardBL by its name.
        /// </summary>
        /// <param name="boardName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public BoardBL GetBoardByName(string boardName)
        {
            foreach (var board in boards.Values)
            {
                if (board.BoardName.Equals(boardName.ToLower()))
                {
                    Log.Info($"Retrieved board '{boardName}'.");
                    return board;
                }
            }

            Log.Error($"Board '{boardName}' does not exist.");
            throw new Exception("Board does not exist.");
        }

    }
}

