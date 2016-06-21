using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace RykonServer
{
   public class BuiltInApps
    {
      public static void executeController(ref ServerConfig Servconf, ref  RykonProcess cp,  ref bool valid_CSRF_tok, ref bool isValidsession, IntPtr handle , FormMain frm )
        {
            if (cp.LocalPath.EndsWith("/Control/thumb.png"))
            {
                cp.Requesting_Binary_data = true;
                cp.OutPutData = AppHelper.ReadFileBts(cp.RequestPage);
                return;

            }
            else 
            if (!Servconf.EnableControler) // disabled
            {
                cp.Output_document = WebDesigner.BuiltInDisabled("controller");
                cp.LoadMaster = true;
            }
            else // working
            {
                cp.LoadMaster = true;
                valid_CSRF_tok = cp.UrlOriginalString.Contains(Servconf.CSRF) || cp.POSTParEqual("CSRF", Servconf.CSRF);
                isValidsession = cp.Reqcuest_cookie_equal(WebServer.Control_auth_token_name, Servconf.controlsession);  /* ||cp.UrlOriginalString.Contains(Servconf.ControlPassword) */

                if (Servconf.SecureControl)
                    cp.AllowedTocontrol = isValidsession;
                else
                    cp.AllowedTocontrol = true;

                string[] pcs = new string[] { };

                if (cp.LocalPath.EndsWith("Control/logout"))
                {
                  
                    cp.SetResponseCooke(WebServer.Control_auth_token_name , WebServer.CookieDeletedvalue);
                    cp.Output_document = "Logged out";
                    return;
                }
                else if (!cp.LocalPath.StartsWith("/Control/exec"))
                {
                    cp.RedirectTo("http://" + cp.Url.Authority + "/Control/exec.rk");
                    return;
                }
                else if (!cp.AllowedTocontrol) // login page
                {
                    bool validformcsrf = cp.POSTParEqual("CSRF", Servconf.CSRF);
                    bool validformpassword = cp.POSTParEqual("pass", Servconf.ControlPassword);

                    if (validformcsrf && validformpassword)
                    {
                        cp.RedirectTo(cp.Url.ToString());
                    }
                    else
                    {      //ControlLoginPage;
                            cp.Output_document =
                            (!valid_CSRF_tok && isValidsession && cp.UrlOriginalString.Contains("CSRF"))
                            ? WebDesigner.invalidAuthTok(cp.Requesting_Host, Servconf)
                            : WebDesigner.ControlNotAllowedIndex(Servconf.CSRF);


                        cp.OutPutData = Encoding.UTF8.GetBytes(cp.Output_document);
                        cp.Output_code = 405;
                        cp.Processing_Type = ProcessingResult.unAuthorized;
                        return;
                    }
                }
                else if (cp.UrlOriginalString.Contains("exec") && cp.UrlOriginalString.Contains("com=") && valid_CSRF_tok)//&& !cp.UrlOriginalString.EndsWith(this.AuthToke))
                {
                    // sending commands 
                    //"http://192.168.1.100:9090/Control/exec?jex&com=msgbx&title=hello+It"
                    if (cp.UrlOriginalString.Contains("?"))
                        pcs = cp.UrlOriginalString.Split('?');

                    else if (cp.UrlOriginalString.Contains("/"))
                        pcs = cp.UrlOriginalString.Split('/');


                }
                if (pcs.Length > 0)  // receive comands
                {
                    // "http://192.168.1.100:9090/Control/exec   jex&com=msgbx&title=hello+It"
                    string main = pcs[pcs.Length - 1];

                    if (main.StartsWith(Servconf.CSRF))
                        main = main.Substring(Servconf.CSRF.Length);

                    RemoteCommandExecuter r = new RemoteCommandExecuter(main);
                    r.HandlePointer = handle;
                    r.proceeed();

                    if (r.RequireUnpreved)
                    {
                        if (frm != null)
                        {
                            if (r.hideOrShowclient())
                            {
                                frm.Visible = r.formvisible;
                                frm.notifyIcon1.Visible = r.ComType == RemoteCommandType.ShowClient;

                                r.Result = "Form = " + (frm.Visible ? "visible" : "hidden");
                                r.Result += WebServer.NewLineReplacor;
                                r.Result += "icon = " + (frm.notifyIcon1.Visible ? "visible" : "hidden");

                            }
                        }
                    }

                    if (r.HasBinaryResult)
                    {
                        cp.OutPutData = r.bytes;
                        cp.Processing_Type = ProcessingResult.Binary;
                        cp.Requesting_Binary_data = true;
                        cp.Request_extn = r.extn;
                    }
                    else
                        cp.Output_document = (r.Result);
                }

                else if (cp.AllowedTocontrol)// List Command index
                {
                
                    cp.Output_document = AppHelper.ReadFileText(Servconf.RootDirectory + "/Control/index.html");
                    cp.OutPutData =      Encoding.UTF8.GetBytes(cp.Output_document);
                }

                if (Servconf.SecureControl)
                    cp.SetResponseHeader("Set-Cookie", WebServer.Control_auth_token_name + "=" + Servconf.controlsession);

            }
        }
     public static void executeUploader(System.Net.HttpListenerContext ctx, RykonProcess cp, ServerConfig Servconf)
      {

          if (cp.Method == "POST")
          {
              HttpNameValueCollection o = new HttpNameValueCollection(ref ctx);
              //WebServer.SaveFile(ctx.Request.ContentEncoding, WebServer.GetBoundary(ctx.Request.ContentType), ctx.Request.InputStream);
          }
          else
          {
              if (cp.LocalPath.EndsWith("/Upload/thumb.png"))
              {
                  cp.OutPutData = AppHelper.ReadFileBts(Servconf.RootDirectory + "\\" + cp.LocalPath);
                  cp.Requesting_Binary_data = true;
                  return;
              }
              cp.Output_document = WebDesigner.getUpload_PostPage(cp.MainUrl(), Servconf.UploadPassword, Servconf.CSRF);
              cp.OutPutData = Encoding.UTF8.GetBytes(cp.Output_document);

          }
      }
    }
   public class HttpNameValueCollection
    {
        public class File
        {
            private string _fileName;
            public string FileName { get { return _fileName ?? (_fileName = ""); } set { _fileName = value; } }

            private string _fileData;
            public string FileData { get { return _fileData ?? (_fileName = ""); } set { _fileData = value; } }

            private string _contentType;
            public string ContentType { get { return _contentType ?? (_contentType = ""); } set { _contentType = value; } }
        }

        private NameValueCollection _get;
        private Dictionary<string, File> _files;
        private readonly HttpListenerContext _ctx;

        public NameValueCollection Get { get { return _get ?? (_get = new NameValueCollection()); } set { _get = value; } }
        public NameValueCollection Post { get { return _ctx.Request.QueryString; } }
        public Dictionary<string, File> Files { get { return _files ?? (_files = new Dictionary<string, File>()); } set { _files = value; } }

        private void PopulatePostMultiPart(string post_string)
        {
            var boundary_index = _ctx.Request.ContentType.IndexOf("boundary=") + 9;
            var boundary = _ctx.Request.ContentType.Substring(boundary_index, _ctx.Request.ContentType.Length - boundary_index);

            var upper_bound = post_string.Length - 4;

            if (post_string.Substring(2, boundary.Length) != boundary)
                throw (new InvalidDataException());

            var raw_post_strings = new List<string>();
            var current_string = new StringBuilder();

            for (var x = 4 + boundary.Length; x < upper_bound; ++x)
            {
                if (post_string.Substring(x, boundary.Length) == boundary)
                {
                    x += boundary.Length + 1;
                    raw_post_strings.Add(current_string.ToString().Remove(current_string.Length - 3, 3));
                    current_string.Clear();
                    continue;
                }

                current_string.Append(post_string[x]);

                var post_variable_string = current_string.ToString();

                var end_of_header = post_variable_string.IndexOf("\r\n\r\n");

                if (end_of_header == -1) throw (new InvalidDataException());

                var filename_index = post_variable_string.IndexOf("filename=\"", 0, end_of_header);
                var filename_starts = filename_index + 10;
                var content_type_starts = post_variable_string.IndexOf("Content-Type: ", 0, end_of_header) + 14;
                var name_starts = post_variable_string.IndexOf("name=\"") + 6;
                var data_starts = end_of_header + 4;

                if (filename_index == -1) continue;

                var filename = post_variable_string.Substring(filename_starts, post_variable_string.IndexOf("\"", filename_starts) - filename_starts);
                var content_type = post_variable_string.Substring(content_type_starts, post_variable_string.IndexOf("\r\n", content_type_starts) - content_type_starts);
                var file_data = post_variable_string.Substring(data_starts, post_variable_string.Length - data_starts);
                var name = post_variable_string.Substring(name_starts, post_variable_string.IndexOf("\"", name_starts) - name_starts);
                Files.Add(name, new File() { FileName = filename, ContentType = content_type, FileData = file_data });
                continue;

            }
        }

        private void PopulatePost()
        {
            //if (_ctx.Request.HttpMethod != "POST" || _ctx.Request.ContentType == null) return;

            //var post_string = new StreamReader(_ctx.Request.InputStream, _ctx.Request.ContentEncoding).ReadToEnd();

            //if (_ctx.Request.ContentType.StartsWith("multipart/form-data"))
            //    PopulatePostMultiPart(post_string);
            //else
            //    Get = /*.ParseQueryString*/(post_string);

        }

        public HttpNameValueCollection(ref HttpListenerContext ctx)
        {
            _ctx = ctx;
            PopulatePost();
        }

   }
    
}
