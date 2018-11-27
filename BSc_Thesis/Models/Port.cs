using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSc_Thesis.Models
{
    class Port
    {
        public string Name { get; set; }
        public string Desc { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Desc);
        }
    }
}
