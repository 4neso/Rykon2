using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RykonServer.Classes;

namespace RykonServer
{
    class WebDesigner
    {
        internal static string ReadFile(string RequestFile)
        {
            try
            {
                return System.IO.File.ReadAllText(RequestFile);
            }
            catch { return "Error reading file"; }
        }
        public static string FileNotFoundTitle()
        {
            string d = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
            return "<h2 style=\"color:blue;\"><a href='" + Program.GithUbURl + "' >Rykon Server V1</a></h2><h4 style=\"color:red;\">404 File Not Found !!! at " + d + "</h4>";
        }
        internal static string StatueCode(int code)
        {

            if (code.BiggerThan_orEQ(200) && code.SmallerThan_orEQ(299))
                return code + " OK";

            if (code.BiggerThan_orEQ(300) && code.SmallerThan_orEQ(399))
                return code + " Redirection";

            if (code.BiggerThan_orEQ(400) && code.SmallerThan_orEQ(499))
                return code + " Request Error";
            if (code.BiggerThan_orEQ(500) && code.SmallerThan_orEQ(599))
                return code + " Server Error";



            return code.ToString();
        }

        internal static string FileNotFoundTitle_Traditional(string p1, string p2)
        {
            return string.Format("<center><h1>404</h1><h2>Not Found</h2><h4>The Requested URL was not found on this Server </h4>");//<hr /><a href='{2}'>Rykon</a> 2.0 (Windows) Server at Host {0} Port {1}  </center> </br>{3}", p1, p2, Program.GithUbURl, WebDesigner.TradeMark(" "));

        }


        public static string metaUtf = "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" /> ";
        internal static string ListDirectory(string requestdir, Classes.RykonFile[] f, ServerConfig Config)
        {

            string indexofdire = requestdir;
            if (Config.ShowFullPaths)
                indexofdire = indexofdire.Remove(0, Config.RootDirectory.Length);
            string doc = "";
            //"<h2>\r\n";
            //doc += " Index of \r\n";
            //doc += " </h2><h4>\r\n";
            //doc += WebServer.EncodeHtmlChars(requestdir) + "\r\n";
            //doc += "</h4>\r\n";
            doc += "<hr/><table>\r\n";
            if (f == null || f.Length < 1)            // empty
                return "Empty Dir";

            string folderic = "";
            string fileicon = "";

            if (Config.ShowDirIcon)
            {
                folderic = WebDesigner.FileIcoTag;
                fileicon = WebDesigner.FileIcoTag;

            }
            int i = -1;
            string endtable = "</table><hr />" + WebDesigner.PoweredBy(Config) + " ";

            foreach (var p in f)
            {
                i++;

                if (p == null)
                    continue;

                string tr = "<span style=\"text-decoration:underline\">";
                tr += "<tr>\r\n";
                tr += "<td>" + ((p.IsDir) ? folderic : fileicon) + "\r\n";
                tr += "<a style='text-decoration: underline;' draggable='true' href ='" + (p.Webpath) + "' >\r\n";
                tr += ((i == 0) ? "Parent Directory" : (i.ToString() + "&nbsp;&nbsp;&nbsp;" + p.Name));
                tr += "</a>\r\n";
                tr += "</td>\r\n";
                tr += "</tr></span>\r\n";


                doc += tr;

            }
            //} 
            doc += endtable;
            return doc;
        }


        private static string PoweredBy(ServerConfig Config)
        {
            return PoweredByConstant.Replace("YasserGersy", Config.PoweredByEnd);
        }

        public static string TradeMark(string p, string c = "center")
        {
            return "<div align='" + c + "'   >" + p + "&reg; 2016</div> ";
        }

        public static string _501InternalServerError(string host, string port, ServerConfig s)
        {
            return string.Format(
                "<center><h1>501</h1><h2>Server Error</h2><h4>Internal Server Error Occured while proccessing your request </br> on this Server </h4><hr /><a href='{2}'>Rykon</a> 2.0 (Windows) Server at Host {0} Port {1}  </center> </br>{3}", host, port, Program.GithUbURl, WebDesigner.TradeMark(s.PoweredByEnd));

        }

        public static string PoweredByConstant = "<div> Powered by <a href='" + Program.GithUbURl + "' >Rykon </a> Server v 2.0 4Neso &reg; 2016 ";
        /*
        private static string IndexOfEmptyDirectory(string RequestPage)
        {
         
            string doc = "<h2>\r\n";
            doc += " Index of \r\n";
            doc += " </h2><h4>\r\n";
            doc += WebServer.EncodeHtmlChars(RequestPage) + "\r\n";
            doc += "</h4>\r\n";
            doc += "<hr/><table>\r\n";

            string p = AppHelper.GoUpAsDirectory(RequestPage);

            string tr = "";
                tr += "<tr>\r\n";
                tr += "<td>\r\n";
                tr += "<a draggable='true' href ='" + (p) + "' >\r\n";
                tr += "Parent Directory	 "; 
                tr += "</a>\r\n";
                tr += "</td>\r\n";
                tr += "</tr>\r\n";
                doc += tr;
          

            doc += "</table><hr />" + WebDesigner.PoweredBy;
            return doc;
        }
        */
        public static string FolderIcoTag = "<img height='22' width='22' src='http://findicons.com/files/icons/766/base_software/128/folderopened_yellow.png' />";
        public static string FileIcoTag = "<img height='22' width='22' src='https://cdn2.iconfinder.com/data/icons/windows-8-metro-style/512/file.png' />";

        internal static string ListenDefaultIndex(string rootdir, string prefx, string port_)
        {
            string[] files;
            int count = 0;
            try
            {
                files = System.IO.Directory.GetFiles(rootdir + "Listen");
                string doc = string.Format("<body background=\"bg.jpg\"><a href 'http://{0}:{1}'> Refresh page </a><br />", prefx, port_);
                doc += "<h2>Enjoy Listening  !!  Rykon Listen </h2><center>\r\n";

                foreach (string f in files)
                {
                    if (!WebServer.isMediaFile(f))
                        continue;
                    count++;
                    string filmn = AppHelper.LastPeice(f, "\\");
                    doc += "<h2>" + filmn + "</h2><div class='header' ><br /><audio controls>\r\n";
                    doc += "  <source src=\"" + filmn + "\" type=\"audio/mpeg\">\r\n</audio>\r\n";
                    doc += "<hr /><br />\r\n\r\n</div><style>#header {    background-color:black;    color:white;    text-align:center;    padding:5px;}#nav {    line-height:30px;    background-color:#eeeeee;    height:300px;    width:100px;    float:left;    padding:5px;	      }#section {    width:350px;    float:left;    padding:10px;	  }#footer {    background-color:black;    color:white;    clear:both;    text-align:center;   padding:5px;	  }</style>";
                }
                if (count < 1)
                {
                    doc += WebDesigner.NoMediaFoundTelladmin;
                }
                return Htmlbeg + doc + "</center> </body><b><p style=\"color:red\">This page was created on " + AppHelper.ReturnAllTime() + "</p></b>" + WebDesigner.HtmlEnd;

            }
            catch { return "no media found "; }
        }
        internal static string VideoDefaultIndex(string rootdir, string prefx, string port_)
        {
            string[] files;
            int count = 0;
            try
            {
                files = System.IO.Directory.GetFiles(rootdir + "Video");
                string doc = string.Format("<body background=\"bg.jpg\"><a href 'http://{0}:{1}'> Refresh page </a><br />", prefx, port_);
                doc += "<h2>Enjoy watching  !!  Rykon Videos </h2><center>\r\n";


                foreach (string f in files)
                {
                    if (!WebServer.isMediaFile(f))
                        continue; count++;
                    string filmn = AppHelper.LastPeice(f, "\\");

                    doc += "<video width=\"220\" height=\"140\" controls>";
                    doc += "  <source src=\"" + filmn + "\" type=\"video/mp4\">";
                    doc += "</video>";

                }
                if (count < 1)
                {
                    doc += WebDesigner.NoMediaFoundTelladmin;
                }

                return Htmlbeg + doc + "</center> </body><b><p style=\"color:red\">This page was created on " + AppHelper.ReturnAllTime() + "</p></b>" + WebDesigner.HtmlEnd;

            }
            catch { return "no media found "; }
        }

        public static string HtmlEnd = "</html>";
        public static string Htmlbeg = "<html><head><meta charset=\"UTF-8\"></head>";

        public static string ControlLoginPage = "";

        internal static string ControlNotAllowedIndex(string CSRF)
        {
            return "<center><h2>not authorized</h2> <hr /><h3> Sorry You are not allowed </h3><form method='POST' action='exec.rk' > enter pass here <hr /> <input type='text' name='pass' /> <input type='hidden' name='CSRF' value='" + CSRF + "' /><input type='submit' /></form> </center>";
        }

        public static string StreamLoginPage(string CSRF)
        {
            return "<center><h2>not authorized</h2> <hr /><h3> Sorry You are not allowed </h3><form method='POST' action='/Stream/index.html' > enter pass here <hr /> <input type='text' name='pass' /> <input type='hidden' name='CSRF' value='" + CSRF + "' /><input type='submit' /></form> </center>";

        }

        internal static string ControlCommandListindex(string p1, string p2, string p3, string CSRF, string password)
        {
            string doc = "<a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=msgbx&mbtitle=hello+It&close=1' >";
            doc += "message box </a><hr />";

            doc += "<a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=procstr&procnm=notepad' > ";
            doc += "launch Process </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=closeprocess&procnm=chrome.exe' >";
            doc += "close process </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=procstr&procnm=explorer.exe&procpar=" + WebServer.EncodeUrlChars("@\"c:\"") + "' >";
            doc += "start explorer  </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=closeprocess&procnm=explorer' >";
            doc += "close explorer </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=procstr&procnm=bg.jpg&proctype=pic' >";
            doc += "Display Pic </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=closeallprocess&procnm=chrome.exe' >";
            doc += "close all Process </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=mutesys&proctype=sound' >";
            doc += " mute system sound </a>";


            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=mvcrs&crsx=35&shftdir=shift' >";
            doc += "move cursor right  </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=mvcrs&crsx=35&shftdir=shiftback' >";
            doc += "move cursor left  </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=mvcrs&crsy=35&shftdir=shiftback' >";
            doc += "move cursor up  </a>";


            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=ls&drpth=" + WebServer.EncodeUrlChars("/") + "' >";
            doc += "list  </a>";


            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=mvcrs&crsy=35&shftdir=shift' >";
            doc += "move cursor  down </a>";


            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=msclk' >";
            doc += "left mouse click  </a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=msrclk' >";
            doc += "right mouse click  </a>";


            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=prntscr' >";
            doc += "Print screen</a>";

            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=hidecl&frm=0' >";
            doc += "Hide Server</a>";
            doc += "<hr /><a href='exec?ps=$passwd$&CSRF=$CSRFtok$&com=showcl&frm=0' >";
            doc += "show Server</a>";
            doc = doc.Replace("$passwd$", password);
            doc = doc.Replace("$CSRFtok$", CSRF);
            return doc;
        }
        public static string IndexofNeedAuthentication = "Canceled by user , Authentication required";
        public static string NoMediaFoundTelladmin = " sorry no files found added to  this Extension , tell the adminstrator ";
        internal static string BuiltInDisabled(string p)
        {
            return string.Format("This application '" + p + "' was disabled by the adminstrator ");
        }

        public static string invalidAuthTok(string main,ServerConfig s )
        {
            if (s.InstalledHostFound(s,s.GET_InstalledHost_Dirname(main)))
                main = "http://"+main+":"+s.Port;
            else
                main = "http://"+s.GetHostAndPort();
            return "Your Broswer sent outdated parameters <a href='"+WebServer.EncodeHtmlChars(main)+"/' >Go To Main Page</a>";
        }

        internal static string DefaultIndex(string rootdir, string hotsandport_slashed)
        {
            string html =WebServer.masterPagePre; //AppHelper.ReadFileText(rootdir + "\\index.html");
          
            int sep =0;
            List<Rykonpath> paths = WebServer.getDirectoryPaths(rootdir,rootdir,out sep);
            string res = "";
            int i = -1;
            foreach (Rykonpath p in paths)
            {
                i++;

                if (p.path.EndsWith("\\index.html"))
                    continue;
                res +=  ((p.type == Rykonpathtype.directory)
                        ?WebDesigner.DirectoryTagAnchorWithImage(p.webpath)
                        : WebDesigner.FileTagAnchor(p.webpath)+"\r\n");
                if (i == sep)
                    res += "<br />";
            }

           // html= html.Replace(WebServer.anchorsTagsReplacor,res);
             html+= res;
            html= html.Replace(WebServer.pagenamereplacor, "Main Page");
           html = html.Replace(WebServer.appvernameReplacor, Program._AppverName);
          html+=WebServer.masterPageAfter;
            return html;
        }

        private static string FileTagAnchor(string p)
        {
            string filetag = "<li class=\"feat\">\r\n<a href=\"/$&filename&$\">\r\n<span>$&filename&$</span></a> \r\n</li>";
            return filetag.Replace(WebServer.fileNameReplacor, p);

        }

        private static string  DirectoryTagAnchorWithImage (string p)
        {
            string dirtagtag = "<li class=\"feat\">\r\n<a href=\"/$&dirname&$/\">\r\n<img src=\"../$&dirname&$/thumb.png\" alt=\"$&dirname&$\">\r\n</a><span>$&dirname&$\r\n</span> </li>";
            return dirtagtag.Replace(WebServer.dirNameReplacor, p);
        }


        internal static string getUpload_PostPage(string mainurl, string password, string csrf)
        {
            string doc = "<form  action=\"" + mainurl + "/Upload/up.rk\" enctype=\"multipart/form-data\" method=\"POST\"  >";
            doc += "<input type=\"file\" />";
            doc += "<input type=\"submit\" />";
            doc += "</form>";
            
            return doc;
        }

    }
}
