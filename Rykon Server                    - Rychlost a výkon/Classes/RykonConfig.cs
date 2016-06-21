using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RykonServer.Classes
{
    public enum TestingType { Separeted, BuiltIn, InBrowser }
    public class RykonConfig
    {
        public bool RunAtStartUp = false;
        public string ConfigFilePAh = "";
        public RykonConfig(string file)
        {
            this.ConfigFilePAh = file;
            if (!AppHelper.IsFileExist(file))
                return;
           string[] data = new string[] { };
            try
            {
                data = System.IO.File.ReadAllLines(file);
            }
            catch { ValidConfigFile = false; return; }
            if (data.Length < 1)
            { ValidConfigFile = false; return; }

            Create(data);


        }

        public RykonConfig()
        {
            // TODO: Complete member initialization
        }
        private void Create(string[] data)
        {
            if (data.Length < 1)
                return;
            foreach (string s in data)
            {
                if (s.Contains('=') == false || s.StartsWith("#"))
                    continue;
                string[] pcs = s.Split('=');
                string id = pcs[0].Trim().ToLower();
                string Value = pcs[1];

                switch (id)
                {

                    case "runatstartup"       : this.Initiate(Value, ref this.RunAtStartUp); break;
                    case "testingtype": this.Initiate(Value, ref this.TestingMode); break;               
 
                }
            }
        }

        private void Initiate(string Value, ref TestingType testingType)
        {
            switch (Value.ToLower())
            {
                case "builtin" : testingType = TestingType.BuiltIn; break;
                case "separeted": testingType = TestingType.Separeted; break;
                default: testingType = TestingType.InBrowser; break;
            }
        }
        TestingType TestingMode = TestingType.InBrowser;
        private void Initiate(string Value, ref string p)
        {
            p = Value.Trim();
        }
        private void Initiate(string Value, ref int p)
        {
            try
            {
                p = int.Parse(Value.Trim());
            }
            catch { p = 9090; }
        }
        private void Initiate(string Value, ref bool p)
        {
            int i = AppHelper.BoolTruefalseUnkown(Value);
            if (i == 1)
                p = true;
            else if (i == 0)
                p = false;
        }
        public bool SaveChanges()
        {
            string data = "#Rykon app configuration file v"+Program._Version;
            data += "\r\nRunAtStartUp=" + Val(RunAtStartUp);
              try
            {
                AppHelper.RepairPath(ConfigFilePAh);
                System.IO.File.WriteAllText(this.ConfigFilePAh, data);
                this.saved = true;
            }
            catch { saved = false; }
            return saved;
        }
        private static string Val(bool a)
        {
            if (a)
                return "Yes";
            else
                return "No";
        }
        public  bool saved =false;
        public bool ValidConfigFile = false;

        public TestingType Testingmode = TestingType.Separeted;

    }
}
