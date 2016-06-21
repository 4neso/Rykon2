using RykonServer.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RykonServer
{
    class WebServer
    {
        public static string CookieDeletedvalue="deleted; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
        internal static bool IsDirectoryFound(string RequestFile)
        {
            return
                 System.IO.Directory.Exists(RequestFile);
        }

        internal static RykonFile[] ListDir(string RequestFile, string rootDir, string HostName, string port, bool ReplaceHost = true)
        {

            List<RykonFile> lstr = new List<RykonFile>(); 
            string[] lstar = System.IO.Directory.GetFiles(RequestFile);
            string[] lstdar = System.IO.Directory.GetDirectories(RequestFile);
            
            bool rootDirRequested = (RequestFile == rootDir);
            string host_et_port = HostName + ":" + port + "/";
            rootDir = rootDir.Replace("\\", "/");

            ///// parent
            if (!rootDirRequested)
            {
                string fx = AppHelper.GoUpAsDirectory(RequestFile);
                string fx2 = fx.Replace("\\", "/");
                string fx3 = fx2.Replace(rootDir, "");
                string fx4 = WebServer.EncodeUrlChars(fx3);
                string fx5 = "http://" + host_et_port + fx4;
                string fx6 = fx5.Replace("%2F", "/");

                RykonFile r = new RykonFile();
                r.IsDir = false;
                r.Fullpath = fx;
                r.Webpath = fx6;
                r.Name = AppHelper.LastPeice(fx6, "/");
                lstr.Add(r);

            }
            else
                lstr.Add(null);
                

            foreach (string f in lstar) //files
            {
                string f2 = f.Replace("\\", "/");
                string f3 = f2.Replace(rootDir, "");
                string f4 = WebServer.EncodeUrlChars(f3);
                string f5 = "http://" + host_et_port + f4;
                string f6 = f5.Replace("%2F", "/");
                
                RykonFile r = new RykonFile();
                r.IsDir = false;
                r.Fullpath = f;
                r.Webpath = f6;
                r.Name = AppHelper.LastPeice(f6, "/");
                lstr.Add(r);

            }
            foreach (string f in lstdar) // dirs
            {
                string f2 = f.Replace("\\", "/");
                string f3 = f2.Replace(rootDir, "");
                string f4 = WebServer.EncodeUrlChars(f3);
                string f5 = "http://" + host_et_port + f4;
                string f6 = f5.Replace("%2F", "/");
              
                RykonFile r = new RykonFile();
                r.IsDir = true;
                r.Fullpath = f;
                r.Webpath = f6+"/";
                r.Name = AppHelper.LastPeice(f6, "/");
                lstr.Add(r);

            } 
            return lstr.ToArray();
        }
        public static List<string> BinExts = new List<string>() { "bmp", "dib", "jfif", "tiff", "tif", "ico", "png", "jpg", "jpeg", "mp3", "mp4", "flv","exe","com","apk" };
        public static List<string> MediaExts = new List<string>() { "mp3", "mp4", "ogg", "wav", "flv" };
        internal static bool IsBinFile(string RequestPage)
        {
            string ext = AppHelper.LastPeice(RequestPage, ".");
            if (WebServer.BinExts.Contains(ext))
                return true;
            return false;
        }

        internal static string GetInternalErrorException(ExceptionType _CurrentExceptionType_)
        {
            return "Not implemented sorry";
        }

        internal static string EncodeHtmlChars(string Dir)
        {

            return System.Net.WebUtility.HtmlEncode(Dir);
        }
        internal static string DecodeHtmlChars(string Dir)
        {

            return System.Net.WebUtility.HtmlDecode(Dir);
        }
        internal static string EncodeUrlChars(string Dir)
        {

            return System.Net.WebUtility.UrlEncode(Dir);
        }
        internal static string DecodeUrlChars(string Dir)
        {

            return System.Net.WebUtility.UrlDecode(Dir);
        }
        public static string Constatn_Parent_directory = "ParentDir";

        internal static bool isMediaFile(string f)
        {
            string ext = AppHelper.ReturnFileExt(f);
            return MediaExts.Contains(ext.ToLower());

        }

        internal static bool CheckBasicAuth(string vall, string id, string pass)
        {
            if(vall==null)
            return false;
            try
            {
                var auth1 = vall;// ctx.Request.Headers["Authorization"];
                auth1 = auth1.Remove(0, 6); // Remove "Basic " From The Header Value
                auth1 = Encoding.UTF8.GetString(Convert.FromBase64String(auth1));
                var auth2 = string.Format("{0}:{1}", id, pass);
                return (auth1 == auth2);
            }
            catch { }
            return false;


        }


        internal static string IsFoundStaticIndex(string RequestDir)
        {
            string x = RequestDir + "index.html";
            
            if (AppHelper.IsFileExist(x))
                return x;

            
            x = RequestDir + "index.xhtml";
            if (AppHelper.IsFileExist(x))
                return x;


            x = RequestDir + "index.htm";
            if (AppHelper.IsFileExist(x))
                return x;

            return "";
        }
        internal static void SetMaster(string mp)
        {
            if (!AppHelper.IsFileExist(mp))
                return;
            string content = AppHelper.ReadFileText(mp);
            if (content.Contains(WebServer.masterMiddleReplacor))
            {
                string[] x = content.Split(new string[] { WebServer.masterMiddleReplacor }, StringSplitOptions.RemoveEmptyEntries);
                WebServer.masterPagePre = x[0];
                WebServer.masterPageAfter = x[1];

            }
        }
   
        public static string NewLineReplacor = "$&_rykon_new_line_&$";
        public static string dirNameReplacor = "$&dirname&$";
        public static string pagenamereplacor = "$&pagename&$";
        public static string fileNameReplacor = "$&filename&$";
        public static string anchorsTagsReplacor = "$&anchorsTags&$";


        internal static List<Rykonpath> getDirectoryPaths(string dir,string rootdirectory,out int sepindex)
        {
            sepindex = 0;
            try
            {
                if (AppHelper.ExistedDir(dir) == false)
                {
                    dir = AppHelper.RepairPathSlashes(dir);
                    AppHelper.RepairPath(dir);

                }
                if (AppHelper.ExistedDir(dir) == false)
                    return null;
                List<Rykonpath> resul = new List<Rykonpath>();
                string[] files = AppHelper.GetFiles(dir);
                string[] dirs = AppHelper.GetDirectories(dir);
                if (dirs != null)
                    foreach (string d in dirs)
                        resul.Add(new Rykonpath(d, Rykonpathtype.directory, rootdirectory));

                if (files != null)
                    foreach (string f in files)
                        resul.Add(new Rykonpath(f, Rykonpathtype.File,rootdirectory));

                sepindex = dirs.Length;
                return resul;
            }
            catch
            {
                return null;
            }
        }

        public static string appvernameReplacor = "$&appvername&$ ";
        public static string Control_auth_token_name = "Cnt_Auth_Tok";
        public static string stream_Auth_Tokenname ="Strm_Auth_Tok";
        public static string masterMiddleReplacor = "$&mastermiddle&$";
        public static string masterPageAfter ="";
        public static string masterPagePre = "";

        internal static string masterPagePre_(string apname, string  pagname2)
        {
            string res = WebServer.masterPagePre;
            res = res.Replace(WebServer.appvernameReplacor, apname); 
            res = res.Replace(WebServer.pagenamereplacor, pagname2); 
            return res;
        }

        public static String GetBoundary(String ctype)
        {
            return "--" + ctype.Split(';')[1].Split('=')[1];
        }
        public static void SaveFile(Encoding enc, String boundary, Stream input)
        {
            Byte[] boundaryBytes = enc.GetBytes(boundary);
            Int32 boundaryLen = boundaryBytes.Length;

            using (FileStream output = new FileStream("data", FileMode.Create, FileAccess.Write))
            {
                Byte[] buffer = new Byte[1024];
                Int32 len = input.Read(buffer, 0, 1024);
                Int32 startPos = -1;

                // Find start boundary
                while (true)
                {
                    if (len == 0)
                    {
                        throw new Exception("Start Boundaray Not Found");
                    }

                    startPos = IndexOf(buffer, len, boundaryBytes);
                    if (startPos >= 0)
                    {
                        break;
                    }
                    else
                    {
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
                    }
                }

                // Skip four lines (Boundary, Content-Disposition, Content-Type, and a blank)
                for (Int32 i = 0; i < 4; i++)
                {
                    while (true)
                    {
                        if (len == 0)
                        {
                            throw new Exception("Preamble not Found.");
                        }

                        startPos = Array.IndexOf(buffer, enc.GetBytes("\n")[0], startPos);
                        if (startPos >= 0)
                        {
                            startPos++;
                            break;
                        }
                        else
                        {
                            len = input.Read(buffer, 0, 1024);
                        }
                    }
                }

                Array.Copy(buffer, startPos, buffer, 0, len - startPos);
                len = len - startPos;

                while (true)
                {
                    Int32 endPos = IndexOf(buffer, len, boundaryBytes);
                    if (endPos >= 0)
                    {
                        if (endPos > 0) output.Write(buffer, 0, endPos - 2);
                        break;
                    }
                    else if (len <= boundaryLen)
                    {
                        throw new Exception("End Boundaray Not Found");
                    }
                    else
                    {
                        output.Write(buffer, 0, len - boundaryLen);
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen;
                    }
                }
            }
        }
        public static Int32 IndexOf(Byte[] buffer, Int32 len, Byte[] boundaryBytes)
        {
            for (Int32 i = 0; i <= len - boundaryBytes.Length; i++)
            {
                Boolean match = true;
                for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
                {
                    match = buffer[i + j] == boundaryBytes[j];
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

    }
    public enum ExceptionType
    { OutOfMemory_, Overflow_, HttpListner_, none_, Empty_dir_And_no_Default_index, CanceledByRequestor, FailedToHandle }
 
}
