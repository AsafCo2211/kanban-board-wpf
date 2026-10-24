using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.BuisnessLayer;
using IntroSE.Kanban.Backend.BuisnessLayer;
using IntroSE.Kanban.Backend.ServiceLayer;

namespace Backend.ServiceLayer
{
    public class BoardSL
    {
        public string BoardName { get; set; }
        public string Owner { get; set;}
        public Dictionary<string, ColumnSL> ColumnsList { get; set; }
        public List<string> Collaborators { get; set; }

        public BoardSL() { } // for JSON use

        internal BoardSL(BoardBL board)
        {
            BoardName =  board.BoardName;
            Owner = board.BoardOwner;
            ColumnsList = ConvertBLtoSL(board.columns);
            Collaborators = board.Collaborators.GetCollaborators();
        }


        private static Dictionary<string, ColumnSL> ConvertBLtoSL(Dictionary<string, ColumnBL> blColumns)
        {
            var result = new Dictionary<string, ColumnSL>();

            foreach (var columnEntry in blColumns)
            {
                string columnName = columnEntry.Key;
                ColumnBL columnBL = columnEntry.Value;

                result[columnName] = new ColumnSL(columnBL.Type, columnBL.ColumnLimit, ConvertTasksFromBLtoSL(columnBL.GetAllTasks()));
            }
            return result;
        }
        private static Dictionary<long, TaskSL> ConvertTasksFromBLtoSL(Dictionary<long, TaskBL> blTasks)
        {
            Dictionary<long, TaskSL> slTasks = new Dictionary<long, TaskSL>();
            foreach (var entry in blTasks)
            {
                slTasks[entry.Key] = new TaskSL(entry.Value);
            }
            return slTasks;
        }
    }
}
