using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.DTOs
{
    internal class BoardDAL
    {
        public const string boardIDColumnName = "boardID";
        public const string ownerColumnName = "owner";
        public const string boardNameColumnName = "boardName";
        private long boardID;
        private string boardName;
        private string owner;
        private BoardController controller;
        private bool isPersisted = false;

        public BoardDAL(long boardID, string boardName, string owner, BoardController bc)
        {
            this.boardID = boardID;
            this.boardName =  boardName;
            this.owner = owner;
            this.controller = bc;

        }
        public bool IsPersisted { get => isPersisted; set { isPersisted = value; } }
        public long BoardID
        {
            get => boardID;
        }

        public string BoardName
        {
            get => boardName;
            set
            {
                if (!isPersisted)
                {
                    throw new Exception($"Board {boardID} is not persisted; cannot update board name.");
                }

                if (!controller.UpdateBoardName(boardID, value))
                {
                    throw new Exception($"Board {boardID} update failed: could not update board name in DB.");
                }

                boardName = value;
            }
        }

        public string Owner
        {
            get => owner;
            set
            {
                if (!isPersisted)
                {
                    throw new Exception($"Board {boardID} is not persisted; cannot change owner.");
                }

                if (!controller.ChangeOwner(boardID, owner, value))
                {
                    throw new Exception($"Board {boardID} update failed: could not change owner in DB.");
                }

                owner = value;
            }
        }

        /// <summary>
        /// Saves the board to the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public bool Save()
        {
            if(isPersisted) // If the board is already persisted, we throw an exception
            {
                throw new Exception("Board is already persisted in the database.");
            }

            if(!controller.AddBoard(this)) // Attempt to add the board to the database, if it fails, an exception is thrown
            {
                throw new Exception("Failed to save board to the database.");
            }

            isPersisted = true; // If the board was successfully added to the database, we set isPersisted to true
            return true;
        }
        /// <summary>
        /// Deletes the board from the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Delete()
        {
            if (!isPersisted)
            {
                throw new Exception($"Board {boardID} is not persisted; cannot delete.");
            }

            if (!controller.DeleteBoard(boardID))
            {
                throw new Exception($"Failed to delete board {boardID} from DB.");
            }

            isPersisted = false; 

        }
    }
}

