using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.ServiceLayer;
using Backend.BuisnessLayer;
using System.Text.Json;

namespace Backend.ServiceLayer
{
    public class TaskService
    {
        private readonly BoardFacade boardFacade;

        internal TaskService(BoardFacade boardFacade)
        {
            this.boardFacade = boardFacade;
        }

        /// <summary>
        /// Upgrades the state of the task by one step in the BoardFacade's columns. 
        /// Moves it from the first list to the other
        /// </summary>
        /// <param name="none"></param>
        /// <returns>Response<TaskSL></returns>
        public string UpdateState(string email, string boardName, int columnNum, int taskID)
        {
            try
            {
                boardFacade.AdvanceTaskColumn(email, boardName, columnNum, taskID);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Edits title of Task  to argument
        /// </summary>
        /// <param name="task"></param>
        /// <param name="name"></param>
        /// <returns>Response<TaskSL></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string EditTitle(string title, long taskID, string email, string boardName)
        {
            try
            {
                boardFacade.EditTaskTitle(title, taskID, email, boardName);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Edits description of Task  to argument
        /// </summary>
        /// <param name="task"></param>
        /// <param name="desc"></param>
        /// <returns>Response<TaskSL></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string EditDesc(string desc, long taskID, string email, string boardName)
        {
            try
            {
                boardFacade.EditTaskDescription(desc, taskID, email, boardName);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Updates due date of certain task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="dueDate"></param>
        /// <returns>Response<TaskSL></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string EditDue_Date(DateTime dueDate, long taskID, string email, string boardName)
        {
            try
            {
                boardFacade.EditTaskDueDate(dueDate, taskID, email, boardName);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }
            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Assigns a task to a user by their email address.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="boardName"></param>
        /// <param name="columnOrdinal"></param>
        /// <param name="taskID"></param>
        /// <param name="emailAssignee"></param>
        /// <returns></returns>
        public string AssignTask(string email, string boardName, int columnOrdinal, int taskID, string emailAssignee)
        {
            try
            {
                boardFacade.AssignTask(email, boardName, columnOrdinal, taskID, emailAssignee);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Response<string>(ex.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }
    }
}
