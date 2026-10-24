using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IntroSE.Kanban.Backend.DataAccessLayer.Controllers;
using IntroSE.Kanban.Backend.DataAccessLayer.DTOs;
using log4net;

namespace IntroSE.Kanban.Backend.BuisnessLayer
{
    internal class Collaborators
    {
        long boardID;
        HashSet<string> collaborators; //a hasSet of all collaborators' email on the board, for fast lookup
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly CollaboratorsController controller;
        private CollaboratorDAL dal;

        public Collaborators(long boardID, CollaboratorsController controller)
        {
            this.boardID = boardID;
            this.controller = controller;
            collaborators = new HashSet<string>();

            Log.Info($"Collaborators object created for board {boardID} with {collaborators.Count} collaborators loaded from DB.");
        }

        /// <summary>
        /// Adds a collaborator to the board.
        /// </summary>
        /// <param name="email"></param>
        /// <exception cref="Exception"></exception>
        public void AddCollaborator(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                Log.Error("Attempted to add an invalid (null or empty) collaborator email.");
                throw new Exception("Invalid collaborator email.");
            }

            CollaboratorDAL dal = new CollaboratorDAL(boardID, email.ToLower(), controller);

            if (dal.Save())
            {  //persisting to db
                collaborators.Add(dal.Email);  // Only update memory if DB succeeded
                Log.Info($"Collaborator '{dal.Email}' added to board {boardID}.");
            }
        }

        /// <summary>
        /// Adds a collaborator to the board without persisting it to the database, its use case is for loading from DB.
        /// </summary>
        /// <param name="email"></param>
        /// <exception cref="Exception"></exception>
        public void AddColaboratorWithoutPersisting(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                Log.Error("Attempted to add an invalid (null or empty) collaborator email.");
                throw new Exception("Invalid collaborator email.");
            }

            collaborators.Add(email.ToLower()); // Add to in-memory set without persisting
            Log.Info($"Collaborator '{email}' added to board {boardID} without persisting.");
        }

        /// <summary>
        /// Removes a collaborator from the board.
        /// </summary>
        /// <param name="email"></param>
        /// <exception cref="Exception"></exception>
        public void RemoveCollaborator(string inputmail)
        {
            string email = inputmail.ToLower(); //makes sure case sensitive

            CollaboratorDAL dal = new CollaboratorDAL(boardID, email, controller);

            dal.Delete();
            collaborators.Remove(email);  // Remove in-memory only after DB succeeded
            Log.Info($"Collaborator '{email}' removed from board {boardID}.");
        }

        /// <summary>
        /// Checks if a user is a collaborator on the board.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool IsCollaborator(string email)
        {
            return collaborators.Contains(email.ToLower());
        }

        /// <summary>
        /// Returns the number of collaborators on the board.
        /// </summary>
        /// <returns></returns>
        public int NumOfCollaborators()
        {
            return collaborators.Count;
        }

        /// <summary>
        /// Returns a copy of the collaborator list (optional utility).
        /// </summary>
        /// <returns></returns>
        public List<string> GetCollaborators()
        {
            return new List<string>(collaborators);
        }

        /// <summary>
        /// Deletes all collaborators for a specific board from the database.
        /// </summary>
        /// <param name="boardID"></param>
        /// <returns></returns>
        public bool DeleteCollaboratorsForBoard(long boardID)
        {
            return controller.DeleteAllCollaboratorsForBoard(boardID);
        }
    }
}
