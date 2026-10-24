using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.ServiceLayer;
using IntroSE.Kanban.Backend.ServiceLayer;

namespace Frontend.Model
{
    internal class ColumnsModel : Notifiable
    {
        public string Type { get; set; }
        public ObservableCollection<TaskModel> Tasks { get; set; }

        internal ColumnsModel(ColumnSL columnSL)
        {
            Type = columnSL.Type;
            Tasks = new ObservableCollection<TaskModel>(
                columnSL.Tasks.Select(t => new TaskModel(t.Value))
            );
        }
    }
}
