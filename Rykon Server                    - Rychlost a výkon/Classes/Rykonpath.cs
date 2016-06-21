using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RykonServer.Classes
{
    public class Rykonpath
    {
        public string path = "";
        public Rykonpathtype type = Rykonpathtype.unknown;
        public string webpath = "";
        public Rykonpath(string f,Rykonpathtype t,string rootdir )
        {
            this.path = f;
            this.type = t;
            this.webpath = this.path.Replace(rootdir,"");
        }
        public  bool isfound()
        {
            return (this.type == Rykonpathtype.directory)
                ? System.IO.Directory.Exists(path)
                :  System.IO.File.Exists(path);
        }

    }
    public enum Rykonpathtype { File, directory, unknown }
}
