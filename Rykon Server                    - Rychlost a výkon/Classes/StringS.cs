using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RykonServer.Classes
{
    public class StringS
    {
        public string id = "";
        public string value= "";
        public StringS()
        {

        }
        public StringS(string i,string v)
        {
            id = i.Trim();
            value = v.Trim();
        }

        internal bool matchId(string p1)
        {
            return this.id.ToLower() == p1.ToLower(); 

        }
    }
}
