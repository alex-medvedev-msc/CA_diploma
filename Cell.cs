using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PractiseVisualizer
{
    public class Cell
    {
        public AutoType Type { get; set; }
        public int Speed { get; set; }
        public bool isFirst { get; set; }
        public int ManCount { get; set; }
        public int StationLimit { get; set; }
    }
    public enum AutoType
    {
        None,
        Car,
        Bus,
        Trouble
    }
}
