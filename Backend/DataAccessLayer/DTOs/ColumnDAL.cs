using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.DTOs
{
    internal class ColumnDAL
    {
        public const string BoardIDColumnName = "boardID";
        public const string TypeColumnName = "type";
        public const string ColumnLimitColumnName = "columnLimit";

        private int columnLimit;
        private ColumnController controller;
        private bool isPersisted = false;

        public bool IsPersisted { get => isPersisted; set { isPersisted = value; } }
        public long BoardID { get; }
        public string Type { get; }
        public int ColumnLimit { get => columnLimit; 
            set 
            {
                if (!isPersisted)
                {
                    throw new Exception($"Column {Type} in board {BoardID} is not persisted; cannot update column limit.");
                }

                if (!controller.UpdateColumnLimit(BoardID, Type, value))
                {
                    throw new Exception($"Failed to update column limit for column {Type} in board {BoardID}.");
                }

                columnLimit = value;
            } 
        }

        public ColumnDAL(long boardID, string type, int columnLimit, ColumnController cc)
        {
            BoardID = boardID;
            Type = type;
            this.columnLimit = columnLimit;
            this.controller = cc;
        }

        /// <summary>
        /// Saves the column to the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Save()
        {
            if (isPersisted) // If the column is already persisted, we throw an exception
            {
                throw new Exception("Column is already persisted in the database.");
            }

            if (!controller.AddColumn(this)) // Attempt to add the column to the database, if it fails, an exception is thrown
            {
                throw new Exception("Failed to save column to the database.");
            }

            isPersisted = true;
        }
        /// <summary>
        /// Deletes the column from the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Delete()
        {
            if (!isPersisted)
            {
                throw new Exception($"Column {Type} in board {BoardID} is not persisted; cannot delete.");
            }

            if (!controller.DeleteColumn(BoardID, Type))
            {
                throw new Exception($"Failed to delete column {Type} in board {BoardID} from DB.");
            }

            isPersisted = false;
        }
    }
}
