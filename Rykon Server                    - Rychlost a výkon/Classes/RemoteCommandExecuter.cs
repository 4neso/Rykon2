using RykonServer.Classes;
using Rykon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RykonServer
{
    public enum RemoteCommandType { ReadFile, WriteFile, MessageBox, CreateFile, CreateDir, Sleep, CloseProcess, CloseAllProcess, unkown, process, MoveMouse, MouseClick, MuteSystemSound, MouseRightClick, PrintScreenShot, LS, HideClient, ShowClient }
    public class RemoteCommandExecuter
    {
        public string ProcessName = "";
        public string FileName = "";
        public string DirName = "";
        public string OutPut = "";
        public string ProcessPar = "";
        private string StringText;

        public RemoteCommandType ComType = RemoteCommandType.unkown;
        public RemoteCommandExecuter(string main)
        {
            initEnvironmentElements();
            string [] pcs = new string[]{};
            this.StringText = main;
           // jex&com=msgbx&title=hello+It"
            if (main.Contains("&"))
                pcs = main.Split('&');
            
            foreach (string s in pcs)
            {
                if (!s.Contains("="))
                    continue;
                string[] pc2 = s.Split('=');
                this.SetIdAndVal(pc2[0], pc2[1]);
            }                               
        }

        private void initEnvironmentElements()
        {
            this.mouseCursorX = Cursor.Position.X;
            this.mouseCursorY = Cursor.Position.Y;
        }

        private void SetIdAndVal(string id, string val)
        {
            string Decoded = WebServer.DecodeUrlChars(val);
            id = id.ToLower().Trim();
            int val_int = 10;
            val_int = AppHelper.StringToint(val, 10);
            switch (id)
            {
                case "com":
                    {
                        if(val=="msgbx")
                            this.ComType = RemoteCommandType.MessageBox;
                        else  if (val == "procstr" || val =="proc")
                                this.ComType = RemoteCommandType.process;
                        else if (val == "closproc" || val == "closeprocess")
                            this.ComType = RemoteCommandType.CloseProcess;
                        else if (val == "closallproc" || val == "closeallprocess")
                            this.ComType = RemoteCommandType.CloseAllProcess;
                        else if (val == "mvcrs" || val == "movecursor")
                            this.ComType = RemoteCommandType.MoveMouse;
                        else if (val == "msclk" || val == "mouseclick")
                            this.ComType = RemoteCommandType.MouseClick;
                        else if (val == "mute" || val == "mutesys")
                            this.ComType = RemoteCommandType.MuteSystemSound;
                        else if (val == "msrclk" || val == "mouseright")
                            this.ComType = RemoteCommandType.MouseRightClick;
                        else if (val == "prntscr" || val == "printscreen")
                            this.ComType = RemoteCommandType.PrintScreenShot;
                        else if (val == "ls" || val == "list")
                            this.ComType = RemoteCommandType.LS;
                        else if (val == "hdcl" || val == "hidecl")
                            this.ComType = RemoteCommandType.HideClient;

                        else if (val == "shcl" || val == "showcl")
                            this.ComType = RemoteCommandType.ShowClient;
                        
                        break;
                    }
                case "mbtitle": this.MessageBoxCap = Decoded; break;
                case "frm": this.formvisible = AppHelper.BoolTruefalsefromString(Decoded); break;
                case "dirpath":
                case "directorypath":
                case "drpth": this.DirPath = Decoded; break;
                case "crsx"     :    { this.X = val_int; ; break; }
                case "close": { this.closeMessage = AppHelper.BoolTruefalsefromString(Decoded) ; break; }
                case "crsy"    :    { this.Y = val_int; ; break; }
                case "procnm": this.ProcessName = val; break;
                case "proctype": this.ViewImageProcess =( val == "pic"); break;
                case "procpar": this.NoProcessPar = false; this.ProcessPar = Decoded; break;
                case "shftdir": this.CursorGoingBack = (val.Contains("back")); break;
            }
        }
        public string Result ="no operation made "; 
        internal void proceeed()
        {
            switch (ComType)
            {
                case RemoteCommandType.process:
                    {
                        if (this.ViewImageProcess)
                            ProcessName = Application.StartupPath + "\\RootDir\\"+ProcessName;
                        string r = "";
                        if (this.NoProcessPar)
                            r = AppHelper.StartProcess(this.ProcessName).ToString();
                        else
                            r = AppHelper.StartProcess(this.ProcessName, this.ProcessPar);
                            this.Result = this.ProcessName + WebServer.NewLineReplacor+"      proce = " + r;
                         break;
                    }
                case RemoteCommandType.MessageBox:
                    {
                        AppHelper.EnormusMessageBox(MessageBoxCap,this.closeMessage); 
                        this.Result = "msgbx sent";
                        break;
                    }
                case RemoteCommandType.MoveMouse:
                    {
                        this.initMouseCursor();
                        RemoteAdmin.MouseMove(this.mouseCursorX, this.mouseCursorY);
                        this.Result = "MovedTo {"+this.mouseCursorX+","+this.mouseCursorY+"}";
                        break;
                    }
                case RemoteCommandType.CloseProcess:
                    {
                        string r =   AppHelper.CloseProcess(ProcessName); 
                        this.Result = "closed=" + r;
                        break;
                    }
                case RemoteCommandType.CloseAllProcess:
                    {
                        this.Result = "closed=" + AppHelper.CloseProcessAll(ProcessName); break;
                    }
                case RemoteCommandType.MouseClick:
                    {
                        this.RequireUnpreved = true;
                        RemoteAdmin.PerformMouseLeftClick();
                        break;
                    }
                case RemoteCommandType.LS:
                    {                        
                        string [] x =  AppHelper.GetDirectoryContents(this.DirPath);
                        this.Result = AppHelper.ConcaTArrayToString(x, "<br />");
                      
                        break;
                    }
                case RemoteCommandType.MuteSystemSound:
                    {
                        this.Result = RemoteAdmin.MuteSystemSound(this.HandlePointer);
                        break;
                    }
                case RemoteCommandType.MouseRightClick:
                    {
                        this.Result = "clicking=" + RemoteAdmin.PerformMouseRightClick();
                        break;
                    }
                case RemoteCommandType.PrintScreenShot:
                    {
                        this.HasBinaryResult = true;
                        string fil = AppHelper.GetRandomFilePAth();
                        var bmp = ScreenCapturePInvoke.CaptureFullScreen(true);
                        bmp.Save(fil);
                        this.bytes = AppHelper.ReadFileBts(fil);
                        AppHelper.deleteFile(fil);
                        this.extn = "jpg";
                        this.Result = fil;
                        break;
                    }
                case RemoteCommandType.ShowClient:
                case RemoteCommandType.HideClient: this.RequireUnpreved=true; break;



            }
        }

        private void initMouseCursor()
        {

            if (CursorGoingBack)
            {
                this.mouseCursorX = this.mouseCursorX - X;
                this.mouseCursorY = this.mouseCursorY - Y;
            }
            else
            {
                this.mouseCursorX = this.mouseCursorX + X;
                this.mouseCursorY = this.mouseCursorY + Y;

            }
        }

        public string MessageBoxCap="";
        public bool ViewImageProcess { get; set; }
        public bool NoProcessPar=true; 
        public int mouseCursorX =5;  
        public int mouseCursorY =5; 
        public bool RequireUnpreved =false; 
        public IntPtr HandlePointer { get; set; } 
        public bool CursorGoingBack  =false; 
        public int X  =0; 
        public int Y =0;

        public bool HasBinaryResult =false; 

        public byte[] bytes = new byte [] {};

        public string extn ="";

        public string  DirPath ="";


        public bool closeMessage =true;

        internal bool hideOrShowclient()
        {
            return ComType == RemoteCommandType.ShowClient || ComType == RemoteCommandType.HideClient;
        }

        public bool formvisible =false; 
    }
}
