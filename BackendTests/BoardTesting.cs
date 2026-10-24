using Backend.BuisnessLayer;
using Backend.ServiceLayer;
using IntroSE.Kanban.Backend.ServiceLayer;
using System;
using System.Text.Json;

namespace BackendTests
{
    public class BoardTesting
    {
        private BoardService boardService;
        private UserService userService;
        private ServiceFactory factory;
        private const string DefaultPassword = "Passw0rd";

        /// <summary>
        /// Instantiate a fresh ServiceFactory and clear all boards/tasks in the DB.
        /// </summary>
        public void Setup()
        {
            factory = new ServiceFactory();
            boardService = factory.Bs;
            userService = factory.Us;
            boardService.ResetForTesting();  // clear persisted data before each test
        }

        /// <summary>
        /// Req. 8: Creating a new board
        /// </summary>
        public void TestCreateBoard_Success()
        {
            Setup();
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            Console.WriteLine("-----Running TestCreateBoard_Success-----");
            string result = boardService.CreateBoard("ProjectX", email);
            if (result.Contains("ErrorMessage\":null"))
                Console.WriteLine("Success: Board created successfully.");
            else
                Console.WriteLine("Failed: Board creation failed.");
        }

        /// <summary>
        /// Req. 10: Case-insensitive duplicate board names rejected
        /// </summary>
        public void TestCreateBoard_CaseInsensitiveName()
        {
            Setup();
            string email = $"ci_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            Console.WriteLine("-----Running TestCreateBoard_CaseInsensitiveName-----");
            boardService.CreateBoard("MyBoard", email);
            string result = boardService.CreateBoard("myboard", email);
            if (result.Contains("already exists"))
                Console.WriteLine("Success: Case-insensitive duplicate detected.");
            else
                Console.WriteLine("Failed: Case-insensitive board name not detected.");
        }

        /// <summary>
        /// Req. 10: Duplicate board names (case-insensitive) rejected
        /// </summary>
        public void TestCreateBoard_DuplicateName()
        {
            Setup();
            string email = $"dup_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            Console.WriteLine("-----Running TestCreateBoard_DuplicateName-----");
            boardService.CreateBoard("MyBoard", email);
            string result = boardService.CreateBoard("myboard", email);
            if (result.Contains("already exists"))
                Console.WriteLine("Success: Duplicate board name rejected.");
            else
                Console.WriteLine("Failed: Duplicate board name accepted.");
        }

        /// <summary>
        /// Req. 8: Invalid (empty) board name rejected
        /// </summary>
        public void TestCreateBoard_InvalidName()
        {
            Setup();
            string email = $"inv_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            Console.WriteLine("-----Running TestCreateBoard_InvalidName-----");
            string result = boardService.CreateBoard("", email);
            if (result.Contains("ErrorMessage"))
                Console.WriteLine("Success: Empty board name rejected.");
            else
                Console.WriteLine("Failed: Empty board name accepted.");
        }

        /// <summary>
        /// Req. 8: Deleting an existing board
        /// </summary>
        public void TestDeleteBoard_Success()
        {
            Setup();
            string email = $"del_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            boardService.CreateBoard("TempBoard", email);
            Console.WriteLine("-----Running TestDeleteBoard_Success-----");
            string result = boardService.DeleteBoard("TempBoard", email);
            if (result.Contains("ErrorMessage\":null"))
                Console.WriteLine("Success: Board deleted successfully.");
            else
                Console.WriteLine("Failed: Board deletion failed.");
        }

        /// <summary>
        /// Req. 11: Proper error when deleting non-existent board
        /// </summary>
        public void TestDeleteBoard_NotExist()
        {
            Setup();
            string email = $"none_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            Console.WriteLine("-----Running TestDeleteBoard_NotExist-----");
            string result = boardService.DeleteBoard("FakeBoard", email);
            if (result.Contains("does not exist"))
                Console.WriteLine("Success: Proper error on deleting non-existent board.");
            else
                Console.WriteLine("Failed: Non-existent board deletion not handled.");
        }

        /// <summary>
        /// Req. 16–17: Setting and enforcing a column task limit
        /// </summary>
        public void TestSetColumnLimit_AndEnforce()
        {
            Setup();
            string email = $"lim_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            boardService.CreateBoard("LimitBoard", email);
            boardService.LimitNumOfTasks("LimitBoard", 0, email, 2);

            // two tasks succeed
            boardService.CreateTask("LimitBoard", email, "T1", "desc", DateTime.Now.AddDays(1));
            boardService.CreateTask("LimitBoard", email, "T2", "desc", DateTime.Now.AddDays(1));

            Console.WriteLine("-----Running TestSetColumnLimit_AndEnforce-----");
            // third must fail
            string result = boardService.CreateTask("LimitBoard", email, "T3", "desc", DateTime.Now.AddDays(1));
            if (result.Contains("Column has reached its task limit"))
                Console.WriteLine("Success: Column limit enforced.");
            else
                Console.WriteLine("Failed: Column limit not enforced.");
        }

        /// <summary>Requirement 9: New user has no boards initially</summary>
        /// <summary>Requirement 9: New user has no boards initially</summary>
        public void Test_NoBoardsInitially()
        {
            Setup();
            string email = $"fresh_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);

            Console.WriteLine("-----Running Test_NoBoardsInitially-----");
            string json = boardService.GetUserBoards(email);
            var resp = JsonSerializer.Deserialize<Response<long[]>>(json);
            if (resp != null && resp.ErrorMessage == null && resp.ReturnValue.Length == 0)
                Console.WriteLine("Success: No boards initially.");
            else
                Console.WriteLine("Fail: Unexpected response: " + json);
        }

        /// <summary>Requirement 11: Non-owner cannot delete someone else's board</summary>
        public void Test_DeleteBoard_NotOwner()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            string other = $"oth_{Guid.NewGuid():N}@kanban.com";
            userService.Register(owner, DefaultPassword);
            userService.Register(other, DefaultPassword);
            boardService.CreateBoard("B1", owner);
            boardService.JoinBoard(other, 0); // other joins the board
            Console.WriteLine("-----Running Test_DeleteBoard_NotOwner-----");
            string json = boardService.DeleteBoard("B1", other);
            if (json.Contains("not the owner"))
                Console.WriteLine("Success: Non-owner deletion rejected.");
            else
                Console.WriteLine("Fail: Non-owner was allowed to delete.");
        }


        /// <summary>Requirement 12: Join existing board</summary>
        public void Test_JoinBoard_Success()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            userService.Register(owner, DefaultPassword);
            boardService.CreateBoard("JBoard", owner);
            string joiner = $"guy_{Guid.NewGuid():N}@kanban.com";
            userService.Register(joiner, DefaultPassword);
            Console.WriteLine("-----Running Test_JoinBoard_Success-----");
            string json = boardService.JoinBoard(joiner, 0);
            if (json.Contains("ErrorMessage\":null"))
                Console.WriteLine("Success: Joined board successfully.");
            else
                Console.WriteLine("Fail: Board join failed.");
        }

        /// <summary>Requirement 12: Join non-existent board rejected</summary>
        public void Test_JoinBoard_NotExist()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            userService.Register(owner, DefaultPassword);

            Console.WriteLine("-----Running Test_JoinBoard_NotExist-----");
            string json = boardService.JoinBoard(owner, 999);
            if (json.Contains("Board does not exist."))
                Console.WriteLine("Success: Joining non-existent board rejected.");
            else
                Console.WriteLine("Fail: No error on join non-existent.");
        }

        /// <summary>Requirement 11: Default unlimited tasks in columns</summary>
        public void Test_ColumnLimit_DefaultUnlimited()
        {
            Setup();
            string email = $"lim_{Guid.NewGuid():N}@kanban.com";
            userService.Register(email, DefaultPassword);
            boardService.CreateBoard("NoLimit", email);
            Console.WriteLine("-----Running Test_ColumnLimit_DefaultUnlimited-----");
            // add 5 tasks should all succeed
            bool allOk = true;
            for (int i = 0; i < 5; i++)
            {
                string json = boardService.CreateTask("NoLimit", email, $"T{i}", "d", DateTime.Now.AddDays(1));
                var r = JsonSerializer.Deserialize<Response<object>>(json);
                if (r?.ErrorMessage != null) allOk = false;
            }
            Console.WriteLine(allOk
                ? "Success: No default limit enforced."
                : "Fail: Unexpected default limit.");
        }
        /// <summary>
        /// Requirement 13: Get board name without user session
        /// </summary>
        public void Test_GetBoardName_WithoutUser()
        {
            Console.WriteLine("-----Running Test_GetBoardName_WithoutUser-----");
            Setup();

            string email = $"temp_{Guid.NewGuid():N}@x.com";
            string pwd = "Strong1A";

            // Register user and create board
            userService.Register(email, pwd);
            boardService.CreateBoard("TempBoard", email);

            // Get board ID
            var userBoardsJson = boardService.GetUserBoards(email);
            var boardIDs = JsonSerializer.Deserialize<Response<long[]>>(userBoardsJson);
            long boardID = boardIDs.ReturnValue[0];

            // simulate restart (don't delete DB)
            factory = new ServiceFactory();
            factory.LoadAllData();

            // Re-register user to enable access
            userService.Login(email, pwd);

            // Try GetBoardName
            string nameJson = boardService.GetBoardName((int)boardID);
            var resp = JsonSerializer.Deserialize<Response<string>>(nameJson);

            if (resp.ErrorMessage == null && resp.ReturnValue != null)
                Console.WriteLine("Success: Board name loaded successfully from persisted data.");
            else
                Console.WriteLine($"Failed: {resp.ErrorMessage}");
        }
        /// <summary>
        ///  Print GetColumn JSON format
        /// </summary>
        public void Test_PrintGetInProgressColumnJsonFormat()
        {
            Console.WriteLine("-----Running Test_PrintGetInProgressColumnJsonFormat-----");

            // Setup: fresh email and board
            string email = $"u_{Guid.NewGuid():N}@kanban.com";
            string pwd = "Strong1A";
            string boardName = "testboard";
            var gs = new GradingService();

            Console.WriteLine("Register:");
            Console.WriteLine(gs.Register(email, pwd));

            Console.WriteLine("DeleteData:");
            Console.WriteLine(gs.DeleteData());

            Console.WriteLine("Register:");
            Console.WriteLine(gs.Register(email, pwd));

            Console.WriteLine("CreateBoard:");
            Console.WriteLine(gs.CreateBoard(email, boardName));

            Console.WriteLine("AddTask 1:");
            Console.WriteLine(gs.AddTask(email, boardName, "Task 1", "Desc 1", DateTime.Now.AddDays(1)));

            Console.WriteLine("AddTask 2:");
            Console.WriteLine(gs.AddTask(email, boardName, "Task 2", "Desc 2", DateTime.Now.AddDays(2)));

            // Move both tasks to InProgress (column 1)
            Console.WriteLine("AdvanceTask 1:");
            Console.WriteLine(gs.AdvanceTask(email, boardName, 0, 0)); // Task ID 0
            Console.WriteLine("AdvanceTask 2:");
            Console.WriteLine(gs.AdvanceTask(email, boardName, 0, 1)); // Task ID 1

            // Get InProgress column
            Console.WriteLine("GetColumn (InProgress):");
            string json = gs.GetColumn(email, boardName, 1);

            Console.WriteLine("Returned JSON:");
            Console.WriteLine(json);
        }
        public void Test_PrintFailingVPLJsons()
        {
            Console.WriteLine("-----Running Test_PrintFailingVPLJsons-----");

            string email = "u_b14660a851994edabe63f3b29243598c@kanban.com";
            string pwd = "Strong1A";
            string boardName = "debugboard";
            string title = "My Task";
            string description = "Details";
            DateTime due = DateTime.Now.AddDays(2);

            var gs = new GradingService();

            Console.WriteLine("Register:");
            Console.WriteLine(gs.Register(email, pwd));

            Console.WriteLine("DeleteData:");
            Console.WriteLine(gs.DeleteData());

            Console.WriteLine("Re-Register:");
            Console.WriteLine(gs.Register(email, pwd));

            Console.WriteLine("CreateBoard:");
            Console.WriteLine(gs.CreateBoard(email, boardName));

            Console.WriteLine("AddTask:");
            Console.WriteLine(gs.AddTask(email, boardName, title, description, due));

            Console.WriteLine("UpdateTaskDueDate:");
            Console.WriteLine(gs.DueDate(email, boardName, 0, 0, DateTime.Now.AddDays(3)));

            Console.WriteLine("GetColumnName:");
            Console.WriteLine(gs.GetColumnName(email, boardName, 0));

            Console.WriteLine("GetUserBoards:");
            Console.WriteLine(gs.GetUserBoards(email));
        }
        public void Test_PrintUpdateDueDate_ValidAssignee()
        {
            Console.WriteLine("-----Running Test_PrintUpdateDueDate_ValidAssignee-----");

            var gs = new GradingService();

            string email = $"u_{Guid.NewGuid():N}@kanban.com";
            string pwd = "Strong1A";
            string board = "proj";

            gs.Register(email, pwd);
            gs.CreateBoard(email, board);
            gs.AddTask(email, board, "Task 1", "Desc", DateTime.Now.AddDays(1));

            // Assign the task to the user
            var assign = gs.AssignTask(email, board, 0, 0, email);
            Console.WriteLine("AssignTask:\n" + assign);

            // Now try to update the due date
            var result = gs.DueDate(email, board, 0, 0, DateTime.Now.AddDays(5));
            Console.WriteLine("UpdateTaskDueDate:\n" + result);
        }
        public void Test_PrintAssignTask()
        {
            Console.WriteLine("-----Running Test_PrintAssignTask-----");

            var gs = new GradingService();

            string email = $"u_{Guid.NewGuid():N}@kanban.com";
            string pwd = "Strong1A";
            string board = "assignboard";

            gs.Register(email, pwd);
            gs.CreateBoard(email, board);
            gs.AddTask(email, board, "Task to assign", "desc", DateTime.Now.AddDays(3));

            string assignJson = gs.AssignTask(email, board, 0, 0, email);  // Self-assign
            Console.WriteLine("AssignTask:\n" + assignJson);
        }
        public void Test_PrintUpdateTaskTitle_WithAssignee()
        {
            Console.WriteLine("-----Running Test_PrintUpdateTaskTitle_WithAssignee-----");

            var gs = new GradingService();

            string email = $"u_{Guid.NewGuid():N}@kanban.com";
            string pwd = "Strong1A";
            string board = "titleboard";

            gs.Register(email, pwd);
            gs.CreateBoard(email, board);
            gs.AddTask(email, board, "Original Title", "desc", DateTime.Now.AddDays(2));

            // Must assign first
            gs.AssignTask(email, board, 0, 0, email);

            string updateTitleJson = gs.UpdateTaskTitle(email, board, 0, 0, "New Task Title");
            Console.WriteLine("UpdateTaskTitle:\n" + updateTitleJson);
        }
        public void RunAllTests()
        {
            Console.WriteLine("\n=== Board Tests ===\n");
            Test_NoBoardsInitially();
            TestCreateBoard_Success();
            TestCreateBoard_CaseInsensitiveName();
            TestCreateBoard_DuplicateName();
            TestCreateBoard_InvalidName();
            TestDeleteBoard_Success();
            TestDeleteBoard_NotExist();
            TestSetColumnLimit_AndEnforce();
            Test_ColumnLimit_DefaultUnlimited();
            Test_JoinBoard_Success();
            Test_JoinBoard_NotExist();
            Test_DeleteBoard_NotOwner();
            Test_GetBoardName_WithoutUser();
            //Test_PrintGetInProgressColumnJsonFormat();
            //Test_PrintFailingVPLJsons();
            //Test_PrintUpdateDueDate_ValidAssignee();
            //Test_PrintAssignTask();
            //Test_PrintUpdateTaskTitle_WithAssignee();
        }
    }
}
