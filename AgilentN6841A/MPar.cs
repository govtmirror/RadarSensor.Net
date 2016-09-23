using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgilentN6841A
{
    public class MPar
    {
        public int RBW { get; set; }
        public long fStart { get; set; }
        public long fStop { get; set; }
        public int n { get; set; }
        public int td { get; set; }
        public string Det { get; set; }
        public string win { get; set; }
        public int ENBW { get; set; }
        public int Atten { get; set; }
        public int VBW { get; set; }
    }
}
