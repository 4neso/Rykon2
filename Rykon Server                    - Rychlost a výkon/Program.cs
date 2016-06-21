using RykonServer;
using RykonServer.Forms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Security.Principal;

namespace RykonServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static string GithUbURl = "https://github.com/4neso/Rykon";
        /// 
        [STAThread]
        static void Main()
        {
            BuildCount();
             _AssemblyName = "Rykon Server  v"+_Version+"      ";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Exec();
            }
            catch (Exception vb)
            {
                MessageBox.Show("Exception happened Please report to gersy.ch2@gmail.com \r \n Exception Text = " + vb.Message);
            }
        }

        private static void BuildCount()
        {
            string ex = "Build=";
            int i = 100;
            string p = Application.StartupPath + "\\build.ini";
            p = p.Replace("\\\\", "\\");
            string ud = AppHelper.ReturnAllTime();
            ud = "Last Edit at " + ud;  
            try
            {
                string d = AppHelper.ReadFileLines(p)[0];
                string[] x = d.Split('=');
                string t = x[1];
                i = int.Parse(t.Trim()); i++;
            }
            catch { }
            ex += i.ToString();
           ex= ex+"\r\n"+ud+"\r\nversion:"+ Program._Version;
            AppHelper.writeToFile(p, ex);
        }
        public static void Exec()
        {
            FormMain main = new FormMain();             
            bool isAcceptedLicense = true;            
            if (!SettingsEditor.GetDonotViewIntro())
            {
              if (new Forms.FrmIntro().ShowDialog() != DialogResult.OK)
                    isAcceptedLicense = false;
            }
            if (isAcceptedLicense)
            {
               if(!Environment.UserName.Contains("Gersy"))
                   MessageBox.Show("this is beta version , project still under Development , \r\n we would like to get text from you \r\n >> gersy.ch2@gmail.com","must said");
                Application.Run(main);                 
            }
        }
        public static string _Version = "2.0";
        public static string _AssemblyName = "Rykon2.0";
        public static string _AppName = "Rykon";
        public static string _AppverName = "Rykon mini Server";


        internal static void lauchProcess(string p)
        {
            throw new NotImplementedException();
        }
    }
}
