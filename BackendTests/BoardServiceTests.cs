using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Backend.ServiceLayer;
using NUnit.Framework;

namespace BackendTests
{
    [TestFixture]
    public class BoardServiceTests
    {
        private const string Alice = "alice@example.com";
        private const string AlicePwd = "Secur3P@ss";
        private const string Bob = "bob@example.com";
        private const string BobPwd = "DiffP@ss1";
        private const string BoardX = "ProjectX";

        private ServiceFactory _factory;
        private UserService _us;
        private BoardService _bs;

        [SetUp]
        public void Init()
        {
            // Arrange: fresh database and registered user
            _factory = new ServiceFactory();
            _factory.DeleteAllData();

            _us = _factory.Us;
            _us.Register(Alice, AlicePwd);

            _bs = _factory.Bs;
        }

        [Test]
        public void CreateBoard_NewBoard_Succeeds()
        {
            // Act
            var json = _bs.CreateBoard(BoardX, Alice);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void CreateBoard_DuplicateName_Fails()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var json = _bs.CreateBoard(BoardX.ToUpper(), Alice);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Does.Contain("already exists"));
            Assert.That(rsp.ReturnValue, Is.Null);
        }

        [Test]
        public void DeleteBoard_ExistingBoard_Succeeds()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var json = _bs.DeleteBoard(BoardX, Alice);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.That(rsp.ErrorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void DeleteBoard_Nonexistent_Fails()
        {
            // Act
            var json = _bs.DeleteBoard("NoSuchBoard", Alice);

            // Assert
            var rsp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.That(rsp.ErrorMessage, Does.Contain("does not exist"));
            Assert.That(rsp.ReturnValue, Is.Null);
        }

        [Test]
        public void GetUserBoards_NoBoards_ShouldReturnEmpty()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us;
            var bs = fac.Bs;
            var u = $"u{Guid.NewGuid():N}@x.com";
            us.Register(u, "P4ssword");

            // Act
            var json = bs.GetUserBoards(u);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<long[]>>(json);
            Assert.IsNull(resp.ErrorMessage, "Expected no error message when user has no boards.");
            Assert.IsNotNull(resp.ReturnValue, "Expected a valid board list (even if empty).");
            Assert.AreEqual(0, resp.ReturnValue.Length, "Expected empty list of boards.");
        }

        [Test]
        public void GetUserBoards_WithBoards_ReturnsIds()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var listJson = _bs.GetUserBoards(Alice);

            // Assert
            var listRsp = JsonSerializer.Deserialize<Response<List<long>>>(listJson);
            Assert.That(listRsp.ErrorMessage, Is.Null);
            Assert.That(listRsp.ReturnValue, Has.Count.EqualTo(1));
        }

        [Test]
        public void ChangeBoardOwner_OnlyNewOwnerCanDelete()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);
            var boardsJson = _bs.GetUserBoards(Alice);
            int id = (int)JsonSerializer.Deserialize<Response<List<long>>>(boardsJson).ReturnValue[0];

            _us.Register(Bob, BobPwd);
            _bs.JoinBoard(Bob, id);

            // Act & Assert: new owner assignment
            var chJson = _bs.ChangeBoardOwner(Alice, Bob, BoardX);
            var chRsp = JsonSerializer.Deserialize<Response<object>>(chJson);
            Assert.That(chRsp.ErrorMessage, Is.Null.Or.Empty);

            // Act & Assert: old owner cannot delete
            var failJson = _bs.DeleteBoard(BoardX, Alice);
            var failRsp = JsonSerializer.Deserialize<Response<string>>(failJson);
            Assert.That(failRsp.ErrorMessage, Does.Contain("not the owner"));

            // Act & Assert: new owner can delete
            var okJson = _bs.DeleteBoard(BoardX, Bob);
            var okRsp = JsonSerializer.Deserialize<Response<object>>(okJson);
            Assert.That(okRsp.ErrorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void OwnerCannotLeaveBoard()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);
            var boardsJson = _bs.GetUserBoards(Alice);
            int id = (int)JsonSerializer.Deserialize<Response<List<long>>>(boardsJson).ReturnValue[0];

            // Act
            var leaveJson = _bs.LeaveBoard(Alice, id);

            // Assert
            var leaveRsp = JsonSerializer.Deserialize<Response<string>>(leaveJson);
            Assert.That(leaveRsp.ErrorMessage, Does.Contain("cannot leave"));
        }

        [Test]
        public void MemberCanJoinAndLeaveBoard()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);
            var boardsJson = _bs.GetUserBoards(Alice);
            int id = (int)JsonSerializer.Deserialize<Response<List<long>>>(boardsJson).ReturnValue[0];

            _us.Register(Bob, BobPwd);

            // Act & Assert: join
            var joinJson = _bs.JoinBoard(Bob, id);
            var joinRsp = JsonSerializer.Deserialize<Response<object>>(joinJson);
            Assert.That(joinRsp.ErrorMessage, Is.Null.Or.Empty);

            // Act & Assert: leave
            var leaveJson = _bs.LeaveBoard(Bob, id);
            var leaveRsp = JsonSerializer.Deserialize<Response<object>>(leaveJson);
            Assert.That(leaveRsp.ErrorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void LimitNumOfTasks_AndEnforceLimit()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act & Assert: set limit
            var setJson = _bs.LimitNumOfTasks(BoardX, 0, Alice, 1);
            var setRsp = JsonSerializer.Deserialize<Response<object>>(setJson);
            Assert.That(setRsp.ErrorMessage, Is.Null.Or.Empty);

            // Act & Assert: first task
            var t1 = _bs.CreateTask(BoardX, Alice, "T1", "D1", DateTime.UtcNow.AddDays(1));
            var t1Rsp = JsonSerializer.Deserialize<Response<object>>(t1);
            Assert.That(t1Rsp.ErrorMessage, Is.Null.Or.Empty);

            // Act & Assert: second task fails
            var t2 = _bs.CreateTask(BoardX, Alice, "T2", "D2", DateTime.UtcNow.AddDays(2));
            var t2Rsp = JsonSerializer.Deserialize<Response<string>>(t2);
            Assert.That(t2Rsp.ErrorMessage, Does.Contain("limit"));
        }

        [Test]
        public void GetColumnName_And_GetColumnLimit_ValidAndInvalid()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act & Assert: valid name
            var nameJson = _bs.GetColumnName(Alice, BoardX, 0);
            var nameRsp = JsonSerializer.Deserialize<Response<string>>(nameJson);
            Assert.That(nameRsp.ReturnValue, Is.EqualTo("backlog").IgnoreCase);

            // Act & Assert: valid limit
            var limJson = _bs.GetColumnLimit(BoardX, Alice, 0);
            var limRsp = JsonSerializer.Deserialize<Response<int>>(limJson);
            Assert.That(limRsp.ReturnValue, Is.EqualTo(-1));

            // Act & Assert: invalid board name
            var badName = _bs.GetColumnName(Alice, "Bad", 0);
            var badNameRsp = JsonSerializer.Deserialize<Response<string>>(badName);
            Assert.That(badNameRsp.ErrorMessage, Does.Contain("does not exist"));

            // Act & Assert: invalid limit
            var badLim = _bs.GetColumnLimit("Bad", Alice, 0);
            var badLimRsp = JsonSerializer.Deserialize<Response<string>>(badLim);
            Assert.That(badLimRsp.ErrorMessage, Does.Contain("does not exist"));
        }

        [Test]
        public void CreateTask_And_GetColumn_ContainsTask()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var createJson = _bs.CreateTask(BoardX, Alice, "Title", "Desc", DateTime.UtcNow.AddDays(1));
            var createRsp = JsonSerializer.Deserialize<Response<object>>(createJson);

            // Assert Create
            Assert.That(createRsp.ErrorMessage, Is.Null.Or.Empty);

            // Act
            var colJson = _bs.GetColumn(Alice, BoardX, 0);
            var colRsp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(colJson);

            // Assert GetColumn
            Assert.That(colRsp.ReturnValue.Count, Is.EqualTo(1));
            Assert.That(colRsp.ReturnValue[0].Title, Is.EqualTo("Title"));
        }

        [Test]
        public void GetColumn_Valid_EmptyList()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs;
            var email = $"e{Guid.NewGuid():N}@x.com";
            us.Register(email, "P4ssword");
            bs.CreateBoard("B", email);

            // Act
            var json = bs.GetColumn(email, "B", 0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(json);
            Assert.IsNull(resp.ErrorMessage);
            Assert.IsNotNull(resp.ReturnValue);
            Assert.AreEqual(0, resp.ReturnValue.Count);
        }

        [Test]
        public void JoinBoard_NonExistentBoard_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs;
            var u = $"u{Guid.NewGuid():N}@x.com";
            us.Register(u, "P4ssword");

            // Act
            var json = bs.JoinBoard(u, 12345);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void LeaveBoard_NonExistentBoard_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs;
            var u = $"u{Guid.NewGuid():N}@x.com";
            us.Register(u, "P4ssword");

            // Act
            var json = bs.LeaveBoard(u, 9999);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void ChangeBoardOwner_NotOwner_Throws()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs;
            var owner = $"o{Guid.NewGuid():N}@x.com";
            var other = $"x{Guid.NewGuid():N}@x.com";
            us.Register(owner, "P4ssword");
            us.Register(other, "P4ssword");
            bs.CreateBoard("B", owner);

            // Act
            var json = bs.ChangeBoardOwner(other, "new@x.com", "B");

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json);
            Assert.IsNotNull(resp.ErrorMessage);
        }

        [Test]
        public void GetBoardName_Valid_Succeeds()
        {
            // Arrange
            var fac = new ServiceFactory();
            var us = fac.Us; var bs = fac.Bs;
            var u = $"u{Guid.NewGuid():N}@x.com";
            us.Register(u, "P4ssword");
            bs.CreateBoard("MyBoard", u);

            // Act
            var json = bs.GetBoardName(0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<string>>(json);
            Assert.IsNull(resp.ErrorMessage);
            Assert.AreEqual("myboard", resp.ReturnValue);
        }

        [Test]
        public void GetColumn_ValidBoard_ShouldSucceed()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var json = _bs.GetColumn(Alice, BoardX, 0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Null, "Valid GetColumn call failed.");
        }

        [Test]
        public void GetColumn_InvalidBoard_ShouldFail()
        {
            // Act
            var json = _bs.GetColumn(Alice, "NoSuchBoard", 0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<List<TaskSL>>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed GetColumn on non-existent board.");
        }

        [Test]
        public void JoinBoard_InvalidBoardId_ShouldFail()
        {
            // Act
            var json = _bs.JoinBoard(Alice, 9999);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed JoinBoard on invalid board ID.");
        }

        [Test]
        public void LeaveBoard_NotMember_ShouldFail()
        {
            // Arrange
            _us.Register(Bob, BobPwd);
            // Bob never joined BoardX (which we haven’t created yet)

            // Act
            var json = _bs.LeaveBoard(Bob, 0);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed LeaveBoard by non-member.");
        }

        [Test]
        public void TransferOwnership_InvalidNewOwner_ShouldFail()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var json = _bs.ChangeBoardOwner(Alice, "nouser@example.com", BoardX);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<object>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed ChangeBoardOwner to a user that doesn’t exist.");
        }

        [Test]
        public void GetBoardName_ValidId_ShouldSucceed()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);
            var ids = JsonSerializer.Deserialize<Response<List<long>>>(_bs.GetUserBoards(Alice))!.ReturnValue;
            var id = (int)ids.First();

            // Act
            var json = _bs.GetBoardName(id);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<string>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Null, "Valid GetBoardName call failed.");
        }

        [Test]
        public void GetBoardName_InvalidId_ShouldFail()
        {
            // Act
            var json = _bs.GetBoardName(int.MaxValue);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<string>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed GetBoardName on invalid ID.");
        }

        [Test]
        public void GetUserBoards_ValidUser_ShouldSucceed()
        {
            // Arrange
            _bs.CreateBoard(BoardX, Alice);

            // Act
            var json = _bs.GetUserBoards(Alice);

            // Assert
            var resp = JsonSerializer.Deserialize<Response<List<long>>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Null, "Valid GetUserBoards failed.");
            Assert.That(resp.ReturnValue.Count, Is.GreaterThan(0), "Expected at least one board.");
        }

        [Test]
        public void GetUserBoards_UnregisteredUser_ShouldFail()
        {
            // Act
            var json = _bs.GetUserBoards("ghost@example.com");

            // Assert
            var resp = JsonSerializer.Deserialize<Response<List<long>>>(json)!;
            Assert.That(resp.ErrorMessage, Is.Not.Null, "Allowed GetUserBoards for non-user.");
        }
    }
}
