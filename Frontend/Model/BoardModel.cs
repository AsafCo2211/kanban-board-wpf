using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.ServiceLayer;

namespace Frontend.Model
{
    internal class BoardModel: Notifiable
    {
       
        public string BoardName { get; set; }
        public string BoardOwner { get; set; }
        public ObservableCollection<string> Members { get; set; }
        public ObservableCollection<ColumnsModel> Columns { get; set; }

        public BoardModel(BoardSL board)
        {
            BoardName = board.BoardName;
            BoardOwner = board.Owner;
            Members = new ObservableCollection<string>(board.Collaborators);
            Columns = new ObservableCollection<ColumnsModel>(
            board.ColumnsList.Select(c => new ColumnsModel(c.Value))
        );
        }
        }
    }
