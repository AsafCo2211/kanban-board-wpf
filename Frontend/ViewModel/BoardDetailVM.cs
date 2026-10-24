using Frontend.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Frontend.ViewModel
{
    class BoardDetailVM :Notifiable
    {
        private BoardModel board;

        public BoardModel Board
        {
            get => board;
            set
            {
                board = value;
                RaisePropertyChanged(nameof(Board));
            }
        }

        public string BoardOwner => Board.BoardOwner;
        public ObservableCollection<string> Members => Board.Members;
        public ObservableCollection<ColumnsModel> Columns => Board.Columns;

        public BoardDetailVM(BoardModel board)
        {
            Board = board;
        }
    }
}
