using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("BackendTests")]


namespace IntroSE.Kanban.Backend.DataAccessLayer.DTOs
{
    internal class CollaboratorDAL
    {
        public const string BoardIDColumnName = "boardID";
        public const string EmailColumnName = "email";

        private CollaboratorsController controller;
        private bool isPersisted = false;

        public long BoardID { get; }
        public string Email { get; }

        public CollaboratorDAL(long boardID, string email, CollaboratorsController controller)
        {
            BoardID = boardID;
            Email = email;
            this.controller = controller;
        }
        public bool IsPersisted { get => isPersisted; set { isPersisted = value; } }
        /// <summary>
        /// Saves the collaborator to the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public bool Save()
        {
            if (isPersisted) // If the collaborator is already persisted, we throw an exception
            {
                throw new Exception("Board is already persisted in the database.");
            }

            if (!controller.AddCollaborator(this)) // Attempt to add the collab to the database, if it fails, an exception is thrown
            {
                throw new Exception("Failed to save board to the database.");
            }

            isPersisted = true; // If the collab was successfully added to the database, we set isPersisted to true
            return true;
        }
        /// <summary>
        /// Deletes the collaborator from the database.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Delete() //deletes one collab from board
        {
            if (!isPersisted)
            {
                return;  // Just skip, no error
            }

            if (!controller.DeleteCollaborator(BoardID, Email))
            {
                throw new Exception($"Failed to delete collaborator {Email} on board {BoardID} from DB.");
            }

            isPersisted = false;
        }
        /// <summary>
        /// Deletes all collaborators for a certain board.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool DeleteCollaboratorsForBoard() //deletes all collabs for a certain board
        {
            if (!isPersisted)
            {
                throw new Exception($"Collaborators for board {BoardID} are not marked as persisted; cannot delete.");
            }

            if (!controller.DeleteAllCollaboratorsForBoard(BoardID))
            {
                throw new Exception($"Failed to delete collaborators for board {BoardID}.");
            }

            isPersisted = false;
            return true;
        }
    }
}
