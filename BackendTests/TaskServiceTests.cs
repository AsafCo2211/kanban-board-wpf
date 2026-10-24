using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Backend.ServiceLayer;
using NUnit.Framework;

namespace BackendTests
{
    [TestFixture]
    public class TaskServiceTests
    {
        private const string Email = "alice@example.com";
        private const string Password = "Secur3P@ss";
        private const string BoardName = "ProjectX";

        private ServiceFactory _factory;
        private TaskService _ts;
        private long _taskId;

        [SetUp]
        public void Init()
        {
            _factory = new ServiceFactory();
            _factory.DeleteAllData();

            _factory.Us.Register(Email, Password);

            _factory.Bs.CreateBoard(BoardName, Email);
            _factory.Bs.CreateTask(BoardName, Email, "Initial Title", "Initial Desc", DateTime.UtcNow.AddDays(7));

            var colJson = _factory.Bs.GetColumn(Email, BoardName, 0);
            var colRsp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(colJson);
            _taskId = colRsp.ReturnValue.First().Id;

            _ts = _factory.Ts;
        }

        /* ---------- positive paths ---------- */

        [Test]
        public void UpdateState_Good()
        {
            // Act
            var json = _ts.UpdateState(Email, BoardName, 0, (int)_taskId);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null);
        }

        [Test]
        public void EditTitle_Good()
        {
            // Arrange
            _factory.Ts.AssignTask(Email, BoardName, 0, (int)_taskId, Email);

            // Act
            var json = _ts.EditTitle("New title", _taskId, Email, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null);
        }

        [Test]
        public void EditDesc_Good()
        {
            // Arrange
            _factory.Ts.AssignTask(Email, BoardName, 0, (int)_taskId, Email);

            // Act
            var json = _ts.EditDesc("New desc", _taskId, Email, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null, $"Description edit by assignee failed: {rsp.ErrorMessage}");
        }

        [Test]
        public void EditDueDate_Good()
        {
            // Arrange
            _factory.Ts.AssignTask(Email, BoardName, 0, (int)_taskId, Email);
            var due = DateTime.UtcNow.AddDays(10);

            // Act
            var json = _ts.EditDue_Date(due, _taskId, Email, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null, $"Due date edit by assignee failed: {rsp.ErrorMessage}");
        }

        [Test]
        public void AssignTask_Good()
        {
            // Arrange
            const string assignee = "bob@example.com";
            _factory.Us.Register(assignee, "DiffP@ss1");
            _factory.Bs.JoinBoard(assignee, 0);

            // Act
            var json = _ts.AssignTask(Email, BoardName, 0, (int)_taskId, assignee);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null);
        }

        /* ---------- negative paths ---------- */

        [Test]
        public void UpdateState_BadTaskId()
        {
            // Act
            var json = _ts.UpdateState(Email, BoardName, 0, 999999);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public void UpdateState_BadColumn()
        {
            // Act
            var json = _ts.UpdateState(Email, BoardName, 5, (int)_taskId);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public void EditTitle_EmptyString()
        {
            // Act
            var json = _ts.EditTitle("", _taskId, Email, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public void EditDesc_EmptyString()
        {
            // Act
            var json = _ts.EditDesc("", _taskId, Email, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public void EditDueDate_PastDate()
        {
            // Arrange
            var past = DateTime.UtcNow.AddDays(-1);

            // Act
            var json = _ts.EditDue_Date(past, _taskId, Email, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public void AssignTask_BadAssignee()
        {
            // Act
            var json = _ts.AssignTask(Email, BoardName, 0, (int)_taskId, "charlie@nowhere.com");

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public void EditTitle_UnassignedTask_AllowsAnyUser()
        {
            // Arrange
            const string other = "eve@example.com";
            _factory.Us.Register(other, "Another1");
            _factory.Bs.JoinBoard(other, 0);

            // Act
            var json = _ts.EditTitle("Hack", _taskId, other, BoardName);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Is.Not.Null, "Edits on unassigned task by non-assignee should fail.");
        }

        [Test]
        public void EditTitle_AssignedTask_WrongUser_Fails()
        {
            // Arrange
            const string owner = "alice@example.com";
            const string other = "eve@example.com";

            // 1) Register both users
            _factory.Us.Register(owner, "Password1");
            _factory.Us.Register(other, "Password2");

            // 2) Create a board and a task in backlog
            _factory.Bs.CreateBoard(BoardName, owner);
            _factory.Bs.CreateTask(
                BoardName,                 // board name
                owner,                     // creator
                "OrigTitle",              // title
                "Desc",                   // description
                DateTime.UtcNow.AddDays(1) // due date
            );

            // 3) Retrieve the newly created task's ID
            var colJson = _factory.Bs.GetColumn(owner, BoardName, 0);
            var colResp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(colJson);
            Assert.That(colResp.ErrorMessage, Is.Null, "Should successfully fetch backlog column.");
            long taskId = colResp.ReturnValue.First().Id;

            // 4) Assign the task to the owner
            var assignJson = _factory.Ts.AssignTask(
                owner,       // caller email
                BoardName,   // board name
                0,           // backlog column ordinal
                (int)taskId, // task ID
                owner        // assignee email
            );
            var assignResp = JsonSerializer.Deserialize<Response<object>>(assignJson);
            Assert.That(assignResp.ErrorMessage, Is.Null, "Assignment to owner should succeed.");

            // 5) Attempt to edit as a different user

            // Act
            var editJson = _factory.Ts.EditTitle(
                "HackedTitle", // new title
                taskId,         // task ID
                other,          // a non-assignee
                BoardName       // board name
            );

            // Assert
            var editResp = JsonSerializer.Deserialize<Response<object>>(editJson);
            Assert.That(editResp.ErrorMessage, Is.Not.Null,
                "Only the assignee should be allowed to edit the title.");
        }

        [Test]
        public void UpdateDueDate_Invalid_Past_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs; var ts = fac.Ts;
            var email = $"e{Guid.NewGuid():N}@x.com";
            us.Register(email, "P4ssword");
            bs.CreateBoard("B", email);
            bs.CreateTask("B", email, "T", "d", DateTime.Now.AddDays(1));

            // Act
            var json = ts.EditDue_Date(DateTime.Now.AddDays(-1), 0, email, "B");

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void UpdateDueDate_ValidFuture_ShouldSucceed()
        {
            // Arrange
            _factory.Ts.AssignTask(Email, BoardName, 0, (int)_taskId, Email);
            var future = DateTime.UtcNow.AddDays(10);

            // Act
            var json = _ts.EditDue_Date(future, _taskId, Email, BoardName);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(resp.ErrorMessage, Is.Null, $"Valid future due-date failed: {resp.ErrorMessage}");
        }

        [Test]
        public void AdvanceTask_NotMember_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs; var ts = fac.Ts;
            var owner = $"o{Guid.NewGuid():N}@x.com";
            var member = $"m{Guid.NewGuid():N}@x.com";
            us.Register(owner, "P4ssword");
            us.Register(member, "P4ssword");
            bs.CreateBoard("B", owner);
            // never joined member
            bs.CreateTask("B", owner, "T", "d", DateTime.Now.AddDays(1));

            // Act
            var json = ts.UpdateState(member, "B", 0, 0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void AdvanceTask_WithoutAssignment_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs; var ts = fac.Ts;
            var u = $"u{Guid.NewGuid():N}@x.com";
            us.Register(u, "P4ssword");
            bs.CreateBoard("B", u);
            bs.CreateTask("B", u, "T", "d", DateTime.Now.AddDays(1));

            // Act
            var json = ts.UpdateState(u, "B", 0, 0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void AssignTask_NonMember_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs; var ts = fac.Ts;
            var owner = $"o{Guid.NewGuid():N}@x.com";
            var other = $"x{Guid.NewGuid():N}@x.com";
            us.Register(owner, "P4ssword");
            us.Register(other, "P4ssword");
            bs.CreateBoard("B", owner);
            bs.CreateTask("B", owner, "T", "d", DateTime.Now.AddDays(1));

            // Act
            var json = ts.AssignTask(owner, "B", 0, 0, other);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void UpdateDueDate_PastDate_ShouldFail()
        {
            // Arrange
            var past = DateTime.UtcNow.AddDays(-1);

            // Act
            var json = _ts.EditDue_Date(past, _taskId, Email, BoardName);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Past due-date was accepted but should be rejected.");
        }

        [Test]
        public void AdvanceTask_InvalidColumn_ShouldFail()
        {
            // Act
            var json = _ts.UpdateState(Email, BoardName, -1, (int)_taskId);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed advancing with invalid column index.");
        }

        [Test]
        public void AdvanceTask_InvalidTaskId_ShouldFail()
        {
            // Act
            var json = _ts.UpdateState(Email, BoardName, 0, int.MaxValue);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed advancing with invalid task ID.");
        }

        [Test]
        public void AssignTask_InvalidAssignee_ShouldFail()
        {
            // Arrange
            const string ghost = "ghost@example.com";

            // Act
            var json = _ts.AssignTask(Email, BoardName, 0, (int)_taskId, ghost);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed assignment to non-collaborator.");
        }

        [Test]
        public void CreateTask_DueDateBoundaryChecks()
        {
            // Arrange
            var email = $"user_{Guid.NewGuid():N}@x.com";
            var boardName = $"board_{Guid.NewGuid():N}";

            _factory.Us.Register(email, "P4ssword");
            _factory.Bs.CreateBoard(boardName, email);

            // Today at 00:00
            var todayMidnight = DateTime.Today;

            // Act
            var jsonToday = _factory.Bs.CreateTask(boardName, email, "TodayTask", "desc", todayMidnight);
            var respToday = JsonSerializer.Deserialize<Response<object>>(jsonToday);

            // Assert
            if (respToday != null && respToday.ErrorMessage != null)
            {
                Console.WriteLine($"Today at 00:00 correctly rejected: {respToday.ErrorMessage}");
            }
            else
            {
                Console.WriteLine("Fail: Today at 00:00 was accepted but should be rejected (already past).");
            }
            Assert.That(respToday?.ErrorMessage, Is.Not.Null, "Today at 00:00 should be rejected as past due date.");

            // Arrange
            var tomorrowMidnight = DateTime.Today.AddDays(1);

            // Act
            var jsonTomorrow = _factory.Bs.CreateTask(boardName, email, "TomorrowTask", "desc", tomorrowMidnight);
            var respTomorrow = JsonSerializer.Deserialize<Response<object>>(jsonTomorrow);

            // Assert
            if (respTomorrow != null && respTomorrow.ErrorMessage == null)
            {
                Console.WriteLine("Success: Tomorrow at 00:00 was accepted as valid.");
            }
            else
            {
                Console.WriteLine($"Fail: Tomorrow at 00:00 was wrongly rejected: {respTomorrow?.ErrorMessage}");
            }
            Assert.That(respTomorrow?.ErrorMessage, Is.Null, "Tomorrow at 00:00 should be accepted as valid future due date.");
        }
    }
}
