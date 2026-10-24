using Backend.BuisnessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.ServiceLayer
{
    
    public class ServiceFactory
    {
        public UserService Us { get; }
        public BoardService Bs { get; }

        public TaskService Ts { get; }

        public ServiceFactory()
        {
            AuthenticationFacade authFacade = new AuthenticationFacade();   
            UserFacade userFacade = new UserFacade(authFacade);
            BoardFacade boardFacade = new BoardFacade(authFacade);
            this.Us = new UserService(userFacade);    
            this.Bs = new BoardService(boardFacade, authFacade);   
            this.Ts = new TaskService(boardFacade); 
        }

        /// <summary>
        /// Loads all of boards and users data to the database.
        /// </summary>
        /// <returns></returns>
        public string LoadAllData()
        {
            try
            {
                Us.LoadAllUserData();
                Bs.LoadAllBoardData();
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }

        /// <summary>
        /// Deletes all of boards and users data from the database.
        /// </summary>
        /// <returns></returns>
        public string DeleteAllData()
        {
            try
            {
                Us.DeleteAllUsers();
                Bs.DeleteAllBoards();
            }
            catch (Exception e)
            {
                return JsonSerializer.Serialize(new Response<string>(e.Message));
            }

            return JsonSerializer.Serialize(new Response<object>());
        }
    }
}
