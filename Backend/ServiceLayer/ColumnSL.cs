using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.BuisnessLayer;
using Backend.ServiceLayer;
using IntroSE.Kanban.Backend.BuisnessLayer;

namespace IntroSE.Kanban.Backend.ServiceLayer
{
    public class ColumnSL
    {
        public string Type { get; set; }
        public Dictionary<long, TaskSL> Tasks { get; set; }
        public int ColumnLimit { get; set; } // -1 means no limit

        public ColumnSL(string type, int columnLimit, Dictionary<long, TaskSL> tasks)
        {
            Type = type;
            ColumnLimit = columnLimit;
            Tasks = tasks;
        }
        
    }
}

