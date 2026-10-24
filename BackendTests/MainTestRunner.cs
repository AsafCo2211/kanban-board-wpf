using Backend.ServiceLayer;
using log4net;
using log4net.Config;
using System;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Data.SQLite;
using System.IO;
using Backend.BuisnessLayer;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.ServiceLayer;


namespace BackendTests
{
    public class MainTestRunner
    {
        public static void Main(string[] args)
        {
            // Log initialization
            var logReposetory = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logReposetory, new FileInfo(Path.Combine(Path.GetFullPath(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName), "Backend\\log4net.config")));
            //THIS IS MAIN FILE

            Console.WriteLine("===== Running Kanban System Tests =====\n\n");

            // Deletes the kanban.db file if it exists, to ensure a clean slate for testing
            // under comment because not needed each time. only when making actual changes to tables
            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            //string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "kanban.db"));
            //if (File.Exists(path))
            //{
            //    File.Delete(path);
            //}
            ///////////////////////////////////////////////////////////////////////////////////////////////////////



            // Ensure tables exist
            new UserController().CreateTable();
            new BoardController().CreateTable();
            new ColumnController(new TaskController()).CreateTable();
            new TaskController().CreateTable();
            new CollaboratorsController().CreateTable();

            AuthenticationFacade authFacade = new AuthenticationFacade();
            UserFacade userFacade = new UserFacade(authFacade);
            BoardFacade boardFacade = new BoardFacade(authFacade);

            boardFacade.DeleteAllBoards(); // Clean up before tests for every table that isnt users to make sure the board, task, column, collabs tables are empty
            userFacade.DeleteAllUsers(); // Clean up before tests to make sure the user table is empty

            //==============================Starting Testing==============================
            var boardTests = new BoardTesting();
            boardTests.Setup();
            boardTests.RunAllTests();
            //==============================Finished Board Tests==============================
            var taskTests = new TaskTesting();
            taskTests.RunAllTests();
            //==============================Finished Task Tests===============================
            var userTests = new UserTesting();
            userTests.RunAllTests();
            //==============================Finished User Tests===============================
            var dalTests = new DalTesting();
            dalTests.RunAllTests();
            //==============================Finished DAL Tests==============================


            boardFacade.DeleteAllBoards(); // Clean up after tests for every table that isnt users
            userFacade.DeleteAllUsers(); // Clean up after tests            
            //==============================Finished Testing==============================
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();

        }
    }
}
