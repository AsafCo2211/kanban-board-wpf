using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.Controllers
{
    internal class UserController
    {

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string UsersTableName = "Users";
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// Sets up the connection string and ensures the users table exists in the database.
        /// </summary>
        public UserController()
        {
            string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "kanban.db"));
            _connectionString = $"Data Source={path}; Version=3;";
            CreateTable();
        }

        /// <summary>
        /// Creates the Users table in the database if it does not already exist.
        /// </summary>
        /// <returns>True if the table was created or already exists; otherwise false.</returns>
        public bool CreateTable()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $@"CREATE TABLE IF NOT EXISTS {UsersTableName} (
                    {UserDAL.EmailColumnName} TEXT PRIMARY KEY,
                    {UserDAL.PasswordColumnName} TEXT NOT NULL);";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.ExecuteNonQuery();
                return true;
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
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The UserDAL object representing the user to add.</param>
        /// <returns>True if the user was added successfully; otherwise false.</returns>
        public bool AddNewUser(UserDAL user)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"INSERT INTO {UsersTableName} ({UserDAL.EmailColumnName}, {UserDAL.PasswordColumnName}) VALUES (@Email, @Password);";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error("AddNewUser failed", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Updates the password for the specified user in the database.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>True if the update succeeded; otherwise false.</returns>
        public bool UpdatePassword(string email, string newPassword)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"UPDATE {UsersTableName} SET {UserDAL.PasswordColumnName} = @Password WHERE {UserDAL.EmailColumnName} = @Email;";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", newPassword);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"UpdatePassword failed for {email}", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Loads all users from the database into memory.
        /// </summary>
        /// <returns>A list of UserDAL objects representing all users in the database.</returns>
        public List<UserDAL> LoadAll()
        {
            var users = new List<UserDAL>();
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"SELECT * FROM {UsersTableName};";
                using var cmd = new SQLiteCommand(query, connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(ConvertReaderToUser(reader));
                }
                return users;
            }
            catch (Exception ex)
            {
                Log.Error("LoadAll failed", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Deletes all users from the database.
        /// </summary>
        /// <returns>True if the deletion succeeded; otherwise false.</returns> //not needed currently
        public bool DeleteAll()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = $"DELETE FROM {UsersTableName};";
                using var cmd = new SQLiteCommand(query, connection);

                return cmd.ExecuteNonQuery() >= 0; // Return true if at least one row was affected (or zero if the table was empty)
            }
            catch (Exception ex)
            {
                Log.Error("DeleteAll failed", ex);
                throw;
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Converts a data row from the Users table into a UserDAL object.
        /// </summary>
        /// <param name="reader">The SQLiteDataReader positioned at the row to convert.</param>
        /// <returns>A UserDAL object representing the data in the current row.</returns>
        private UserDAL ConvertReaderToUser(SQLiteDataReader reader)
        {
            UserDAL user = new UserDAL(reader.GetString(0), reader.GetString(1), this);
            user.IsPersisted = true; // Mark as persisted since it was loaded from the database
            return user;
        }
    }
}

