using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.Controllers
{
    internal class ColumnController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ColumnController));
        private const string ColumnsTableName = "Columns";
        private const string TasksTableName = "Tasks";
        private const string BoardsTableName = "Boards";
        private readonly string _connectionString;
        private TaskController taskController;

        public ColumnController(TaskController tc)
        {
            string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "kanban.db"));
            _connectionString = $"Data Source={path}; Version=3;";
            CreateTable();
            this.taskController = tc;
        }

        /// <summary>
        /// Creates the Columns table in the database if it does not already exist.
        /// </summary>
        /// <returns>True if the table was created or already exists; otherwise false.</returns>
        public bool CreateTable()
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

                string query = $@"CREATE TABLE IF NOT EXISTS {ColumnsTableName} (
                    {ColumnDAL.BoardIDColumnName} INTEGER NOT NULL,
                    {ColumnDAL.TypeColumnName} TEXT NOT NULL,
                    {ColumnDAL.ColumnLimitColumnName} INTEGER NOT NULL,
                    PRIMARY KEY ({ColumnDAL.BoardIDColumnName}, {ColumnDAL.TypeColumnName}),
                    FOREIGN KEY ({ColumnDAL.BoardIDColumnName}) REFERENCES {BoardsTableName}({BoardDAL.boardIDColumnName})
                );";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.ExecuteNonQuery();
                Log.Info("Columns table created or already exists.");  //  Added success log
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("CreateTable failed for Columns table.", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }
        /// <summary>
        /// Adds a new column to the database.
        /// </summary>
        /// <param name="column"></param>
        /// <returns>True if the column was added; otherwise false.</returns>
        public bool AddColumn(ColumnDAL column)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"INSERT INTO {ColumnsTableName} ({ColumnDAL.BoardIDColumnName}, {ColumnDAL.TypeColumnName}, {ColumnDAL.ColumnLimitColumnName}) VALUES (@boardID, @type, @limit)";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@boardID", column.BoardID);
                cmd.Parameters.AddWithValue("@type", column.Type);
                cmd.Parameters.AddWithValue("@limit", column.ColumnLimit);
                bool success = cmd.ExecuteNonQuery() > 0;
                if (success)
                {
                    Log.Info($"Added column '{column.Type}' for board '{column.BoardID}'.");  //  Added success log
                    return success;
                }
                throw new Exception("Couldnt add column.");
            }
            catch (Exception ex)
            {
                Log.Error($"AddColumn failed for board '{column.BoardID}', column '{column.Type}'.", ex);  //  Added error log
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Updates the task limit of a specific column.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="type"></param>
        /// <param name="newLimit"></param>
        /// <returns>True if the column limit was updated; otherwise false.</returns>
        public bool UpdateColumnLimit(long boardID, string type, int newLimit)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"UPDATE {ColumnsTableName} SET {ColumnDAL.ColumnLimitColumnName} = @limit WHERE {ColumnDAL.BoardIDColumnName} = @boardID AND {ColumnDAL.TypeColumnName} = @type";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@limit", newLimit);
                cmd.Parameters.AddWithValue("@boardID", boardID);
                cmd.Parameters.AddWithValue("@type", type);
                bool success = cmd.ExecuteNonQuery() > 0;
                if (success)
                {
                    Log.Info($"Updated column limit to {newLimit} for '{type}' in board '{boardID}'."); // Added success log  
                    return success;
                }
                throw new Exception("Couldnt update column limit.");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateColumnLimit failed for board '{boardID}', column '{type}'.", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Retrieves all tasks in a specific column by board ID and column type.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="type"></param>
        /// <returns>A list of TaskDALs in the specified column.</returns>
        public List<TaskDAL> SelectTasksInColumn(long boardID, string type)
        {
            var tasks = new List<TaskDAL>();
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"SELECT * FROM {TasksTableName} WHERE {TaskDAL.BoardIDColumnName} = @boardID AND {TaskDAL.TypeColumnName} = @type";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@boardID", boardID);
                cmd.Parameters.AddWithValue("@type", type);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TaskDAL taskToAdd = new TaskDAL(
                       reader.GetInt64(0),
                       reader.GetInt64(1),
                       reader.GetString(2),
                       reader.GetString(3),
                       reader.GetString(4),
                       DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
                       DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
                       reader.IsDBNull(7) ? null : reader.GetString(7),
                       taskController
                   );
                    taskToAdd.IsPersisted = true; // Mark as persisted since we retrieved it from the DB
                    tasks.Add(taskToAdd);
                }
                Log.Info($"Selected {tasks.Count} tasks in column '{type}' for board '{boardID}'.");  //  Added success log
                return tasks;
            }
            catch (Exception ex)
            {
                Log.Error($"SelectTasksInColumn failed for board '{boardID}', column '{type}'.", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Selects a column from the database by board ID and column type.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="type"></param>
        /// <returns>A ColumnDAL if found; otherwise null.</returns>
        public ColumnDAL SelectColumn(long boardID, string type)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"SELECT * FROM {ColumnsTableName} WHERE {ColumnDAL.BoardIDColumnName} = @boardID AND {ColumnDAL.TypeColumnName} = @type;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@boardID", boardID);
                cmd.Parameters.AddWithValue("@type", type);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ColumnDAL column = new ColumnDAL(
                        reader.GetInt64(0),     // BoardID
                        reader.GetString(1),    // Type
                        reader.GetInt32(2),      // ColumnLimit
                        this
                    );
                    column.IsPersisted = true; // Mark as persisted
                    Log.Info($"Selected column '{type}' for board '{boardID}'.");  //  Added success log
                    return column;
                }
                throw new Exception("Couldnt retrieve the column.");
            }
            catch (Exception ex)
            {
                Log.Error($"SelectColumn failed for board '{boardID}', column '{type}'.", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Loads all columns from the database.
        /// </summary>
        /// <returns></returns>
        public List<ColumnDAL> LoadAllColumns()
        {
            try
            {
                var columns = new List<ColumnDAL>();
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var cmd = new SQLiteCommand($"SELECT * FROM {ColumnsTableName};", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ColumnDAL column = new ColumnDAL(
                        reader.GetInt64(0),     // BoardID
                        reader.GetString(1),    // Type
                        reader.GetInt32(2),      // ColumnLimit
                        this
                    );
                    column.IsPersisted = true; // Mark as persisted
                    columns.Add(column);
                }
                Log.Info("Loaded all columns from DB.");
                return columns;
            }
            catch (Exception ex)
            {
                Log.Error("LoadAllColumns failed.", ex);
                throw;
            }
        }
        /// <summary>
        /// Deletes a column from the database by board ID and column type.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool DeleteColumn(long boardID, string type)
        {
            try
            {
                return ExecuteNonQuery(
                    $"DELETE FROM Columns WHERE {ColumnDAL.BoardIDColumnName} = @boardID AND {ColumnDAL.TypeColumnName} = @type;",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@boardID", boardID);
                        cmd.Parameters.AddWithValue("@type", type);
                    }
                ) > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteColumn failed for board '{boardID}', column '{type}'.", ex);
                throw;
            }
        }
        /// <summary>
        /// Executes a non-query command against the database.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameterSetter"></param>
        /// <returns></returns>
        private int ExecuteNonQuery(string query, Action<SQLiteCommand> parameterSetter)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var cmd = new SQLiteCommand(query, connection);
            parameterSetter(cmd);
            return cmd.ExecuteNonQuery();
        }
    }
}