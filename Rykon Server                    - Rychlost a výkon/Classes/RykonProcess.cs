using RykonServer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RykonServer
{
    public class RykonProcess
    {
        public bool Requesting_Binary_data = false;
        public byte[] OutPutData = new byte[] { };
        public string Output_document = "";
        public int Output_code = 200;

        public string Client_UserAgent = "";
        public string Client_IpAddress = "";
        public bool Client_error = false;


        public bool Success = false;
        public bool InternalError = false;
        public ExceptionType exception = ExceptionType.none_;
        public List<RykonProcessHeader> OutPut_Headers = new List<RykonProcessHeader>();


        public string LocalPath = "";//   { get; set; }
        public string RequestPage = "";// { get; set; }

        public string Request_extn = "";// { get; set; }

        public string Requesting_Host = "";//{ get; set; }

        public bool Canceled = false;//{ get; set; }

        public ProcessingResult Processing_Type = ProcessingResult.TextHtml;//{ get; set; }

        internal void AddRequestHeader(StringS hp)
        {
            if (hp == null)
                return;
            if (hp.id.Length < 0)
                return;

            // uncomment to prevent multi headers
            //int i = 0;
            //foreach (StringS h in this.RequestHeaders)
            //{
            //    if (h.id == hp.id)
            //    {
            //        this.RequestHeaders[i].value=(hp.value);
            //        return;
            //    }
            //    i++;

            //}
            this.Request_Headers.Add(hp);

        }
        //internal void AddRequestHeader_httpmultirequestheadervals(HttpHeader hp)
        //{
        //    int i = 0;
        //    foreach (HttpHeader h in this.RequestHeaders)
        //    {
        //        if (h.name == hp.name)
        //        {
        //            this.RequestHeaders[i].UpdateVals(hp);
        //            return;
        //        } i++;

        //    }
        //    this.RequestHeaders.Add(hp);

        //}

        public List<StringS> Request_Headers = new List<StringS>();
        public List<StringS> Response_Headers = new List<StringS>();


        internal void SaveRequestHeaders(System.Collections.Specialized.NameValueCollection nameValueCollection)
        {
            // user-agent:fox
            // cookie;id=5
            //cooki=user=ger;date=15
            StringS h = new StringS();
            foreach (string key in nameValueCollection.AllKeys)
            {
                h.id = key.Trim();
                string[] values = nameValueCollection.GetValues(key);
                if (values.Length < 1)
                    continue;
                
                    if (h.id.ToLower() == "cookie")
                    {
                        this.ParseCookie(values[0]);
                        continue;
                    }
                    this.AddRequestHeader(h);
            }
        }

        private void ParseCookie(string p)
        {
            //    locale=en_US; wd=800x319; act=2463889535920%2F0
            bool hassep = p.Contains(';');
            string[] coks = new string[] { };
            if (hassep)
            { coks = p.Split(';'); }
            else
                coks = new string[] { p };
            foreach (string po in coks)
            {
                if (po.Length < 1)
                    continue;
                bool haseq = po.Contains('=');
                if (haseq)
                {
                    string[] cav = po.Split('=');
                    this.SetCookieSinglepar(cav[0], cav[1]);
                }
                else
                    this.SetCookieSinglepar(po, "");

            }


        }

        private void SetCookieSinglepar(string id, string val)
        {
            int o = 0;
            foreach (var r in this.COOKie)
                if (r.id == id)
                {
                    COOKie[o].value = val; return;
                }

            COOKie.Add(new StringS(id, val));

        }




        //internal bool IsHeaderHas(string hname, string val1,string val2)
        //{
        //    List<bool> lst = new List<bool>();
        //    foreach (var  h in this.RequestHeaders)
        //    {
        //        if (h.ISCookieHeader())
        //        {
        //            return h.Vals.Contains(val1) && h.Vals.Contains(val2);
        //        }
        //    }
        //    return false;
        //}

        public string UrlOriginalString = "";
        internal  bool IsREquestingTool(string p)
        {

            if (p.StartsWith("/Stream/") || p == "/Stream")
            {
                this.lastdirName = "Stream";
                return true;
            } if (p.StartsWith("/Video/") || p == "/Video")
            {
                this.lastdirName = "video";

                return true;
            } if (p.StartsWith("/Listen/") || p == "/Listen")
            {
                this.lastdirName = "Listen";

                return true;
            } if (p.StartsWith("/Control/") || p == "/Control")
            {
                this.lastdirName = "Control";

                return true;
            }
            return false;
        }

        public bool RequestBuiltInTool = false;
        public bool CanConnect = true;
        public string RequestorAddress = "";

        internal void SetData_ReadBinFile(string p)
        {
            try
            {
                this.OutPutData = AppHelper.ReadFileBts(p);

            }
            catch { }
        }

        internal void SetData_ReadTextFile(string p)
        {
            try
            {
                this.Output_document = AppHelper.ReadFileText(p);
                this.OutPutData = ASCIIEncoding.UTF8.GetBytes(this.Output_document);

            }
            catch { }
        }

        public string ContentType = "text/html";//{ get; set; }

        public string ErrorMessage { get; set; }

        internal int getLenght()
        {
            int i = this.Output_document.Length;
            if (this.Processing_Type == ProcessingResult.Binary)
                i = this.OutPutData.Length;
            return i;
        }

        public Uri Url;//= new Uri(""); 
        public string RequestPostData = "";
        public bool ServerErroroccured = false;
        public string Requestor_Host = "";

        public List<StringS> POST = new List<StringS>();
        public List<StringS> COOKie = new List<StringS>();


        public RykonProcess(Uri uri)
        {
            this.Url = uri;
        }

        internal void ParsePostData(string p)
        {
            // __a=1&__be=0&__dyn=7xeU6CQ3S3mbx67e-C1swgE98nwgU6C7UW3e3eaxe1qwh8eU88lwIwHwaa6Egx6&__pc=PHASED%3ADEFAULT&__req=2&__rev=234933
            bool sep = p.Contains('&');

            string[] pcs = new string[] { };
            if (sep)// __a=1&__be=0
            {
                pcs = p.Split('&');
            }
            else
            {
                pcs = new string[] { p };
            }

            foreach (string sp in pcs)
            {
                if (p.Length < 1)
                    continue;
                bool eq = sp.Contains('=');
                if (eq)
                {
                    //__a=1
                    string[] iv = sp.Split('=');
                    string id = WebServer.DecodeUrlChars(iv[0]);
                    string val = WebServer.DecodeUrlChars(iv[1]);
                    this.SetPostSinglepar(id, val);
                }
                else
                    this.SetPostSinglepar(sp, "");
            }

        }

        private void SetPostSinglepar(string id, string val)
        {
            int o = 0;
            foreach (var r in this.POST)
                if (r.id == id)
                {
                    POST[o].value = val; return;
                }
            POST.Add(new StringS(id, val));


        }

        public bool AllowedTocontrol = true;

        public string ResponseServerHeader = "Rykon";//{ get; set; }

        internal bool HasrequestHeader(string p1, string p2)
        {
            //xheader=xval
            foreach (StringS st in this.Request_Headers)
            {
                if (!st.matchId(p1))
                    continue;
                if (st.value.Contains(p2))
                    return true;

            }
            return false;

        }

        internal void SetResponseHeader(string p1, string p2)
        {
            if (!string.IsNullOrEmpty(p1))
                this.Response_Headers.Add(new StringS(p1, p2));
        }

        internal bool Reqcuest_cookie_equal(string p1, string p2)
        {
            foreach (StringS s in this.COOKie)
            {
                if (s.id != p1)
                    continue;
                else
                {
                    if (s.value == p2)
                        return true;
                }
            }

            return false;
        }

        internal bool POSTParEqual(string p1, string p2)
        {
            foreach (StringS s in this.POST)
            {
                if (s.id == p1)
                    return s.value == p2;
                }
            return false;
        }

        internal void RedirectTo(string p)
        {
            this.Output_code = 302;
            this.SetResponseHeader("Location", p);
        }

        public bool LoadMaster =false;

        public string  lastfilename ="";

        internal void SETLocalPath(string p)
        {
            this.LocalPath = p;
            if (p == "/")
            {
                lastfilename = lastdirName = "Main";
                
            }
            else
            {
                this.lastfilename = AppHelper.LastPeice(p, "/");
                this.lastdirName = AppHelper.LastPeice(p, "/", 2);

                if (this.lastfilename == null)
                    this.lastfilename = "";
                if (this.lastdirName == null)
                    this.lastdirName = "";
                this.NextTitle = this.lastdirName;
            }
        }

        public string lastdirName ="";

        public string Method { get; set; }

        internal string  MainUrl()
        {
            return "http://" + this.Url.Authority + "/";
        }

        internal void Die(int p1, string p2)
        {
            this.Output_code = p1;
            this.Output_document = p2;
            this.Requesting_Binary_data = false;
            this.LoadMaster = true;

        }
        public bool Dead = false;
        public string NextTitle =""; 
        public bool validCSRF =false; 
        public void SetResponseCooke(string p1, string p2)
        {
            this.SetResponseHeader("Set-Cookie", p1 + "=" + p2);
        }
    }
        public enum ProcessingResult { NotFound, TextHtml, Binary, ServerError, unAuthorized, AuthRequired }
    
}
