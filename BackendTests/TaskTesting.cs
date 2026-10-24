using System;
using System.Collections.Generic;
using System.Text.Json;
using Backend.ServiceLayer;

namespace BackendTests
{
    public class TaskTesting
    {
        private ServiceFactory factory = null!;
        private UserService Us = null!;
        private BoardService Bs = null!;
        private TaskService Ts = null!;
        private const string DefaultPassword = "P4ssword";
        private const string BoardName = "TaskBoard";

        /// <summary>
        /// Build the services (once). You can comment out the ResetForTesting()
        /// if you truly want to accumulate state between tests.
        /// </summary>
        public void Setup()
        {
            factory = new ServiceFactory();
            Us = factory.Us;
            Bs = factory.Bs;
            Ts = factory.Ts;

            // Optional: wipe the DB before all tests run
            Bs.ResetForTesting();
        }
        /// <summary>Requirement 12: Leave unassigns tasks</summary>
        public void Test_LeaveBoard_UnassignsTasks()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            string member = $"mem_{Guid.NewGuid():N}@kanban.com";
            Us.Register(owner, DefaultPassword);
            Us.Register(member, DefaultPassword);
            Bs.CreateBoard("UnassignTest", owner);
            Bs.JoinBoard(member, 0);
            Bs.CreateTask("UnassignTest", owner, "T", "d", DateTime.Now.AddDays(1));
            Ts.AssignTask(owner, "UnassignTest", 0, 0, member);
            Console.WriteLine("-----Running Test_LeaveBoard_UnassignsTasks-----");
            Bs.LeaveBoard(member, 0);

            var task = Bs.GetTaskForTest(owner, "UnassignTest", 0, 0);
            string check = task.Assignee;
            if (task != null && task.Assignee == null)
                Console.WriteLine("Success: Tasks unassigned after leave.");
            else
                Console.WriteLine("Fail: Task remained assigned.");
        }
        public void Test_CreateTask_Success()
        {
            // each test makes its own user & board
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            Console.WriteLine("-----Running Test_CreateTask_Success-----");
            string json = Bs.CreateTask(boardName, email, "T1", "desc", DateTime.Now.AddDays(2));
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage == null)
                Console.WriteLine("Success: Task created");
            else
                Console.WriteLine("Fail: Task creation failed");
        }

        public void Test_CreateTask_EmptyTitle()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            Console.WriteLine("-----Running Test_CreateTask_EmptyTitle-----");
            string json = Bs.CreateTask(boardName, email, "", "d", DateTime.Now.AddDays(1));
            var resp = JsonSerializer.Deserialize<Response<TaskSL>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Empty title rejected");
            else
                Console.WriteLine("Fail: Empty title accepted");
        }

        public void Test_CreateTask_TitleTooLong()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            Console.WriteLine("-----Running Test_CreateTask_TitleTooLong-----");
            var longTitle = new string('X', 51);
            string json = Bs.CreateTask(boardName, email, longTitle, "d", DateTime.Now.AddDays(1));
            var resp = JsonSerializer.Deserialize<Response<TaskSL>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Too-long title rejected");
            else
                Console.WriteLine("Fail: Too-long title accepted");
        }

        public void Test_CreateTask_DescTooLong()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            Console.WriteLine("-----Running Test_CreateTask_DescTooLong-----");
            var longDesc = new string('D', 301);
            string json = Bs.CreateTask(boardName, email, "d", longDesc, DateTime.Now.AddDays(1));
            var resp = JsonSerializer.Deserialize<Response<TaskSL>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Too-long description rejected");
            else
                Console.WriteLine("Fail: Too-long description accepted");
        }

        public void Test_CreateTask_PastDueDate()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            Console.WriteLine("-----Running Test_CreateTask_PastDueDate-----");
            string json = Bs.CreateTask(boardName, email, "TPast", "d", DateTime.Now.AddDays(-1));
            var resp = JsonSerializer.Deserialize<Response<TaskSL>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Past-due date rejected");
            else
                Console.WriteLine("Fail: Past-due date accepted");
        }

        public void Test_CreateTask_Unauthorized()
        {
            // owner sets up board
            string ownerEmail = $"owner_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(ownerEmail, DefaultPassword);
            Bs.CreateBoard(boardName, ownerEmail);

            Console.WriteLine("-----Running Test_CreateTask_Unauthorized-----");
            string json = Bs.CreateTask(boardName, "fake@user.com", "T4", "d", DateTime.Now.AddDays(1));
            var resp = JsonSerializer.Deserialize<Response<TaskSL>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Unauthorized user rejected");
            else
                Console.WriteLine("Fail: Unauthorized user accepted");
        }

        public void Test_DeleteTask_Success()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            // create one task
            var createJson = Bs.CreateTask(boardName, email, "DelMe", "d", DateTime.Now.AddDays(1));
            var createResp = JsonSerializer.Deserialize<Response<object>>(createJson);
            if (createResp == null || createResp.ErrorMessage != null)
            {
                Console.WriteLine("Fail: Task creation failed before deletion");
                return;
            }

            // fetch it
            string colJson = Bs.GetColumn(email, boardName, 0);
            var colResp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(colJson);
            if (colResp == null || colResp.ErrorMessage != null ||
                colResp.ReturnValue == null || colResp.ReturnValue.Count == 0)
            {
                Console.WriteLine("Fail: Could not retrieve task for deletion");
                return;
            }
            var taskToDelete = colResp.ReturnValue[0];

            // delete
            Console.WriteLine("-----Running Test_DeleteTask_Success-----");
            string delJson = Bs.DeleteTask(boardName, 0, email, taskToDelete);
            var delResp = JsonSerializer.Deserialize<Response<object>>(delJson);
            if (delResp != null && delResp.ErrorMessage == null)
                Console.WriteLine("Success: Task deleted");
            else
                Console.WriteLine("Fail: Delete failed");
        }

        public void Test_DeleteTask_NotExist()
        {
            string email = $"user_{Guid.NewGuid():N}@kanban.com";
            string boardName = $"board_{Guid.NewGuid():N}";
            Us.Register(email, DefaultPassword);
            Bs.CreateBoard(boardName, email);

            Console.WriteLine("-----Running Test_DeleteTask_NotExist-----");
            var fake = new TaskSL("Nope", "desc", DateTime.Now.AddDays(1));
            string json = Bs.DeleteTask(boardName, 0, email, fake);
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Non-existent delete rejected");
            else
                Console.WriteLine("Fail: Non-existent delete accepted");
        }

        /// <summary>Requirement 18: Only board members can add tasks</summary>
        public void Test_CreateTask_NotMember()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            string nonmem = $"mem_{Guid.NewGuid():N}@kanban.com";
            Us.Register(owner, DefaultPassword);
            Us.Register(nonmem, DefaultPassword);
            Bs.CreateBoard(BoardName, owner);

            Console.WriteLine("-----Running Test_CreateTask_NotMember-----");
            string json = Bs.CreateTask(BoardName, nonmem, "T", "d", DateTime.Now.AddDays(1));
            var resp = JsonSerializer.Deserialize<Response<TaskSL>>(json);
            if (resp != null && resp.ErrorMessage != null)
                Console.WriteLine("Success: Non-member task creation rejected.");
            else
                Console.WriteLine("Fail: Non-member was allowed to create task.");
        }

        public void Test_MoveTask_Various()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            string member = $"mem_{Guid.NewGuid():N}@kanban.com";
            Us.Register(owner, DefaultPassword);
            Us.Register(member, DefaultPassword);
            Bs.CreateBoard(BoardName, owner);
            Bs.JoinBoard(member, 0);
            Bs.CreateTask(BoardName, member, "T", "d", DateTime.Now.AddDays(1));

            // Use your GetTaskForTest helper to directly retrieve the task
            var task = Bs.GetTaskForTest(member, BoardName, 0, 0); // assuming task ID = 0 for first task
            if (task == null)
            {
                Console.WriteLine("Fail: Could not retrieve task for move");
                return;
            }

            Console.WriteLine("-----Running Test_MoveTask_Success-----");
            string result = Ts.UpdateState(member, BoardName, 0, (int)task.TaskID);
            if (result.Contains("ErrorMessage\":null"))
                Console.WriteLine("Success: Assignee moved task.");
            else
                Console.WriteLine("Fail: Assignee move failed.");
        }


        /// <summary>
        /// Requirement 20–21: Unassigned tasks able to be assigned by anyone, edited by assignee only until task is done; creation time immutable
        /// </summary>
        public void Test_EditTask_AssigneeOnly_ImmutableCreation()
        {
            Setup();
            // 1️⃣ Create the task (unassigned)
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            Us.Register(owner, DefaultPassword);
            Bs.CreateBoard(BoardName, owner);
            Bs.CreateTask(BoardName, owner, "T", "d", DateTime.Now.AddDays(1));

            // 2️⃣ Grab its ID
            var colJson = Bs.GetColumn(owner, BoardName, 0);
            var colResp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(colJson);
            if (colResp == null || colResp.ErrorMessage != null || colResp.ReturnValue.Count == 0)
            {
                Console.WriteLine("Fail: Could not retrieve task for editing");
                return;
            }
            int id = (int)colResp.ReturnValue[0].Id;

            // 3️⃣ Assign task to owner before editing
            Ts.AssignTask(owner, BoardName, 0, id, owner);

            // 4️⃣ Owner edits – should succeed
            Console.WriteLine("-----Running Test_EditTask_ByAssignee-----");
            string edit1 = Ts.EditTitle("NewT", id, owner, BoardName);
            Console.WriteLine(edit1.Contains("ErrorMessage\":null")
                ? "Success: Assignee edit allowed."
                : "Fail: Assignee edit rejected.");

            // 5️⃣ Bob (non-assignee) edits – should fail
            Console.WriteLine("-----Running Test_EditTask_ByNonAssignee (unassigned)-----");
            Us.Register("bob@example.com", DefaultPassword);
            Bs.JoinBoard("bob@example.com", 0);
            string edit2 = Ts.EditDesc("X", id, "bob@example.com", BoardName);
            var resp2 = JsonSerializer.Deserialize<Response<object>>(edit2);
            Console.WriteLine(resp2 != null && resp2.ErrorMessage != null
                ? "Success: Non-assignee edit blocked as expected."
                : "Fail: Non-assignee was able to edit.");

            // 6️⃣ Attempt to set due date before creation – should fail with 'cannot change creation'
            Console.WriteLine("-----Running Test_EditTask_CreationTimeImmutable-----");
            string edit3 = Ts.EditDue_Date(DateTime.UtcNow.AddYears(-1), id, owner, BoardName);
            var resp3 = JsonSerializer.Deserialize<Response<object>>(edit3);
            Console.WriteLine(resp3 != null
                && resp3.ErrorMessage != null
                && resp3.ErrorMessage.Contains("cannot change creation")
                ? "Success: Creation time immutable."
                : $"Fail: Creation time was changed or wrong error: {resp3?.ErrorMessage}");
        }

        /// <summary>Requirement 22: List all in-progress tasks across boards</summary>
        public void Test_ListInProgressTasks()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            Us.Register(owner, DefaultPassword);
            Bs.CreateBoard(BoardName, owner);
            Bs.CreateTask(BoardName, owner, "T1", "d", DateTime.Now.AddDays(1));
            Ts.UpdateState(owner, BoardName, 0, 0);

            Console.WriteLine("-----Running Test_ListInProgressTasks-----");
            // corrected to call the BoardService method
            string json = Bs.ListInProgTasks(owner);
            Console.WriteLine(json.Contains("T1")
                ? "Success: In-progress tasks listed."
                : "Fail: In-progress tasks missing.");
        }

        /// <summary>
        /// Requirement 23: Task assignment rules – only current assignee (creator) can reassign
        /// </summary>
        public void Test_AssignTask_Various()
        {
            Setup();
            string owner = $"own_{Guid.NewGuid():N}@kanban.com";
            string member = $"mem_{Guid.NewGuid():N}@kanban.com";
            string nonMember = "fake@x.com";
            Us.Register(owner, DefaultPassword);
            Us.Register(member, DefaultPassword);
            Bs.CreateBoard(BoardName, owner);
            Bs.JoinBoard(member, 0);
            Bs.CreateTask(BoardName, owner, "T", "d", DateTime.Now.AddDays(1));

            // fetch the task ID
            var colJson = Bs.GetColumn(owner, BoardName, 0);
            var colResp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(colJson);
            if (colResp == null || colResp.ErrorMessage != null || colResp.ReturnValue.Count == 0)
            {
                Console.WriteLine("Fail: Could not retrieve task for assignment");
                return;
            }
            int id = (int)colResp.ReturnValue[0].Id;

            // 1️⃣ Assign unassigned task (owner → member) — should succeed
            Console.WriteLine("-----Running Test_AssignTask_Success-----");
            string ok1 = Ts.AssignTask(owner, BoardName, 0, id, member);
            Console.WriteLine(ok1.Contains("ErrorMessage\":null")
                ? "Success: Assignment by board member allowed."
                : "Fail: Assignment failed.");

            // 2️⃣ Try assign to non-member — should fail
            Console.WriteLine("-----Running Test_AssignTask_AssigneeNotMember-----");
            string bad1 = Ts.AssignTask(member, BoardName, 0, id, nonMember);
            var resp1 = JsonSerializer.Deserialize<Response<object>>(bad1);
            Console.WriteLine(resp1 != null && resp1.ErrorMessage != null && resp1.ErrorMessage.Contains("not a member")
                ? "Success: Assignment to non-member rejected."
                : $"Fail: Assignment to non-member succeeded or wrong error: {resp1?.ErrorMessage}");

            // 3️⃣ Try reassign by *non-assignee* (owner tries reassign after member assigned) — should fail
            Console.WriteLine("-----Running Test_ReassignTask_ByNonAssignee-----");
            string bad2 = Ts.AssignTask(owner, BoardName, 0, id, owner);
            var resp2 = JsonSerializer.Deserialize<Response<object>>(bad2);
            Console.WriteLine(resp2 != null && resp2.ErrorMessage != null && resp2.ErrorMessage.Contains("Only the current assignee can reassign")
                ? "Success: Reassignment by non-assignee rejected."
                : $"Fail: Reassignment by non-assignee allowed or wrong error: {resp2?.ErrorMessage}");

            // 4️⃣ Try reassign by assignee (member → owner) — should succeed
            Console.WriteLine("-----Running Test_ReassignTask_ByAssignee-----");
            string ok2 = Ts.AssignTask(member, BoardName, 0, id, owner);
            Console.WriteLine(ok2.Contains("ErrorMessage\":null")
                ? "Success: Reassignment by assignee allowed."
                : "Fail: Reassignment by assignee failed.");
        }

        /// <summary>
        /// Call this once to run all task tests in sequence.
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("\n=== Task Tests ===\n");
            Setup();
            Test_LeaveBoard_UnassignsTasks();
            Test_CreateTask_Success();
            Test_CreateTask_EmptyTitle();
            Test_CreateTask_TitleTooLong();
            Test_CreateTask_DescTooLong();
            Test_CreateTask_PastDueDate();
            Test_CreateTask_Unauthorized();
            Test_DeleteTask_Success();
            Test_DeleteTask_NotExist();
            Test_CreateTask_NotMember();
            Test_MoveTask_Various();
            Test_EditTask_AssigneeOnly_ImmutableCreation();
            Test_ListInProgressTasks();
            Test_AssignTask_Various();
        }
    }
}
