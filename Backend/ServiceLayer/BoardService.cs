using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Backend.BuisnessLayer;

namespace Backend.ServiceLayer
{
    public class BoardService
    {

        private readonly BoardFacade boardFacade;
        private readonly AuthenticationFacade authFacade;
        

        internal BoardService(BoardFacade boardFacade, AuthenticationFacade af)
        {
            this.boardFacade = boardFacade;
            this.authFacade = af;  
        }

        /// <summary>
        /// Creates a new board for the user.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public string CreateBoard(string boardName, string email)
        {
            BoardBL newBoard;
            try
            {
                newBoard = boardFacade.CreateBoard(email, boardName);
            }
            catch(Exception e) {
                string err = e.Message;
                return JsonSerializer.Serialize(new Response<string>(null, e.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Deletes a board for the user based on given board name.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public string DeleteBoard(string boardName, string email)
        {
            try
            {
                boardFacade.DeleteBoard(email, boardName);  
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }
            return  JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Limits the number of tasks in a given column for a given board.
        /// </summary>
        /// <param name="board_Name"></param>
        /// <param name="column_Num"></param>
        /// <param name="email"></param>
        /// <param name="newLimit"></param>
        /// <returns></returns>
        public string LimitNumOfTasks(string board_Name, int column_Num,string email, int newLimit)
        {
            try
            {
                boardFacade.LimitTaskInColumn(email, board_Name, column_Num, newLimit);
            }
            catch(Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());

        }

        /// <summary>
        /// Creates a new task in the given board and column.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="email"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="due_date"></param>
        /// <returns></returns>
        public string CreateTask(string boardName, string email, string title, string description, DateTime due_date)
        {
            try
            {
                boardFacade.CreateTask(boardName, email, title, description, due_date);                
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Deletes a task from the given board and column.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="email"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public string DeleteTask(string boardName, int columnNum, string email, TaskSL task)
        {
            try
            {
                if (task == null)
                    throw new Exception("Invalid task.");

                boardFacade.DeleteTask(boardName, columnNum, email, task.Id);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// lists all tasks that are in the in progress column in each user board for specific user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public string ListInProgTasks(string email)
        {
            TaskSL[] inProgSL;
            try
            {
                List<TaskBL> inProg = boardFacade.GetAllInProgressTasks(email);
                inProgSL = inProg.Select(taskBL => new TaskSL(taskBL)).ToArray();
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<TaskSL[]>(inProgSL));
        }


        /// <summary>
        /// Gets the limit of a given column in a given board.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="email"></param>
        /// <param name="columnOrdinal"></param>
        /// <returns></returns>
        public string GetColumnLimit(string boardName, string email, int columnOrdinal)
        {
            int limit;
            try
            {
                limit = boardFacade.GetColumnLimit(email, boardName, columnOrdinal);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(
                    new Response<string>(e.Message),
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Never }
                );
            }
            return JsonSerializer.Serialize(
                    new Response<int>(limit, null),
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Never }
                );
        }

        /// <summary>
        /// Returns the name of a given column in a given board.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <param name="columnOrdinal"></param>
        /// <returns></returns>
        public string GetColumnName(string email, string boardName, int columnOrdinal)
        {
            string columnName;
            try
            {
                columnName = boardFacade.GetColumnName(email, boardName, columnOrdinal);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }
            return JsonSerializer.Serialize(new Response<string>(columnName, null));
        }

        /// <summary>
        /// Gets the column of a given board and column ordinal.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <param name="columnOrdinal"></param>
        /// <returns></returns>
        public string GetColumn(string email, string boardName, int columnOrdinal)
        {
            TaskSL[] tasksSL;

            try
            {
                List<TaskBL> tasksBL = boardFacade.GetColumnAsList(email, boardName, columnOrdinal);
                tasksSL = tasksBL.Select(t => new TaskSL(t)).ToArray();
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<TaskSL[]>(tasksSL));
        }

        /// <summary>
        /// Returns a list of all board IDs the user has access to based on their email.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public string GetUserBoards(string email)
        {
            List<long> userBoards;
            try
            {
                userBoards = boardFacade.GetUserBoards(email);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<long[]>(userBoards.ToArray(), null));
        }

        /// <summary>
        /// Adds a collaborator(email) to a given board.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardID"></param>
        /// <returns></returns>
        public string JoinBoard(string email, int boardID)
        {
            try
            {
                boardFacade.JoinBoard(email, boardID);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Removes the user from a given board based on the email and board ID.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardID"></param>
        /// <returns></returns>
        public string LeaveBoard(string email, int boardID)
        {
            try
            {
                boardFacade.LeaveBoard(email, boardID);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Changes the owner of a board from the current owner to a new owner based on their emails and the board name.
        /// </summary>
        /// <param name="currentOwnerEmail"></param>
        /// <param name="newOwnerEmail"></param>
        /// <param name="boardName"></param>
        /// <returns></returns>
        public string ChangeBoardOwner(string currentOwnerEmail, string newOwnerEmail, string boardName)
        {
            try
            {
                boardFacade.ChangeBoardOwner(currentOwnerEmail, boardName, newOwnerEmail);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Returns the name of a board based on its ID.
        /// </summary>
        /// <param name="boardID"></param>
        /// <returns></returns>
        public string GetBoardName(int boardID)
        {
            string name;
            try
            {                
                name = boardFacade.GetBoardName(boardID);
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<string>(name, null));
        }

        /// <summary>
        /// returns a boardSL based on the board name and the user's email.
        /// </summary>
        /// <param name="boardName"></param>
        /// <param name="mail"></param>
        /// <returns></returns>
        public string GetBoard(string boardName, string mail)
        {
            BoardSL board;
            try
            {
                board = new BoardSL(boardFacade.GetBoard(mail, boardName));
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<BoardSL>(board, null));
        }

        /// <summary>
        /// Loads all board data from the database.
        /// </summary>
        /// <returns></returns>
        public string LoadAllBoardData()
        {
            try
            {
                boardFacade.LoadAllBoards();
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Deletes all boards from the database.
        /// </summary>
        /// <returns></returns>
        public string DeleteAllBoards()
        {
            try
            {
                boardFacade.DeleteAllBoards();
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Specifically used for testing purposes to reset the board state for Setup().
        /// </summary>
        public void ResetForTesting()
        {
            boardFacade.DeleteAllBoards();
        }
        /// <summary>
        /// Gets a task for testing purposes based on the owner, board name, column ordinal, and task ID.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="boardName"></param>
        /// <param name="columnOrdinal"></param>
        /// <param name="taskID"></param>
        /// <returns></returns>
        internal TaskBL GetTaskForTest(string owner, string boardName, int columnOrdinal, long taskID)
        {
            var board = boardFacade.GetBoard(owner.ToLower(), boardName.ToLower());
            var column = board.GetColumnBL(columnOrdinal);
            TaskBL task = column.GetTask(taskID);
            return task;
        }
    }
}
