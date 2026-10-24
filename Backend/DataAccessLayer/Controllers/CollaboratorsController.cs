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
    internal class CollaboratorsController
    {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
        private const string CollaboratorsTableName = "Collaborators";
        private const string UsersTableName = "Users";
        private const string BoardsTableName = "Boards";
        private readonly string _connectionString;
        private static readonly ILog Log = LogManager.GetLogger(typeof(CollaboratorsController));

        public CollaboratorsController()
        {
            string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "kanban.db"));
            _connectionString = $"Data Source={path}; Version=3;";
            CreateTable();
        }
        /// <summary>
        /// Creates the Collaborators table in the database if it does not already exist.
        /// </summary>
        /// <returns>True if the table was created or already exists; otherwise false.</returns>
        public void CreateTable()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using (var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }
                string query = $@"CREATE TABLE IF NOT EXISTS {CollaboratorsTableName} (
                    {CollaboratorDAL.BoardIDColumnName} INTEGER NOT NULL,
                    {CollaboratorDAL.EmailColumnName} TEXT NOT NULL,
                    PRIMARY KEY ({CollaboratorDAL.BoardIDColumnName}, {CollaboratorDAL.EmailColumnName}),
                    FOREIGN KEY ({CollaboratorDAL.BoardIDColumnName}) REFERENCES {BoardsTableName}({BoardDAL.boardIDColumnName}),
                    FOREIGN KEY ({CollaboratorDAL.EmailColumnName}) REFERENCES {UsersTableName}({UserDAL.EmailColumnName})
                );";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.ExecuteNonQuery();
                Log.Info("Collaborators table ensured.");
            }
            catch (Exception ex)
            {
                Log.Error("CreateTable failed for Collaborators table.", ex);
                throw;
            }
        }
        /// <summary>
        /// Adds a new collaborator to the specified board in the database.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool AddCollaborator(CollaboratorDAL colabDal)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"INSERT INTO {CollaboratorsTableName} ({CollaboratorDAL.BoardIDColumnName}, {CollaboratorDAL.EmailColumnName}) VALUES (@boardID, @email);";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@boardID", colabDal.BoardID);
                cmd.Parameters.AddWithValue("@email", colabDal.Email);
                int rowsAffected = cmd.ExecuteNonQuery();
                Log.Info($"Added collaborator '{colabDal.Email}' to board {colabDal.BoardID}.");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"AddCollaborator failed for board {colabDal.BoardID} , email ' {colabDal.Email}'.", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a collaborator from the specified board in the database.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool DeleteCollaborator(long boardID, string email)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"DELETE FROM {CollaboratorsTableName} WHERE {CollaboratorDAL.BoardIDColumnName} = @boardID AND {CollaboratorDAL.EmailColumnName} = @email;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@boardID", boardID);
                cmd.Parameters.AddWithValue("@email", email);
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Log.Info($"Deleted collaborator '{email}' from board {boardID}.");
                    return true;
                }
                else
                {
                    Log.Error($"No collaborator '{email}' found on board {boardID} to delete.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteCollaborator failed for board {boardID}, email '{email}'.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a list of all collaborator emails for a given board.
        /// </summary>
        /// <param name="boardID"></param>
        /// <returns>A list of collaborator emails.</returns>
        public List<CollaboratorDAL> LoadAllCollaborators()
        {
            try
            {
                var collabs = new List<CollaboratorDAL>();
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var cmd = new SQLiteCommand($"SELECT * FROM {CollaboratorsTableName};", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    CollaboratorDAL collab = new CollaboratorDAL(
                        reader.GetInt64(0),    // BoardID
                        reader.GetString(1),    // Email
                        this
                    );
                    collab.IsPersisted = true;
                    collabs.Add(collab);
                }
                Log.Info("Loaded all collaborators from DB.");
                return collabs;
            }
            catch (Exception ex)
            {
                Log.Error("LoadAllCollaborators failed.", ex);
                throw;
            }
        }
        /// <summary>
        /// Deletes all collaborators for a specific board from the database.
        /// </summary>
        /// <param name="boardID"></param>
        /// <returns></returns>
        public bool DeleteAllCollaboratorsForBoard(long boardID)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"DELETE FROM Collaborators WHERE {CollaboratorDAL.BoardIDColumnName} = @boardID;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@boardID", boardID);

                int rowsAffected = cmd.ExecuteNonQuery();

                Log.Info($"Deleted {rowsAffected} collaborators from board {boardID}.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteAllCollaboratorsForBoard failed for board {boardID}.", ex);
                throw;
            }
        }


    }
}
