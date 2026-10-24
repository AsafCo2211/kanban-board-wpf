using Backend.ServiceLayer;
using Frontend.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Frontend.ViewModel
{
    class BoardViewModel : Notifiable
    {
        public UserModel CurrentUser { get; }

        public string Email => CurrentUser.Email; // to showcase the email as the header in the view

        private ObservableCollection<BoardModel> boards;
        public ObservableCollection<BoardModel> Boards
        {
            get => boards;
            set
            {
                boards = value;
                RaisePropertyChanged(nameof(Boards));
            }
        }

        public BoardViewModel(UserModel user)
        {
            CurrentUser = user;
            Boards = new ObservableCollection<BoardModel>();
            LoadBoards();
        }

        private void LoadBoards()
        {
            var boardListJson = CurrentUser.Controller.BS.GetUserBoards(CurrentUser.Email);
            
            var boardList = JsonSerializer.Deserialize<Response<long[]>>(boardListJson);
            if (boardList.ErrorMessage == null)
            {
                foreach (var boardID in boardList.ReturnValue)
                {
                    string boardName = CurrentUser.Controller.BS.GetBoardName((int)boardID);
                    boardName = JsonSerializer.Deserialize <Response<string>>(boardName).ReturnValue;
                    var boardJson = CurrentUser.Controller.BS.GetBoard(boardName, CurrentUser.Email);
                    var boardSL = JsonSerializer.Deserialize<Response<BoardSL>>(boardJson).ReturnValue;
                    if (boardSL != null)
                    {
                        if (boardSL != null)
                        {
                            Boards.Add(new BoardModel(boardSL)); 
                        }
                    }
                }
            }
        }

        public void CreateBoard(string boardName)
        {
            if (Boards == null)
                Boards = new ObservableCollection<BoardModel>();  // triggers RaisePropertyChanged

            var errorCheck = CurrentUser.Controller.BS.CreateBoard(boardName, CurrentUser.Email);
            var response = JsonSerializer.Deserialize<Response<object>>(errorCheck);
            if (response.ErrorMessage != null)
            {
                throw new Exception(response.ErrorMessage);
            }

            var boardJson = CurrentUser.Controller.BS.GetBoard(boardName, CurrentUser.Email);
            var boardSL = JsonSerializer.Deserialize<Response<BoardSL>>(boardJson);
            if (boardSL != null)
            {
                Boards.Add(new BoardModel(boardSL.ReturnValue));
            }
        }

        public void DeleteBoard(BoardModel board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            CurrentUser.Controller.BS.DeleteBoard(board.BoardName, CurrentUser.Email);
            Boards.Remove(board);
        }
    }
}
