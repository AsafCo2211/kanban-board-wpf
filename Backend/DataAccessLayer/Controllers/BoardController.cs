using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.Controllers
{
    internal class BoardController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BoardController));
        private const string BoardsTableName = "Boards";
        private const string UsersTableName = "Users";
        private const string ColumnsTableName = "Columns";
        private const string TasksTableName = "Tasks";
        private const string CollabsTableName = "Collaborators";
        private readonly string _connectionString;
        public BoardController()
        {
            string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "kanban.db"));
            this._connectionString = $"Data Source={path}; Version=3;";
            CreateTable();
        }
        /// <summary>
        /// Creates the Boards table in the database if it does not already exist. Will be used exterior to this class
        /// </summary>
        /// <returns>True if the table was created or already exists; otherwise false.</returns>
        public void CreateTable()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using (var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }
                string query = $@"CREATE TABLE IF NOT EXISTS {BoardsTableName} (
                    {BoardDAL.boardIDColumnName} INTEGER PRIMARY KEY,
                    {BoardDAL.boardNameColumnName} TEXT NOT NULL,
                    {BoardDAL.ownerColumnName} TEXT NOT NULL,
                    FOREIGN KEY ({BoardDAL.ownerColumnName}) REFERENCES {UsersTableName}({UserDAL.EmailColumnName})
                );";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.ExecuteNonQuery();
                Log.Info("Created board table successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("CreateTable failed", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }
        /// <summary>
        /// Adds a new board to the database.
        /// </summary>
        /// <param name="board"></param>
        /// <returns>True if a board was inserted; otherwise false.</returns>
        public bool AddBoard(BoardDAL board)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"INSERT INTO {BoardsTableName}
                    ({BoardDAL.boardIDColumnName}, {BoardDAL.boardNameColumnName}, {BoardDAL.ownerColumnName})
                    VALUES (@ID, @Name, @Owner);";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", board.BoardID);
                cmd.Parameters.AddWithValue("@Name", board.BoardName);
                cmd.Parameters.AddWithValue("@Owner", board.Owner);
                Log.Info($"Successfully added board {board.BoardID}");

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"AddBoard failed for {board.BoardID}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// Deletes a board from the database based on its ID.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public bool DeleteBoard(long boardID)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"DELETE FROM {BoardsTableName}
                          WHERE {BoardDAL.boardIDColumnName} = @ID;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", boardID);
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Log.Info($"Successfully deleted board {boardID}");
                    return true;    
                }
                else
                {
                    Log.Error($"DeleteBoard: No board with ID {boardID} found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteBoard failed for {boardID}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Updates the name of a board in the database based on its ID.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool UpdateBoardName(long boardID, string newName)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"UPDATE {BoardsTableName}
                    SET {BoardDAL.boardNameColumnName} = @NewName
                    WHERE {BoardDAL.boardIDColumnName} = @ID;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@NewName", newName);
                cmd.Parameters.AddWithValue("@ID", boardID);
                if (cmd.ExecuteNonQuery() > 0)
                {
                    Log.Info($"Successfully updated board name for {boardID}");
                    return true;
                }
                return false;
                //throw new Exception("Couldnt update board name in db.");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateBoardName failed for {boardID}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }
        /// <summary>
        /// Changes the owner of a board in the database based on its ID and the former owner's email.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="formerOwner"></param>
        /// <param name="newOwner"></param>
        /// <returns>True if the board owner was updated; otherwise false.</returns>
        public bool ChangeOwner(long boardID, string formerOwner, string newOwner)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"UPDATE {BoardsTableName}
                    SET {BoardDAL.ownerColumnName} = @NewOwner
                    WHERE {BoardDAL.boardIDColumnName} = @ID AND {BoardDAL.ownerColumnName} = @OldOwner;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@NewOwner", newOwner);
                cmd.Parameters.AddWithValue("@OldOwner", formerOwner);
                cmd.Parameters.AddWithValue("@ID", boardID);
                if (cmd.ExecuteNonQuery() > 0)
                {
                    Log.Info($"Successfully updated board owner for {boardID}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"ChangeOwner failed for {boardID}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Retrieves all boards owned by a specific user from the database.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public List<BoardDAL> GetAllBoardsForUser(string email)
        {
            var boards = new List<BoardDAL>();
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"SELECT * FROM {BoardsTableName}
                    WHERE {BoardDAL.ownerColumnName} = @Email;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Email", email);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BoardDAL board = new BoardDAL(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), this);
                    board.IsPersisted = true; // Mark as persisted since it was loaded from the database
                    boards.Add(board);
                }
                return boards;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllBoardsForUser failed for {email}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Selects a board from the database based on its ID.
        /// </summary>
        /// <param name="boardID"></param>
        /// <returns></returns>
        public BoardDAL SelectBoard(long boardID)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"SELECT * FROM {BoardsTableName}
                    WHERE {BoardDAL.boardIDColumnName} = @ID;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", boardID);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    BoardDAL board = new BoardDAL(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), this);
                    board.IsPersisted = true; // Mark as persisted since it was loaded from the database
                    return board;
                }

                throw new Exception("Couldnt retrieve board from the DB.");
            }
            catch (Exception ex)
            {
                Log.Error($"SelectBoard failed for {boardID}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Deletes all boards from the database. Also deletes all columns, collabs and tasks associated with those boards.
        /// </summary>
        /// <returns>True if the deletion succeeded; otherwise false.</returns> //not needed currently
        public bool DeleteAllData()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                // Disable foreign key constraints
                using var pragmaOffCmd = new SQLiteCommand("PRAGMA foreign_keys = OFF;", connection);
                pragmaOffCmd.ExecuteNonQuery();

                var tables = new[] { TasksTableName, ColumnsTableName, CollabsTableName, BoardsTableName };
                foreach (var table in tables)
                {
                    using var cmd = new SQLiteCommand($"DELETE FROM {table};", connection);
                    cmd.ExecuteNonQuery();
                }
                using var pragmaOnCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection);
                pragmaOnCmd.ExecuteNonQuery();
                Log.Info("Deleted all data from Tasks, Columns, Collaborators, and Boards tables.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("DeleteAllData failed.", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }
        /// <summary>
        /// Loads all boards from the database into memory.
        /// </summary>
        /// <returns></returns>
        public List<BoardDAL> LoadAllBoards()
        {
            try
            {
                var boards = new List<BoardDAL>();
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var cmd = new SQLiteCommand($"SELECT * FROM {BoardsTableName};", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BoardDAL add = new BoardDAL(
                        reader.GetInt64(0),     // BoardID
                        reader.GetString(1),    // BoardName
                        reader.GetString(2),     // BoardOwner
                        this
                    );
                    add.IsPersisted = true; // Mark as persisted since it was loaded from the database
                    boards.Add(add);
                }
                Log.Info("Loaded all boards from DB.");
                return boards;
            }
            catch (Exception ex)
            {
                Log.Error("LoadAllBoards failed.", ex);
                throw;
            }
        }
    }
}
