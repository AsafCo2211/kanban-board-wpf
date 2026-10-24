using System;
using System.Collections.Generic;
using System.Linq;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;

namespace BackendTests
{
    /// <summary>
    /// DAL-layer tests for Milestone 2, using DAL controllers and one-line pass/fail output.
    /// </summary>
    public class DalTesting
    {
        private UserController userController = null!;
        private BoardController boardController = null!;
        private CollaboratorsController collabController = null!;
        private ColumnController columnController = null!;
        private TaskController taskController = null!;

        private string owner = null!;
        private long boardId;

        /// <summary>
        /// Initializes DAL controllers and seeds one user+board. Call before running tests.
        /// </summary>
        public void Setup()
        {
            userController = new UserController();
            boardController = new BoardController();
            collabController = new CollaboratorsController();
            columnController = new ColumnController(new TaskController());
            taskController = new TaskController();
            columnController = new ColumnController(taskController);

            owner = $"owner_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(owner, "pwd", userController));

            boardId = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(boardId, "DALBoard", owner, boardController));
        }

        /// <summary>Requirement 6: Add and retrieve a user</summary>
        public void Test_AddUser_AndFind()
        {
            Console.WriteLine("-----Running Test_AddUser_AndFind-----");
            string email = $"user_{Guid.NewGuid():N}@dal.test";
            bool added = userController.AddNewUser(new UserDAL(email, "pwd", userController));
            bool found = userController.LoadAll().Any(u => u.Email == email);
            if (added && found)
                Console.WriteLine("Success: user added and found.");
            else
                Console.WriteLine("Fail: user missing or not found in database.");
        }

        /// <summary>Requirement 6: SelectAll count increments by one after insert</summary>
        public void Test_SelectAll_CountIncrement()
        {
            Console.WriteLine("-----Running Test_SelectAll_CountIncrement-----");
            int before = userController.LoadAll().Count;
            userController.AddNewUser(new UserDAL($"cnt_{Guid.NewGuid():N}@dal.test", "pwd", userController));
            int after = userController.LoadAll().Count;
            if (after == before + 1)
                Console.WriteLine("Success: user count incremented by one.");
            else
                Console.WriteLine($"Fail: expected {before + 1} but was {after}.");
        }

        /// <summary>Requirement 20: Update password persists</summary>
        public void Test_UpdatePassword_Persists()
        {
            Console.WriteLine("-----Running Test_UpdatePassword_Persists-----");
            string email = $"upd_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(email, "oldpwd", userController));
            bool ok = userController.UpdatePassword(email, "newpwd");
            var dto = userController.LoadAll().FirstOrDefault(u => u.Email == email);
            bool persisted = dto != null && dto.Password == "newpwd";
            if (ok && persisted)
                Console.WriteLine("Success: password updated and verified.");
            else
                Console.WriteLine("Fail: password update failed or not persisted.");
        }

        /// <summary>Requirement 6: ConvertReaderToUser returns matching DTO</summary>
        public void Test_ConvertReaderToUser()
        {
            Console.WriteLine("-----Running Test_ConvertReaderToUser-----");
            string email = $"conv_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(email, "pwd", userController));
            var dto = userController.LoadAll().FirstOrDefault(u => u.Email == email);
            if (dto != null && dto.Password == "pwd")
                Console.WriteLine("Success: DTO returned with correct data.");
            else
                Console.WriteLine("Fail: DTO was null or data mismatched.");
        }

        /// <summary>Requirement 8: Add and retrieve a board</summary>
        public void Test_AddBoard_AndFind()
        {
            Console.WriteLine("-----Running Test_AddBoard_AndFind-----");
            string owner2 = $"owner2_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(owner2, "pwd", userController));
            long id = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(id, "BoardX", owner2, boardController));
            bool exists = boardController.GetAllBoardsForUser(owner2).Any(b => b.BoardID == id);
            if (exists)
                Console.WriteLine("Success: board inserted and found.");
            else
                Console.WriteLine("Fail: board not found after insert.");
        }

        /// <summary>Requirement 8: Select board by ID</summary>
        public void Test_SelectBoard_ById()
        {
            Console.WriteLine("-----Running Test_SelectBoard_ById-----");
            string owner3 = $"owner3_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(owner3, "pwd", userController));
            long id = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(id, "BoardY", owner3, boardController));
            var dto = boardController.SelectBoard(id);
            if (dto != null)
                Console.WriteLine("Success: board retrieved by ID.");
            else
                Console.WriteLine("Fail: board not retrieved.");
        }

        /// <summary>Requirement 8: Board count increments after insert</summary>
        public void Test_GetAllBoardsForUser_CountIncrement()
        {
            Console.WriteLine("-----Running Test_GetAllBoardsForUser_CountIncrement-----");
            string owner4 = $"owner4_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(owner4, "pwd", userController));
            int before = boardController.GetAllBoardsForUser(owner4).Count;
            long id = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(id, "BoardZ", owner4, boardController));
            int after = boardController.GetAllBoardsForUser(owner4).Count;
            if (after == before + 1)
                Console.WriteLine("Success: board count incremented by one.");
            else
                Console.WriteLine($"Fail: expected {before + 1} but was {after}.");
        }

        /// <summary>Requirement 10: Update board name persists</summary>
        public void Test_UpdateBoardName()
        {
            Console.WriteLine("-----Running Test_UpdateBoardName-----");
            string owner5 = $"owner5_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(owner5, "pwd", userController));
            long id = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(id, "Old", owner5, boardController));
            boardController.UpdateBoardName(id, "New");
            var dto = boardController.SelectBoard(id);
            if (dto != null && dto.BoardName == "New")
                Console.WriteLine("Success: board name updated.");
            else
                Console.WriteLine("Fail: board name did not change.");
        }

        /// <summary>Requirement 13: Change board owner persists</summary>
        public void Test_ChangeOwner()
        {
            Console.WriteLine("-----Running Test_ChangeOwner-----");
            string oldO = $"old_{Guid.NewGuid():N}@dal.test";
            string newO = $"new_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(oldO, "pwd", userController));
            userController.AddNewUser(new UserDAL(newO, "pwd", userController));
            long id = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(id, "Transfer", oldO, boardController));
            boardController.ChangeOwner(id, oldO, newO);
            bool appears = boardController.GetAllBoardsForUser(newO).Any(b => b.BoardID == id);
            if (appears)
                Console.WriteLine("Success: ownership transferred.");
            else
                Console.WriteLine("Fail: board not found under new owner.");
        }

        /// <summary>Requirement 11: Delete board persists</summary>
        public void Test_DeleteBoard_Persists()
        {
            Console.WriteLine("-----Running Test_DeleteBoard_Persists-----");
            string owner6 = $"owner6_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(owner6, "pwd", userController));
            long id = DateTime.UtcNow.Ticks;
            boardController.AddBoard(new BoardDAL(id, "DelMe", owner6, boardController));
            boardController.DeleteBoard(id);
            bool stillThere = boardController.GetAllBoardsForUser(owner6).Any(b => b.BoardID == id);
            if (!stillThere)
                Console.WriteLine("Success: board deleted.");
            else
                Console.WriteLine("Fail: board still present.");
        }

        /// <summary>Requirement 16: Add column returns empty task list</summary>
        public void Test_AddColumn_EmptyTasks()
        {
            Console.WriteLine("-----Running Test_AddColumn_EmptyTasks-----");
            columnController.AddColumn(new ColumnDAL(boardId, "Backlog", 5, columnController));
            var tasks = columnController.SelectTasksInColumn(boardId, "Backlog");
            if (tasks != null && tasks.Count == 0)
                Console.WriteLine("Success: column added and empty task list returned.");
            else
                Console.WriteLine("Fail: unexpected SelectTasksInColumn result.");
        }

        /// <summary>Requirement 16: SelectColumn returns correct limit</summary>
        public void Test_SelectColumn_Metadata()
        {
            Console.WriteLine("-----Running Test_SelectColumn_Metadata-----");
            columnController.AddColumn(new ColumnDAL(boardId, "Done", 4, columnController));
            var col = columnController.SelectColumn(boardId, "Done");
            if (col != null && col.ColumnLimit == 4)
                Console.WriteLine("Success: column metadata correct.");
            else
                Console.WriteLine("Fail: column metadata incorrect.");
        }

        /// <summary>Requirement 16: UpdateColumnLimit persists</summary>
        public void Test_UpdateColumnLimit_Persists()
        {
            Console.WriteLine("-----Running Test_UpdateColumnLimit_Persists-----");
            columnController.AddColumn(new ColumnDAL(boardId, "Todo", 1, columnController));
            columnController.UpdateColumnLimit(boardId, "Todo", 3);
            var col = columnController.SelectColumn(boardId, "Todo");
            if (col != null && col.ColumnLimit == 3)
                Console.WriteLine("Success: limit updated and tasks under new limit.");
            else
                Console.WriteLine("Fail: ColumnLimit={col?.ColumnLimit}.");
        }

        /// <summary>Requirement 18: Add task and retrieve InProgress</summary>
        public void Test_AddTask_AndFind_InProgress()
        {
            Console.WriteLine("-----Running Test_AddTask_AndFind_InProgress-----");
            long taskId = DateTime.UtcNow.Ticks;
            taskController.AddTask(new TaskDAL(taskId, boardId, "in progress", "T", "D", DateTime.Now.AddDays(1), DateTime.Now, owner, taskController));
            bool found = taskController.SelectInProgTasks(owner).Any(t => t.TaskID == taskId);
            if (found)
                Console.WriteLine("Success: in-progress task retrieved.");
            else
                Console.WriteLine("Fail: task not found after insert.");
        }

        /// <summary>Requirement 21: Update task title persists</summary>
        public void Test_UpdateTaskTitle_Persists()
        {
            Console.WriteLine("-----Running Test_UpdateTaskTitle_Persists-----");
            long taskId = DateTime.UtcNow.Ticks;
            taskController.AddTask(new TaskDAL(taskId, boardId, "in progress", "Old", "D", DateTime.Now.AddDays(1), DateTime.Now, owner, taskController));
            taskController.UpdateTitle(boardId, taskId, "New");
            var dto = taskController.SelectInProgTasks(owner).FirstOrDefault(t => t.TaskID == taskId);
            if (dto != null && dto.Title == "New")
                Console.WriteLine("Success: title updated in database.");
            else
                Console.WriteLine("Fail: title did not update.");
        }

        /// <summary>Requirement 21: Update task description persists</summary>
        public void Test_UpdateTaskDescription_Persists()
        {
            Console.WriteLine("-----Running Test_UpdateTaskDescription_Persists-----");
            long taskId = DateTime.UtcNow.Ticks;
            taskController.AddTask(new TaskDAL(taskId, boardId, "in progress", "T", "OldD", DateTime.Now.AddDays(1), DateTime.Now, owner, taskController));
            taskController.UpdateDescription(boardId, taskId, "NewD");
            var dto = taskController.SelectInProgTasks(owner).FirstOrDefault(t => t.TaskID == taskId);
            if (dto != null && dto.Description == "NewD")
                Console.WriteLine("Success: description updated.");
            else
                Console.WriteLine("Fail: description did not update.");
        }

        /// <summary>Requirement 21: Update task due time persists</summary>
        public void Test_UpdateTaskDueTime_Persists()
        {
            Console.WriteLine("-----Running Test_UpdateTaskDueTime_Persists-----");
            long taskId = DateTime.UtcNow.Ticks;
            DateTime newDue = DateTime.Now.AddDays(5);
            taskController.AddTask(new TaskDAL(taskId, boardId, "in progress", "T", "D", DateTime.Now.AddDays(1), DateTime.Now, owner, taskController));
            taskController.UpdateDueTime(boardId, taskId, newDue);
            var dto = taskController.SelectInProgTasks(owner).FirstOrDefault(t => t.TaskID == taskId);
            if (dto != null && dto.DueDate == newDue)
                Console.WriteLine("Success: due date updated.");
            else
                Console.WriteLine("Fail: due date did not update.");
        }

        /// <summary>Requirement 23: Update task assignee persists</summary>
        public void Test_UpdateTaskAssignee_Persists()
        {
            Console.WriteLine("-----Running Test_UpdateTaskAssignee_Persists-----");
            long taskId = DateTime.UtcNow.Ticks;
            string newUser = $"assn_{Guid.NewGuid():N}@dal.test";
            userController.AddNewUser(new UserDAL(newUser, "pwd", userController));
            taskController.AddTask(new TaskDAL(taskId, boardId, "in progress", "T", "D", DateTime.Now.AddDays(1), DateTime.Now, owner, taskController));
            taskController.UpdateAssignee(boardId, taskId, newUser);
            bool assigned = taskController.SelectInProgTasks(newUser)
                                        .Any(t => t.TaskID == taskId);
            if (assigned)
                Console.WriteLine("Success: task reassigned correctly.");
            else
                Console.WriteLine("Fail: task not found under new assignee.");
        }

        /// <summary>Runs all DAL tests</summary>
        public void RunAllTests()
        {
            Console.WriteLine("\n=== DAL Tests ===\n");
            Setup();
            Test_AddUser_AndFind();
            Test_SelectAll_CountIncrement();
            Test_UpdatePassword_Persists();
            Test_ConvertReaderToUser();

            Test_AddBoard_AndFind();
            Test_SelectBoard_ById();
            Test_GetAllBoardsForUser_CountIncrement();
            Test_UpdateBoardName();
            Test_ChangeOwner();
            Test_DeleteBoard_Persists();

            Test_AddColumn_EmptyTasks();
            Test_SelectColumn_Metadata();
            Test_UpdateColumnLimit_Persists();

            Test_AddTask_AndFind_InProgress();
            Test_UpdateTaskTitle_Persists();
            Test_UpdateTaskDescription_Persists();
            Test_UpdateTaskDueTime_Persists();
            Test_UpdateTaskAssignee_Persists();
        }
    }
}
