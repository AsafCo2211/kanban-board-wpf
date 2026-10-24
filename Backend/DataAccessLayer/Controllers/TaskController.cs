using Backend.BuisnessLayer;
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
    internal class TaskController
    {
        private const string TasksTableName = "Tasks";
        private const string UsersTableName = "Users";
        private const string ColumnsTableName = "Columns";
        private readonly string _connectionString;
        private static readonly ILog Log = LogManager.GetLogger(typeof(TaskController));
        public TaskController()
        {
            string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "kanban.db"));
            _connectionString = $"Data Source={path}; Version=3;";
            CreateTable();
        }
        /// <summary>
        /// Creates the Tasks table in the database once and not in this class.
        /// </summary>
        /// <returns>True if the table was created or already exists; otherwise false.</returns>
        public bool CreateTable()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using (var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }
                string query = $@"CREATE TABLE IF NOT EXISTS {TasksTableName} (
                    {TaskDAL.TaskIDColumnName} INTEGER NOT NULL,
                    {TaskDAL.BoardIDColumnName} INTEGER NOT NULL,
                    {TaskDAL.TypeColumnName} TEXT NOT NULL,
                    {TaskDAL.TitleColumnName} TEXT NOT NULL,
                    {TaskDAL.DescriptionColumnName} TEXT,
                    {TaskDAL.DueDateColumnName} TEXT NOT NULL,
                    {TaskDAL.CreationTimeColumnName} TEXT NOT NULL,
                    {TaskDAL.AssigneeColumnName} TEXT,
                    PRIMARY KEY ({TaskDAL.BoardIDColumnName}, {TaskDAL.TaskIDColumnName}),
                    FOREIGN KEY ({TaskDAL.BoardIDColumnName}, {TaskDAL.TypeColumnName})
                        REFERENCES {ColumnsTableName}({ColumnDAL.BoardIDColumnName}, {ColumnDAL.TypeColumnName}),
                    FOREIGN KEY ({TaskDAL.AssigneeColumnName})
                        REFERENCES {UsersTableName}({UserDAL.EmailColumnName})
                );";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.ExecuteNonQuery();
                Log.Info("Tasks table ensured.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("CreateTable failed.", ex);
                throw;
            }
        }

        /// <summary>
        /// Adds a new task to the database.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>True if the task was inserted; otherwise false.</returns>
        public bool AddTask(TaskDAL task)
        {
            try
            {
                return ExecuteNonQuery($@"
                INSERT INTO {TasksTableName} 
                    ({TaskDAL.TaskIDColumnName}, {TaskDAL.BoardIDColumnName}, {TaskDAL.TypeColumnName}, 
                    {TaskDAL.TitleColumnName}, {TaskDAL.DescriptionColumnName}, {TaskDAL.DueDateColumnName}, 
                    {TaskDAL.CreationTimeColumnName}, {TaskDAL.AssigneeColumnName})
                VALUES (@id, @boardID, @type, @title, @desc, @due, @create, @assignee);",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@id", task.TaskID);
                        cmd.Parameters.AddWithValue("@boardID", task.BoardID);
                        cmd.Parameters.AddWithValue("@type", task.Type);
                        cmd.Parameters.AddWithValue("@title", task.Title);
                        // allow description to be NULL too, if desired
                        cmd.Parameters.AddWithValue("@desc", (object)task.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@due", task.DueDate.ToString("o"));
                        cmd.Parameters.AddWithValue("@create", task.CreationTime.ToString("o"));
                        // bind a real NULL when Assignee is null, else bind the email
                        cmd.Parameters.AddWithValue(
                            "@assignee",
                            task.Assignee != null
                            ? (object)task.Assignee
                            : DBNull.Value
                        );
                    }
                ) > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"AddTask failed for task {task.TaskID}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a specific task from the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <returns></returns>
        public bool DeleteTask(long boardID, long taskID)
        {
            try
            {
                return ExecuteNonQuery(
                    $"DELETE FROM {TasksTableName} WHERE {TaskDAL.BoardIDColumnName} = @boardID AND {TaskDAL.TaskIDColumnName} = @id;",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@boardID", boardID);
                        cmd.Parameters.AddWithValue("@id", taskID);
                    }
                ) > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteTask failed for task {taskID}.", ex);
                throw;
            }
        }
        /// <summary>
        /// Updates a specific field of a task in the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool UpdateField(long boardID, long taskID, string column, object value)
        {
            try
            {
                return ExecuteNonQuery(
                    $"UPDATE {TasksTableName} SET {column} = @value WHERE {TaskDAL.BoardIDColumnName} = @boardID AND {TaskDAL.TaskIDColumnName} = @id;",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@value", value);
                        cmd.Parameters.AddWithValue("@boardID", boardID);
                        cmd.Parameters.AddWithValue("@id", taskID);
                    }
                ) > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"Update {column} failed for task {taskID}.", ex);
                throw;
            }
        }
        /// <summary>
        /// Selects all tasks assigned to a specific user that are currently in progress.
        /// </summary>
        /// <param name="assignee"></param>
        /// <returns></returns>
        public List<TaskDAL> SelectInProgTasks(string assignee)
        {
            var tasks = new List<TaskDAL>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                string query = $@"SELECT * FROM {TasksTableName} 
                          WHERE {TaskDAL.AssigneeColumnName} = @assignee 
                          AND {TaskDAL.TypeColumnName} = 'in progress';";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@assignee", assignee);

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
                        this
                    );
                    taskToAdd.IsPersisted = true; // Mark as persisted since we retrieved it from the DB
                    tasks.Add(taskToAdd);
                }

                Log.Info($"Retrieved {tasks.Count} in progress tasks for assignee '{assignee}'.");
                return tasks;
            }
            catch (Exception ex)
            {
                Log.Error($"SelectInProgTasks failed for assignee '{assignee}'.", ex);
                // Optionally: throw or just return empty list depending on desired behavior
                throw;
            }
        }

        /// <summary>
        /// Updates the title of a task in the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <param name="newTitle"></param>
        /// <returns></returns>
        public bool UpdateTitle(long boardID, long taskID, string newTitle)
        {
            return UpdateField(boardID, taskID, TaskDAL.TitleColumnName, newTitle);
        }
        /// <summary>
        /// Updates the description of a task in the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <param name="newDesc"></param>
        /// <returns></returns>
        public bool UpdateDescription(long boardID, long taskID, string newDesc)
        {
            return UpdateField(boardID, taskID, TaskDAL.DescriptionColumnName, newDesc);
        }
        /// <summary>
        /// Updates the due date of a task in the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <param name="newDue"></param>
        /// <returns></returns>
        public bool UpdateDueTime(long boardID, long taskID, DateTime newDue)
        {
            return UpdateField(boardID, taskID, TaskDAL.DueDateColumnName, newDue.ToString("o"));
        }
        /// <summary>
        /// Updates the assignee of a task in the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <param name="newAssignee"></param>
        /// <returns></returns>
        public bool UpdateAssignee(long boardID, long taskID, string newAssignee)
        {
            return UpdateField(boardID, taskID, TaskDAL.AssigneeColumnName, newAssignee);
        }
        /// <summary>
        /// Updates the type of a task in the database based on its ID and the board it belongs to.
        /// </summary>
        /// <param name="boardID"></param>
        /// <param name="taskID"></param>
        /// <param name="newType"></param>
        /// <returns></returns>
        public bool UpdateType(long boardID, long taskID, string newType)
        {
            return UpdateField(boardID, taskID, TaskDAL.TypeColumnName, newType);
        }

        /// <summary>
        /// Executes a non-query SQL command with the provided query and parameter setter action.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameterSetter"></param>
        /// <returns>The number of rows affected by the command.</returns>
        private int ExecuteNonQuery(string query, Action<SQLiteCommand> parameterSetter)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var command = new SQLiteCommand(query, connection);
            parameterSetter(command);
            return command.ExecuteNonQuery();
        }
        /// <summary>
        /// Loads all tasks from the database and returns them as a list of TaskDAL objects.
        /// </summary>
        /// <returns></returns>
        public List<TaskDAL> LoadAllTasks()
        {
            try
            {
                var tasks = new List<TaskDAL>();
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var cmd = new SQLiteCommand($"SELECT * FROM {TasksTableName};", connection);
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
                        this
                    );
                    taskToAdd.IsPersisted = true; // Mark as persisted since we retrieved it from the DB
                    tasks.Add(taskToAdd);
                }
                Log.Info("Loaded all tasks from DB.");
                return tasks;
            }
            catch (Exception ex)
            {
                Log.Error("LoadAllTasks failed.", ex);
                throw; 
            }
        }


    }
}