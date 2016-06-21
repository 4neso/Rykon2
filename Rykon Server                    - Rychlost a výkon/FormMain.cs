using RykonServer.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using RykonServer.Forms;
using System.Threading;
using Rykon;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace RykonServer
{
    

 public  partial class   FormMain : Form
 {
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int WM_APPCOMMAND = 0x319;    

        private List<Tuple<string, string>> _Prefixs_;
        RykonConfig AppConfiguration = new RykonConfig();

        int _handled = 0; 
        private _Mode_ ServerMode = _Mode_.off;
        private HttpListener _MainServer_;
        private XCompiler _MainCompiler_ = new XCompiler();


        public int _ShotEvery = 500;
        private object _mrlocker_ = new object();
        private ReaderWriterLock _rwlck_ = new ReaderWriterLock();
        private MemoryStream _imgstr_ = new MemoryStream();


        private bool _Listening_;
        private bool _isPreview;
        private bool _isMouseCapture;
        public bool _isTakingScreenshots = true;
        public bool _StreamerEnabled = false;
        private bool separatedBrowser = false;

        public string _RootDirectory =""; 

        public string _LogsFilePath = "\\logs.txt";
        public string _ErrorFilePath = "\\Errors.txt";

        public ServerConfig Servconf;
        private FrmSelfBrowser TestingForm = new FrmSelfBrowser();

        public int _CanceledReqs = 0;
        public int _Port = 9090;

        ContextMenu TrayMenu;
        public int screened = 0;
      
        private async Task StartServer()
        {
            ServerMode = _Mode_.on;
            ViewLog("Staring server ...");
            SetStatue("Staring server ...");

            string TrimmedPrefex = this._Prefixs_[(cb_Prefixs.SelectedIndex)].Item2;
            string SelectedPrefix = txbx_serverUrl.Text = "http://" + TrimmedPrefex + ":" + NumPort.Value.ToString() + "/";
            string mp = Servconf.GetMainApp();
            
            this.textBoxUrlMainAPP.Text = SelectedPrefix+mp+"/";
            gpxmainapp.Text = mp;
            labelmainapp.Text = mp + " url";

            generatedefaultindex();
            GenerateListenPlayer();
            GenerateMediaPlayer();
            GenerateControlIndex();

            ChangeControlerS();
            _MainServer_ = new HttpListener();
            _MainServer_.Prefixes.Add(SelectedPrefix);
            _MainServer_.Prefixes.Add("http://*:" + NumPort.Value.ToString() + "/");
            _MainServer_.Start();

            Servconf.currentHost = TrimmedPrefex;
            string xt = "Running on " + this._Port;
            Ballooon(xt);
            ViewLog(xt);

            if (this._StreamerEnabled)
                ViewLog("Stream on " + textBoxUrlMainAPP.Text);
            if (this.Servconf.EnableControler)
                ViewLog("Control from " + SelectedPrefix + "Control/");
            if (this.Servconf.EnableVideo)
                ViewLog("Video from " + SelectedPrefix + "Video/");
            if (this.Servconf.EnableListen)
                ViewLog("Listen from " + SelectedPrefix + "Listen/");
             if (this.Servconf.EnableUpload)
                 ViewLog("Upload on " + SelectedPrefix + "Upload/");


            SetStatue(xt);
            notifyIcon1.Text = "Rykon Online ";

            while (_Listening_)
            {
                try
                {
                    if (_MainServer_.IsListening == false)
                        break;

                    var ctx = await _MainServer_.GetContextAsync();
                    string ad = ((!this._RootDirectory.EndsWith("\\") ? "\\" : ""));

                    RykonProcess cp = new RykonProcess(ctx.Request.Url);
                    cp.SaveRequestHeaders(ctx.Request.Headers);
                    
                    cp.UrlOriginalString = ctx.Request.Url.OriginalString;
                    cp.SETLocalPath ( ctx.Request.Url.LocalPath);
                    cp.RequestBuiltInTool = cp.IsREquestingTool(cp.LocalPath);
                    cp.RequestPage = (this._RootDirectory + /*ad+*/ cp.LocalPath.Replace("/", "\\")).Replace("\\\\", "\\");
                    cp.Request_extn = AppHelper.LastPeice(cp.RequestPage, ".");
                    cp.Request_extn = AppHelper.removeSlashes(cp.Request_extn);
                    cp.Requestor_Host = AppHelper.FirstPieceof(ctx.Request.RemoteEndPoint.Address.ToString(), ':');
                    cp.Requesting_Host = ctx.Request.Url.Host; 
                    cp.CanConnect = (this.Servconf.IsPublicServer);
                    cp.RequestorAddress = ctx.Request.UserHostAddress;
                    cp.Url = ctx.Request.Url;
                    if (cp.RequestPage.EndsWith("\\/"))
                        cp.RequestPage = cp.RequestPage.Substring(0, cp.RequestPage.Length - 1);
                  
                    cp.RequestPage = WebServer.DecodeUrlChars(cp.RequestPage);
                    bool validauthtok = false;
                    bool IsValidSession = false;
                    cp.LoadMaster = cp.RequestBuiltInTool;
                        cp.Method=ctx.Request.HttpMethod ;

                    // receiving data        
                  //  cp.SaveRequestHeaders(ctx.Request.Headers);
                    if (ctx.Request.HttpMethod == "POST")
                    {
                        if (ctx.Request.HasEntityBody)
                        {
                            using (System.IO.Stream body = ctx.Request.InputStream) // here we have data
                            {
                                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, ctx.Request.ContentEncoding))
                                {
                                    cp.ParsePostData( reader.ReadToEnd());
                                }
                            }
                        }
                    }
                    //foreach(var p in ctx.Request.Headers)
                        
                    try
                    {
                        cp.CanConnect = true;
                        if (!this.Servconf.IsPublicServer)
                           cp. CanConnect = WebServer.CheckBasicAuth(ctx.Request.Headers["Authorization"], Servconf.ServerAuthId, Servconf.ServerAutPass);

                        if (!cp.CanConnect) // ask credit 
                        {
                            cp.Output_document = WebDesigner.IndexofNeedAuthentication;
                            cp.Output_code = 401;
                            cp.OutPutData = ASCIIEncoding.UTF8.GetBytes(cp.Output_document);
                            ctx.Response.AddHeader("WWW-Authenticate", "Basic realm=Rykon Server : ");
                            cp.Processing_Type = ProcessingResult.AuthRequired; 
                            
                        }
                        else if (cp.LocalPath.StartsWith("/Control"))
                        {
                         
                            BuiltInApps. executeController(ref Servconf,ref cp,  ref validauthtok, ref IsValidSession, this.Handle, this);
                           
                        }
                        //else if (cp.LocalPath.StartsWith("/Upload/"))

                        //{
                        //    BuiltInApps.executeUploader(ctx,cp,Servconf);

                        //}

                        else if (cp.LocalPath.StartsWith("/Stream/index.html") || cp.LocalPath.StartsWith("/Stream/LiveStream.jpg"))

                        {
                            if (!Servconf.EnableStream )
                            {
                                cp.Output_document = WebDesigner.BuiltInDisabled("Stream");
                                cp.LoadMaster = true;

                            }
                            else
                            {
                                bool allowtostream = true;
                               
                                    if (Servconf.SecureStream)
                                    {
                                        allowtostream = cp.Reqcuest_cookie_equal(WebServer.stream_Auth_Tokenname, Servconf.streamSession);
                                    }

                                    if (cp.Method == "POST")
                                    {
                                        cp.validCSRF = cp.UrlOriginalString.Contains(Servconf.CSRF) || cp.POSTParEqual("CSRF", Servconf.CSRF);
                                        bool validformpassword = cp.POSTParEqual("pass", Servconf.StreamPassword);

                                        if (validformpassword)
                                        {
                                            cp.SetResponseCooke(WebServer.stream_Auth_Tokenname, Servconf.streamSession);
                                            cp.RedirectTo("index.html");
                                        }
                                        else
                                        {
                                            cp.Output_document = WebDesigner.StreamLoginPage(Servconf.CSRF);

                                            cp.LoadMaster = true;
                                        }
                                    }
                                    else if (!allowtostream && (cp.LocalPath.EndsWith("Stream/") || cp.LocalPath.EndsWith("LiveStream.jpg") || cp.LocalPath.EndsWith("index.html")))
                                    {
                                        cp.Output_document = WebDesigner.StreamLoginPage(Servconf.CSRF);

                                    }
                                    else
                                    {
                                        var page = _RootDirectory + cp.LocalPath;
                                       
                                        bool fileExist;
                                        lock (_mrlocker_)
                                            fileExist = File.Exists(page);


                                        if (fileExist)
                                        {
                                            if (!page.EndsWith("jpg"))
                                            {
                                                cp.Output_document = AppHelper.ReadFileText(page);;
                                            }
                                            else
                                            {
                                                cp.LoadMaster = false;
                                                _rwlck_.AcquireReaderLock(Timeout.Infinite);
                                                cp.OutPutData = File.ReadAllBytes(page);
                                                //  cp.OutPutData = _imgstr_.ToArray();
                                                _rwlck_.ReleaseReaderLock();
                                                ctx.Response.ContentType = "text/jpg"; // Important For Chrome Otherwise will display the HTML as plain text.
                                                cp.Requesting_Binary_data = true;
                                                cp.Processing_Type = ProcessingResult.Binary;
                                            }
                                        }

                                    }
                               
                            }
                        }
                        else if (AppHelper.IsFileExist(cp.RequestPage))                   //dynamic  static page  or bin 
                        {
                            cp.RequestPage = AppHelper.Correctpath(cp.RequestPage);
                            if (_MainCompiler_.IsCompilable(cp.RequestPage))   //dynamic page                         {
                            {
                                cp.Output_document = _MainCompiler_.CompileThis(cp.RequestPage, cp.Url.Query.ToString(), cp.RequestPostData);
                                cp.SetData_ReadTextFile(cp.Output_document);
                            }
                            else    
                            {
                                long filesize = AppHelper.FileSize(cp.RequestPage);
                                if (this.Servconf.MaxFileSize < filesize)
                                    cp.Die(200, "File is too big");
                                else    if (WebServer.IsBinFile(cp.RequestPage))           // binary 
                                {

                                    cp.Output_document = (cp.RequestPage);
                                    cp.Requesting_Binary_data = true;
                                    cp.SetData_ReadBinFile(cp.RequestPage);
                                    cp.ContentType = "content/" + cp.Request_extn;
                                    cp.Processing_Type = ProcessingResult.Binary;
                                }
                                else                                            // static  page
                                {
                                    cp.Output_document = WebDesigner.ReadFile(cp.RequestPage);
                                    cp.SetData_ReadTextFile(cp.Output_document);
                                    cp.ContentType = "text/" + cp.Request_extn;

                                }
                            }

                        }

                        else if (ctx.Request.Url.LocalPath.EndsWith("/") || AppHelper.ExistedDir(cp.RequestPage))
                        //default index or browse Dir
                        {

                            string outed = "";
                            if (_MainCompiler_.IsFoundDefaultIndex(cp.RequestPage, out outed))
                                cp.Output_document = _MainCompiler_.CompileThis((outed == "") ? cp.RequestPage : outed, cp.Url.Query.ToString(), cp.RequestPostData);

                            else if (WebServer.IsDirectoryFound(cp.RequestPage))
                            {
                                cp.Output_document = WebDesigner.ListDirectory(cp.RequestPage, WebServer.ListDir(cp.RequestPage, this._RootDirectory, cp.Requesting_Host, this._Port.ToString()), Servconf);
                                cp.LoadMaster = true;
                            }
                            else
                            {
                                cp.Output_document = WebDesigner.FileNotFoundTitle_Traditional(cp.Requesting_Host, this._Port.ToString());
                                cp.Output_code = 404;
                                cp.Processing_Type = ProcessingResult.NotFound;

                            }

                        }
                        else                         // not found 
                        {

                            cp.Output_document = WebDesigner.FileNotFoundTitle_Traditional(cp.Requesting_Host, this._Port.ToString());
                            cp.Output_code = 404;
                            cp.Processing_Type = ProcessingResult.NotFound;
                            cp.LoadMaster = true;
                            cp.NextTitle = "";

                        }
                        
                        ctx.Response.StatusCode = cp.Output_code;
                        ctx.Response.ContentType = cp.ContentType;
                        ctx.Response.Headers["server"] = cp.ResponseServerHeader;
                        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                        ctx.Response.Headers["X-Powered-By"] = "C#-4Neso-Ryon";

                        foreach (var p in cp.Response_Headers)
                            ctx.Response.Headers.Add(p.id, p.value);

                        if(cp.ContentType.Contains("html"))
                           cp.Output_document = cp.Output_document.Replace(WebServer.NewLineReplacor, "<br />");

                        if (cp.Requesting_Binary_data && !cp.Dead)
                        {
                            ctx.Response.Headers.Add("Accept-Ranges", "bytes");
                            ctx.Response.Headers.Add("Last-Modified", "");
                            ctx.Response.Headers.Add("Server", "Rykon");
                            ctx.Response.Headers.Add("Date", System.DateTime.Now.ToShortDateString());
                            ctx.Response.Headers.Add("Content-Type", "image/" + cp.Request_extn);
                            await ctx.Response.OutputStream.WriteAsync(cp.OutPutData, 0, cp.OutPutData.Length);
                        }
                        else
                        {
                            if (cp.LoadMaster)
                            {
                                cp.NextTitle=WebServer.EncodeHtmlChars(cp.NextTitle);
                                cp.Output_document = (WebServer.masterPagePre_(Program._AppverName, cp.NextTitle) + cp.Output_document + WebServer.masterPageAfter);
                            } await ctx.Response.OutputStream.WriteAsync(ASCIIEncoding.UTF8.GetBytes(cp.Output_document), 0, cp.Output_document.Length);
                        } ctx.Response.Close();

                        if (cp.Processing_Type == ProcessingResult.AuthRequired)
                            continue;


                    }
                    //catch
                    //{
                    //    cp.Output_document = WebDesigner._501InternalServerError(cp.Requesting_Host, this._Port.ToString(), this.ServerConfiguration);
                    //    cp.Output_code = 501;
                    //}
                    catch (OutOfMemoryException h)
                    {
                        cp.ErrorMessage = h.Message;
                        cp.exception = ExceptionType.OutOfMemory_;
                    }
                    catch (HttpListenerException h)
                    {
                        cp.ErrorMessage = h.Message;
                        cp.exception = ExceptionType.HttpListner_;
                        if (h.Message.Contains("The specified network name is no longer ava"))
                            cp.ErrorMessage = "Dropped Request";
                    }

                    if (cp.exception != ExceptionType.none_)
                    {
                        cp.ServerErroroccured = true;
                        cp.Output_code = 501;
                        ctx.Response.StatusCode = cp.Output_code;

                        switch (cp.exception)
                        {
                            case ExceptionType.OutOfMemory_:
                                {
                                     cp.Output_document = WebServer.GetInternalErrorException(cp.exception);
                                    break;
                                }
                            
                            case ExceptionType.HttpListner_:
                                { 
                                    if (cp.ErrorMessage == "The I/O operation has been aborted because of either a thread exit or an application request" || cp.ErrorMessage== "The specified network name is no longer available")
                                    {
                                        this._CanceledReqs++;
                                        cp.exception = ExceptionType.CanceledByRequestor;
                                        cp.Output_document = "Request Canceled by client";
                                        cp.Canceled = true;
                                    }
                                    break;
                                }

                        }

                        try // Informing client with server error
                        {
                            await ctx.Response.OutputStream.WriteAsync(ASCIIEncoding.UTF8.GetBytes(cp.Output_document), 0, cp.Output_document.Length);
                        }
                        catch (Exception h) { cp.ErrorMessage = h.Message;   cp.exception = ExceptionType.FailedToHandle;}
                         
                    }
                    
                   // ctx.Response.OutputStream.Close();
                    ctx.Response.Close();
                   
                    if (!cp.Canceled)
                        _handled++;

                    ViewLog("  ["+cp.Requesting_Host+"]   ["+cp.Url.LocalPath+WebServer.DecodeUrlChars(cp.Url.Query)+ "]    [" + WebDesigner.StatueCode(cp.Output_code)+((cp.ServerErroroccured)?("("+cp.ErrorMessage+")"):"")+"]   ["+cp.getLenght()+"]");
                    
                    ShowCounters();
                }
                catch (Exception sas) { ViewLog(sas.Message); }

            }
            if (!_Listening_)
                stopserver();
        }

        private void GenerateMediaPlayer()
        {
            string mp = this._RootDirectory + "\\Video\\index.html";
            mp = AppHelper.RepairPathSlashes(mp);
            AppHelper.RepairPath(mp);
            string d = WebDesigner.VideoDefaultIndex(this._RootDirectory, this._Prefixs_[(cb_Prefixs.SelectedIndex)].Item2, this._Port.ToString());
            bool b = AppHelper.writeToFile(mp, d);
        }
        private void GenerateControlIndex()
        {
            string mp = this._RootDirectory + "\\Control\\index.html";
            mp = AppHelper.RepairPathSlashes(mp);
            AppHelper.RepairPath(mp);

            string d = WebDesigner.ControlCommandListindex(this._RootDirectory, this._Prefixs_[(cb_Prefixs.SelectedIndex)].Item2, this._Port.ToString(), Servconf.CSRF, "");// ,this.Servconf.ControlPassword);
            bool b = AppHelper.writeToFile(mp, d);

        }
        private void generatedefaultindex()
        {
            string mp = this._RootDirectory + "\\index.html";
            mp = AppHelper.RepairPathSlashes(mp);
            AppHelper.RepairPath(mp);
            WebServer.SetMaster(mp);

            string d = WebDesigner.DefaultIndex(this._RootDirectory, mainurl());
            bool b = AppHelper.writeToFile(mp, d);


        }

        private string mainurl()
        {
            return this._Prefixs_[(cb_Prefixs.SelectedIndex)].Item2 +"::"+this._Port.ToString();
        }
        private void GenerateListenPlayer()
        {
            string mp = this._RootDirectory + "\\Listen\\index.html";
            mp = AppHelper.RepairPathSlashes(mp);
            AppHelper.RepairPath(mp);

            string d = WebDesigner.ListenDefaultIndex(this._RootDirectory, this._Prefixs_[(cb_Prefixs.SelectedIndex)].Item2, this._Port.ToString());
            bool b = AppHelper.writeToFile(mp, d);
        }
        private void stopserver()
        {
            try
            {
                _MainServer_.Stop();
                _MainServer_.Abort();
                _MainServer_ = new HttpListener();

                string xt = "Server stopped";
                Ballooon(xt);
                ViewLog(xt);

                SetStatue(xt);
                notifyIcon1.Text = "Rykon Offline ";
            }
            catch { }
        }
        public FormMain()
        {
            InitializeComponent();
            InitializeComponent2();

            _MainServer_ = new HttpListener();
            _MainCompiler_ = new XCompiler();
            this._LogsFilePath = Application.StartupPath + "\\Logs.txt";
            this._ErrorFilePath = Application.StartupPath + "\\Errors.txt";

            LoadServerConfiguration();

            this._StreamerEnabled = Servconf.EnableStream;
            this._ControlerEnabled_ = Servconf.EnableControler;

            LoadPrefixes();
            LoadFiles();


            LoadLanguages();
            LoadSettings();


            SetStatue(""); 

        }

        private void InitializeComponent2()
        {
             
        }

        private void LoadServerConfiguration()
        {
            string ServerConfigPath = Application.StartupPath + "\\Req\\httpd.conf".Replace("\\\\", "\\");
            this.Servconf = new ServerConfig(ServerConfigPath);


            cp_private.Checked = !Servconf.IsPublicServer;
            txbxServerPass.Text = Servconf.ServerAutPass;
            TxbxServerId.Text = Servconf.ServerAuthId;

            TxbxServerId.Text = Servconf.ServerAuthId;
            txbxServerPass.Text = Servconf.ServerAutPass;
            txbx_pass_Control.Text = Servconf.ControlPassword;
            txbx_pass_Listen.Text = Servconf.ListenPassword;
            txbx_pass_Stream.Text = Servconf.StreamPassword;
            txbx_pass_Upload.Text = Servconf.UploadPassword;
            txbx_pass_video.Text = Servconf.VideoPassword;

            cb_secure_control.Checked = Servconf.SecureControl;
            cb_secure_listen.Checked = Servconf.SecureListen;
            cb_secure_stream.Checked = Servconf.SecureStream;
            cb_secure_upload.Checked = Servconf.SecureUpload;
            cb_secure_video.Checked = Servconf.SecureVideo;


            videoToolStripMenuItem.Checked =videoonToolStripMenuItem1.Checked= Servconf.EnableVideo;
            offvideoToolStripMenuItem1.Checked = !Servconf.EnableVideo;

            listenToolStripMenuItem.Checked = onlistenToolStripMenuItem.Checked = Servconf.EnableListen;
            offlistenToolStripMenuItem.Checked = !Servconf.EnableListen;


            int port = this.Servconf.Port;
            if (port > 0 && port <= 65353)
                this.NumPort.Value = port;

            string v= Servconf.GetMainApp();
            gpxmainapp.Text = v;
            comboBoxmainapp.SelectedItem = v;
            labelmainapp.Text = v + " url";


        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            linkLabel2.Text = Program._Version;

            this.linkLabel15.Text = Program._Version;
            //this.Size = new Size(this.Size.Width + 10, this.Size.Height + 10);
            TbControlSettings.Size = new Size(470, 236);
            TbControlSettings.Location = new Point(89, 2);
            this.MinimumSize = this.Size;
            comboBoxmainapp.SelectedIndex=0;


            if (Servconf.AutoStartListening)
                btnSwitch.PerformClick();
            else
                ChangeControlerS();

            CloseTabPages();
            comboBoxmainapp.SelectedItem =Servconf.GetMainApp();
            formloaded = true;
            if (cmbxLangs.Items.Count > 0)
                cmbxLangs.SelectedIndex = 0;

        }

        private void CloseTabPages()
        {
            while (tabControlMain.TabCount > 1)
                tabControlMain.TabPages.RemoveAt(1);
        }
        private void LoadLanguages(bool read=true)
        {
            EmptyLangsFields();
            cmbxLangs.Items.Clear();
            if (read)
            {
                this._MainCompiler_.ClearList();
                _MainCompiler_.savepath = Application.StartupPath + "\\Req\\langs.conf".Replace("\\\\", "\\");
                string languagesCollectionString = AppHelper.ReadFileText(_MainCompiler_.savepath);
                cmbxLangs.DisplayMember = "LangName";
                if (languagesCollectionString.Contains(RykonLang.Langs_Separator.ToString()))
                {
                    string[] langsArr = languagesCollectionString.Split(RykonLang.Langs_Separator);
                    foreach (string _l_ in langsArr)
                    {
                        RykonLang r = RykonLang.Build(_l_);
                        if (!r.validLang())
                            continue;
                        _MainCompiler_.AddLanguage(r);
                        cmbxLangs.Items.Add(r.LangName);
                    }
                }
                else if (!string.IsNullOrEmpty(languagesCollectionString))
                {
                    RykonLang r = RykonLang.Build(languagesCollectionString);
                    if (r.validLang())
                    {
                        _MainCompiler_.AddLanguage(r);
                        cmbxLangs.Items.Add(r.LangName);
                    }
                }
            }
            else           
                foreach (RykonLang r in this._MainCompiler_.LanguageList)              
                    cmbxLangs.Items.Add(r.LangName);


        } 
        private void LoadFiles()
        {
            this.logsoldData = AppHelper.ReadFileText(this._LogsFilePath, true);
            this.ErroroldData = AppHelper.ReadFileText(this._ErrorFilePath, true);

            txbxLogs.Text = logsoldData;
        }

        private void LoadPrefixes()
        {
            _Prefixs_ = DrNetwork.GetAllIPv4Addresses();
            foreach (var ip in _Prefixs_)
            {
                cb_Prefixs.Items.Add(ip.Item2 + " - " + ip.Item1);
            }

            if (!SelectIpIfFound(SettingsEditor.GetFavPrefix()))
                if (!SelectIpIfFound("192.168.1.1"))
                    cb_Prefixs.SelectedIndex = cb_Prefixs.Items.Count - 1;


        }

        //netsh http add urlacl url=http://vaidesg:8080/ user=everyone
        //httpcfg set urlacl /u http://vaidesg1:8080/ /a D:(A;;GX;;;WD)


        private Task AddFirewallRule(int port)
        {
            return Task.Run(() =>
            {

                string cmd = RunCMD("netsh advfirewall firewall show rule \"Rykon\"");
                if (cmd.StartsWith("\r\nNo rules match the specified criteria."))
                {
                    cmd = RunCMD("netsh advfirewall firewall add rule name=\"Rykon\" dir=in action=allow remoteip=localsubnet protocol=tcp localport=" + port);
                    if (cmd.Contains("Ok."))
                    {
                        //   SetLog("Rykon Rule added to your firewall");
                    }
                }
                else
                {
                    cmd = RunCMD("netsh advfirewall firewall delete rule name=\"Rykon\"");
                    cmd = RunCMD("netsh advfirewall firewall add rule name=\"Rykon\" dir=in action=allow remoteip=localsubnet protocol=tcp localport=" + port);
                    if (cmd.Contains("Ok."))
                    {
                        //  SetLog("Rykon Rule updated to your firewall");
                    }
                }
            });

        }
        private string RunCMD(string cmd)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/C " + cmd;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            string res = proc.StandardOutput.ReadToEnd();
            proc.StandardOutput.Close();

            proc.Close();
            return res;
        }

        private bool SelectIpIfFound(string p)
        {
            bool found = false;
            int i = -1;
            foreach (string s in cb_Prefixs.Items)
            {
                i++;
                if (s.Trim() == p.Trim())
                {
                    cb_Prefixs.SelectedIndex = i; found = true;
                }
            }


            return found;
        }

        private void LoadSettings()
        {
            this.separatedBrowser = cbTestInSeparated.Checked = SettingsEditor.GetseparatedBrowser();
            this._RootDirectory = txbxRootDir.Text = SettingsEditor.GetRootDirectory(Application.StartupPath);
            this.tstrpBtnStream.Checked = tstrpBtnStreamOn.Checked = _StreamerEnabled;
            this.tstrpBtnStreamOff.Checked = !_StreamerEnabled;

            this.Tsrtrp_controler.Checked = Tsrtrp_controler_on.Checked = _ControlerEnabled_;
            this.Tsrtrp_controler_off.Checked = !_ControlerEnabled_;

        }
        private async void btnSwitch_Click(object sender, EventArgs e)
        {
            Servconf.RegenerateAuthCode();
            btnSwitch.Enabled = false;
            btnSwitch.Enabled = false;

            if (ServerMode == _Mode_.on)//stop 
            {
                this._Listening_ = false;
                this._isTakingScreenshots = false;
                ServerMode = _Mode_.off;
                stopserver();
            }
            else if (ServerMode == _Mode_.off)//pressed start
            {
                try
                {

                    ViewLog("initiating , Please Wait...");

                    _MainServer_.IgnoreWriteExceptions = true;
                    _isTakingScreenshots = true;
                    _Listening_ = true;

                    await AddFirewallRule((int)NumPort.Value);

                    if (_StreamerEnabled)
                        Task.Factory.StartNew(() => CaptureScreenEvery(this.ScreenTicks)).Wait();


                    await StartServer();


                }
                catch (ArgumentException ae)
                {
                    ServerMode = _Mode_.off;
                    string p = ("Starting Server exception " + ae.Message);

                    ViewLog(p);
                }

                catch (HttpListenerException ae)
                {
                    ServerMode = _Mode_.off;
                    string msg = "";
                    if (ae.Message.Contains("The process cannot access the file because it is being used by another process"))
                    {
                        msg = "Port in use ";
                        SetStatue("Can not listen on Port (" + NumPort.Value.ToString() + ") because it  is in use ");
                        if (MessageBox.Show("port is in use , Do you want to try another one?", "Error used port", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Random r = new Random(23232);
                            NumPort.Value = (decimal)r.Next(1000, 64000);
                            btnSwitch_Click(null, null);
                            return;
                        }
                        else
                            msg = ae.Message;

                        string p = ("Starting Server exception " + msg);

                        ViewLog(p);

                    }
                }
                catch (ObjectDisposedException disObj)
                {
                    _MainServer_ = new HttpListener();
                    _MainServer_.IgnoreWriteExceptions = true;
                }
                catch (Exception ae)
                {
                    ServerMode = _Mode_.off;
                    string p = ("Starting Server Error" + ae.Message);
                    // SetError(p);
                    ViewLog(p);
                }
            }
            ChangeControlerS();
        }

       


        private void SavePrefix()
        {
            if (cb_Prefixs.SelectedIndex > -1)
                SettingsEditor.SetFavPrefix(cb_Prefixs.SelectedItem.ToString());

        }
        private void TakeScreenshot(bool captureMouse)
        {
            screened++;
            if (captureMouse)
            {
                var bmp = ScreenCapturePInvoke.CaptureFullScreen(true);
                _rwlck_.AcquireWriterLock(Timeout.Infinite);
                bmp.Save(this._RootDirectory + "/Stream/LiveStream.jpg", ImageFormat.Jpeg);
               
                _rwlck_.ReleaseWriterLock();
                if (_isPreview)
                {
                    _imgstr_ = new MemoryStream();
                    bmp.Save(_imgstr_, ImageFormat.Jpeg);
                    imgPreview.Image = new Bitmap(_imgstr_);
                }
                return;
            }
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                _rwlck_.AcquireWriterLock(Timeout.Infinite);
                bitmap.Save(this._RootDirectory + "/Stream/LiveStream.jpg", ImageFormat.Jpeg);
                _rwlck_.ReleaseWriterLock();
                if (_isPreview)
                {
                    _imgstr_ = new MemoryStream();
                    bitmap.Save(_imgstr_, ImageFormat.Jpeg);
                    imgPreview.Image = new Bitmap(_imgstr_);
                }


            }
        }

        private async Task CaptureScreenEvery(int msec)
        {

            while (_Listening_)
            {
                if (this._isTakingScreenshots && _StreamerEnabled)
                {
                    TakeScreenshot(_isMouseCapture);
                    msec = this.ScreenTicks;// (int)numShotEvery.Value;
                    await Task.Delay(msec);
                }
            }

        }


        private string now()
        {
            return DateTime.Now.ToShortDateString() + "-" + DateTime.Now.ToShortTimeString();
        }

        private void Ballooon(string p)
        {
            this.notifyIcon1.BalloonTipText = p;
            notifyIcon1.ShowBalloonTip(500);
        }


        private void ShowCounters()
        {
            labelStatue.Text = "Handled {" + _handled + "}  Canceled {" + _CanceledReqs + "} ";
        }


        private void ViewLog(string p)
        {
            if (this.lastlogline == p)
                return;

            this.lastlogline = p;
            string nh = "["+(now() + "]  " + p + "\r\n");

            LogsnewData += nh;
            txbxLogs.AppendText ( nh);

            txbxLogs.SelectionStart = txbxLogs.Text.Length - 1;
            txbxLogs.ScrollToCaret();
        }

        private void ChangeControlerS()
        {
            bool starting = ServerMode == _Mode_.Middle;
            _Listening_ = _isTakingScreenshots = (ServerMode == _Mode_.on);

            pcbxLoader.Visible = _Listening_;

            btnSwitch.Enabled =  true;
            onlineToolStripMenuItem.Checked = modeToolStripMenuItem.Checked = _Listening_;
            offlineToolStripMenuItem.Checked = !_Listening_;
            privateToolStripMenuItem.Checked = !Servconf.IsPublicServer;
            publicToolStripMenuItem.Checked = Servconf.IsPublicServer;
            privateToolStripMenuItem.Enabled = publicToolStripMenuItem.Enabled = !_Listening_;
            Servconf.EnableStream = this._StreamerEnabled;
            Servconf.EnableControler = this._ControlerEnabled_;
            
            string stat = "";
            if (_Listening_)
                stat = "Stop Server ";
            else if (starting)
                stat = "starting ..";
            else
                stat = "Start Server";

            btnSwitch.Text = stat; 
            NumPort.Enabled = cb_Prefixs.Enabled = !_Listening_;
             
            txbx_serverUrl.Enabled = _Listening_;
            this.Text = Program._AssemblyName + ((_Listening_) ? ("       (Running on " + NumPort.Value + ")") : (""));
            buttonTestSelfBrowser.Enabled = _Listening_;
            btnTestMainapp.Enabled =( _Listening_&&textBoxUrlMainAPP.Text.Length>2);
            if (!_Listening_)
                txbx_serverUrl.Text = "";

            tstrpBtnStream.Checked = tstrpBtnStreamOn.Checked = this._StreamerEnabled;
            tstrpBtnStreamOff.Checked = !this._StreamerEnabled;

            Tsrtrp_controler_on.Checked = Tsrtrp_controler.Checked = this._ControlerEnabled_;
            Tsrtrp_controler_off.Checked = !this._ControlerEnabled_;
            Tsrtrp_controler_on.Enabled =
            Tsrtrp_controler_off.Enabled =
            tstrpBtnStreamOff.Enabled =
            tstrpBtnStreamOn.Enabled = ServerMode == _Mode_.off;
            panelBottom.BackColor = (_Listening_) ? Color.Yellow : Color.FromArgb(202, 81, 0);
            reloadServerConfigurationToolStripMenuItem.Enabled =    ServerMode!=_Mode_.on;
        }

        private void SetStatue(string p)
        {
            labelStatue.Text = p;
        }

        private void SetLog(string p)
        {
            try
            {
                ViewLog(p);
                this.LogsnewData += p + "\r\n";

            }
            catch { }
        }

        private void txbxRootDir_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnBrowseDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            if (f.ShowDialog() != DialogResult.OK)
                return;
            txbxRootDir.Text = f.SelectedPath;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            //  new FrmSettings(_MainCompiler_).ShowDialog();
            LoadSettings();
        }

        private void NumPort_ValueChanged(object sender, EventArgs e)
        {
            Servconf.Port = this._Port = (int)NumPort.Value;



        }

        private void cb_Prefixs_SelectedIndexChanged(object sender, EventArgs e)
        {
            SavePrefix();
        }


        private void btnCopyUrl_Click(object sender, EventArgs e)
        {

        }


        private void buttonTestSelfBrowser_Click(object sender, EventArgs e)
        {
            Test(txbx_serverUrl.Text);
        }

        private void Test(string p)
        {
            switch (AppConfiguration.Testingmode)
            {
                case TestingType.BuiltIn: LaunchBrowser(p); break ;
                case TestingType.InBrowser: Program.lauchProcess(p); break;
                case TestingType.Separeted: SepTestingForm(p); break;


            }
        }

        private void SepTestingForm(string p)
        {
            TestingForm.ShowInTaskbar = true;
            if (TestingForm.IsDisposed)
                TestingForm = new FrmSelfBrowser();

            TestingForm.SetUrl(p);
            TestingForm.Show();
        }
        private void LaunchBrowser(string url)
        {

            if (url.Length < 2)
                return;

                LookForTabPAge(tabControlMain, tbpgBrowser);
                textBox_BrowserUrl.Text = url;
                buttonNavigate.PerformClick();
                
            
           
        }

        private void linkLabelOpenRootDir_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            if (System.IO.Directory.Exists(txbxRootDir.Text))
                AppHelper.StartProcess(txbxRootDir.Text);
        }


        private void FormMain_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                //  notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            //notifyIcon1.Visible = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;

            this.btnTestMainapp.Size = this.StreamBtnsize;
            this.btnTestMainapp.Location = this.StreamBtnLocation;


            this.buttonTestSelfBrowser.Size = this.serverBtnsize;
            this.buttonTestSelfBrowser.Location = this.serverBtnLocation;

        }


        private void cb_Streamer_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Switch_cb_Streamer_()//object sender, EventArgs e)
        {
            _StreamerEnabled = tstrpBtnStream.Checked; 
            Servconf.EnableStream = _StreamerEnabled;

        }

        private void txbxUrl_TextChanged(object sender, EventArgs e)
        {
            lnk_copyUrl.Visible = txbx_serverUrl.TextLength > 1;
            textBoxUrlMainAPP.Text = (txbx_serverUrl.Text.Length < 1 || !_StreamerEnabled) ? "" : (txbx_serverUrl.Text + "Stream");
        }

        private void buttonCopyStrmUrl_Click(object sender, EventArgs e)
        {

        }

        private void btnTestApp_Click(object sender, EventArgs e)
        { 
                Test(textBoxUrlMainAPP.Text);
        }

        private void label5_Click(object sender, EventArgs e)
        {
            string x = "";
            x += "mouscap = " + this._isMouseCapture + "\r";
            x += "iswork = " + this._Listening_ + "\r";
            x += "shotev = " + this._ShotEvery + "\r";
            x += "mouscap = " + this._isMouseCapture + "\r";
            x += "istakscrs =" + this._isTakingScreenshots + "\r";
            x += "screentook " + screened;
            MessageBox.Show(x);
        }


        public int ScreenTicks = 500;


        public string lastlogline = "";

        private void gpxStreamer_Enter(object sender, EventArgs e)
        {

        }

        private void lnk_copyUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (string.IsNullOrEmpty(txbx_serverUrl.Text))
                return;
            Clipboard.SetText(txbx_serverUrl.Text);
            SetStatue("Url Copied to Clipboard");
        }

        private void lnk_copyStreamUrl_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txbx_serverUrl.Text))
                return;
            Clipboard.SetText(textBoxUrlMainAPP.Text);
            SetStatue("Url Copied to Clipboard");
        }

        private void txbxUrl_TextChanged_1(object sender, EventArgs e)
        {
        }

       

        private void tstrpBtnStreamOn_Click(object sender, EventArgs e)
        {
            _StreamerEnabled = this.Servconf.EnableStream = true;
            ChangeControlerS();
        }

        private void tstrpBtnStreamOff_Click(object sender, EventArgs e)
        {
            _StreamerEnabled = this.Servconf.EnableStream = false;
            ChangeControlerS();
        }

        private void Tsrtrp_controler_off_Click(object sender, EventArgs e)
        {
            _ControlerEnabled_ = this.Servconf.EnableControler = false;
            ChangeControlerS();
        }

        private void Tsrtrp_controler_on_Click(object sender, EventArgs e)
        {
            this._ControlerEnabled_ = this.Servconf.EnableControler = true;
            ChangeControlerS();
        }
        public bool _ControlerEnabled_ = false;
        private void lnk_copyStreamUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.textBoxUrlMainAPP.TextLength > 0)
                Clipboard.SetText(textBoxUrlMainAPP.Text);
        }

        private void onlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ServerMode==_Mode_.on)
                return;

            onlineToolStripMenuItem.Checked = modeToolStripMenuItem.Checked = true;
            offlineToolStripMenuItem.Checked = false;
            btnSwitch.PerformClick();
        }

        private void offlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ServerMode!=_Mode_.on)
                return;
            onlineToolStripMenuItem.Checked = modeToolStripMenuItem.Checked = false;
            offlineToolStripMenuItem.Checked = true;
            btnSwitch.PerformClick();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnk_copyUrl_VisibleChanged(object sender, EventArgs e)
        {
            int w = btnSwitch.Size.Width;
            if (!lnk_copyUrl.Visible)
            {
                buttonTestSelfBrowser.Size = new Size(w, 23);
                buttonTestSelfBrowser.Location = new Point(424, 38);
            }
            else
            {
                w = w - (lnk_copyUrl.Size.Width);

                buttonTestSelfBrowser.Size = new Size(w, 23);
                buttonTestSelfBrowser.Location = new Point(458, 38);
            }

            this.serverBtnLocation = buttonTestSelfBrowser.Location;
            this.serverBtnsize = buttonTestSelfBrowser.Size;
        }

        private void lnk_copyMainAppUrl_VisibleChanged(object sender, EventArgs e)
        {
            int w = btnSwitch.Size.Width;

            if (!lnk_copy_mainapp_Url.Visible)
            {
                btnTestMainapp.Size = new Size(w, 23);
                btnTestMainapp.Location = new Point(424, 14);
            }
            else
            {
                w = w - (lnk_copy_mainapp_Url.Size.Width);
                btnTestMainapp.Size = new Size(w, 23);
                btnTestMainapp.Location = new Point(463, 14);
            }
            this.StreamBtnLocation = btnTestMainapp.Location;
            this.StreamBtnsize = btnTestMainapp.Size;

        }

        private void btnSwitch_EnabledChanged(object sender, EventArgs e)
        {
            btnTestMainapp.BackColor = (btnTestMainapp.Enabled) ? Color.FromArgb(225, 111, 9) : Color.FromArgb(250, 200, 150);
            buttonTestSelfBrowser.BackColor = (buttonTestSelfBrowser.Enabled) ? Color.FromArgb(225, 111, 9) : Color.FromArgb(250, 200, 150);
            btnSwitch.BackColor = (btnSwitch.Enabled) ? Color.FromArgb(225, 111, 9) : Color.FromArgb(250, 200, 150);


        }

        public Size StreamBtnsize = new Size(64, 23);

        public Point StreamBtnLocation = new Point(424, 14);

        public Point serverBtnLocation = new Point(463, 38);

        public System.Drawing.Size serverBtnsize = new Size(64, 23);

        private void timerWriter_Tick(object sender, EventArgs e)
        {
            saveData(); 
        }

        private void SaveLogs()
        {
            AppHelper.writeToFile(this._LogsFilePath, (this.logsoldData + "\r\n" + this.LogsnewData).Trim());
        }

        public string logsoldData = "";

        public string LogsnewData = "";



        internal void saveData()
        {
           this.Servconf.SaveChanges();
           _MainCompiler_.Save();
            SaveLogs();
            saveErrors();
            SettingsEditor.Save();
            SetStatue("All Saved");
        }

        private void saveErrors()
        {
            AppHelper.writeToFile(this._ErrorFilePath, this.ErroroldData + "\r\n" + this.ErrorsnewData);

        }

        public string ErroroldData = "";//{ get; set; }
        public string ErrorsnewData = "";

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ServerMode == _Mode_.on)
            {
                DialogResult dg = MessageBox.Show("Server is online \r\n if you exited , it will not be able to process current connection \r\n Are you sure to exite ?", "warning server online ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dg == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                btnSwitch.PerformClick();
            }
            saveData();
            notifyIcon1.Icon = null;
            notifyIcon1.Dispose();

        }

        private void lnkcloseLogs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CloseCurrentTabPage();
        }

        private void CloseCurrentTabPage()
        {
            int i = tabControlMain.SelectedIndex;
            if (tabControlMain.TabPages[i].Text == tbpgBrowser.Text)
                webBrowser1.Navigate("");

            tabControlMain.TabPages.RemoveAt(i);
            tabControlMain.SelectedIndex = ((i < tabControlMain.TabCount)) ? i : tabControlMain.TabCount - 1;
        }

        private void logsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            LookForTabPAge(tabControlMain, tabPageLogs);
            LookForTabPAge(TbControlSettings, tbpg_set_Server);
        }

        private void LookForTabPAge(TabControl x, TabPage tpg)
        {
            for (int i = 0; i < x.TabCount; i++)
                if (x.TabPages[i].Text == tpg.Text)
                {
                    x.SelectedIndex = i;
                    return;
                }
            x.TabPages.Add(tpg);
            x.SelectedIndex = x.TabCount - 1;

        }

        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            LookForTabPAge(tabControlMain, tabPageSettings);

        }

        private void lnk_close_Tab(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CloseCurrentTabPage();
        }
         

        private void viewStreamerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LookForTabPAge(tabControlMain, tabPage_streamer);

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LookForTabPAge(tabControlMain, tabPageAbout);

        }



        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(Program.GithUbURl);
        }

        private void closeTabsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseTabPages();
        }

        private void cbswitch_streamerPreview_CheckedChanged(object sender, EventArgs e)
        {
            _isPreview = cbswitch_streamerPreview.Checked;
            if (_isPreview == false)
                this.imgPreview.BackgroundImage = global::RykonServer.Properties.Resources.Untitled;

        }

        private void reloadServerConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadLanguages();
            LoadServerConfiguration();
        }




        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

            try
            {
                TbControlSettings.SelectedIndex = int.Parse(treeView1.SelectedNode.Tag.ToString());
            }
            catch { }
        }

        private void privateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            privateToolStripMenuItem.Checked = !privateToolStripMenuItem.Checked;
            publicToolStripMenuItem.Checked = !privateToolStripMenuItem.Checked;

            if (privateToolStripMenuItem.Checked)
                Servconf.IsPublicServer = true;
        }

        private void publicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            publicToolStripMenuItem.Checked = !publicToolStripMenuItem.Checked;
            privateToolStripMenuItem.Checked = !publicToolStripMenuItem.Checked;

            if (publicToolStripMenuItem.Checked)
                Servconf.IsPublicServer = true;
        }

        private void credintialToolStripMenuItem_Click(object sender, EventArgs e)
        {

            LookForTabPAge(tabControlMain, tabPageSettings);
            LookForTabPAge(TbControlSettings, tbpgSecurity);


        }

        private void cp_private_CheckedChanged(object sender, EventArgs e)
        {
            Servconf.IsPublicServer = !cp_private.Checked;
            panel_server_credit.Enabled = cp_private.Checked;
            
            publicToolStripMenuItem.Checked = Servconf.IsPublicServer = !cp_private.Checked;
            privateToolStripMenuItem.Checked = !publicToolStripMenuItem.Checked;
        
        }

        private void Txbx_ServerId_TextChanged(object sender, EventArgs e)
        {
            this.Servconf.ServerAuthId = TxbxServerId.Text;

        }

        private void txbxServerPass_TextChanged(object sender, EventArgs e)
        {
            this.Servconf.ServerAutPass = txbxServerPass.Text;
        }

        private void saveNewSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveData();
        }

        private void cbTestInSeparated_CheckedChanged(object sender, EventArgs e)
        {
            SettingsEditor.SetseparatedBrowser(cbTestInSeparated.Checked); 
            this.separatedBrowser = cbTestInSeparated.Checked;
        }

        private void buttonNavigate_Click(object sender, EventArgs e)
        {
            navigateNow();
        }
        private void navigateNow()
        {

            webBrowser1.Navigate(textBox_BrowserUrl.Text);
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            webBrowser1.GoBack();
        } 

        private void cb_run_atstartup_CheckedChanged(object sender, EventArgs e)
        {
            AppHelper.SetStartup(Program._AppName, cb_run_atstartup.Checked);
            AppConfiguration.RunAtStartUp = cb_run_atstartup.Checked;
        }
        private void traymenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
           MessageBox.Show( (string)sender);
        }

        private void viewPlayerToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void viewRListenToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
      

        private void lstbxExts_SelectedIndexChanged(object sender, EventArgs e)
        {
            lnk_removeExtn.Visible = lstbxExts.SelectedIndex> 0;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            lnk_add_newExt.Visible = TxbxAddExtn.TextLength > 1;
        }

        private void lnk_add_newExt_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string p = TxbxAddExtn.Text.Trim().ToLower();
            p = AppHelper.RemoveInvalidPathChars(p);
            if (!p.StartsWith("."))
                p = "." + p;
            if (lstbxExts.Items.Contains(p))
                return;
            lstbxExts.Items.Add(p);
        }

        private void lnk_ClearExtns_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lstbxExts.Items.Clear();
        }

        private void panel_lang_controls_VisibleChanged(object sender, EventArgs e)
        {
            lnk_langPath_Browse.Visible = panel_lang_controls.Visible;
            txbxLangArgs.ReadOnly = txbxLangName.ReadOnly = txbxLangPath.ReadOnly = txbxLangVer.ReadOnly = !panel_lang_controls.Visible;
            cb_langEnabled.Enabled = panel_lang_controls.Visible;

        }

        private void lnk_langPath_Browse_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            OpenFileDialog o = new OpenFileDialog();
            o.Filter = "EXE|*.exe";

            if (o.ShowDialog() == DialogResult.OK)
                this.txbxLangPath.Text = o.FileName;
        }

        private void btnEditLang_Click(object sender, EventArgs e)
        {
            panel_lang_controls.Visible = !panel_lang_controls.Visible;
            bool EditMode = panel_lang_controls.Visible;
            
            btnEditLangs.Text = EditMode ? "Save" : "Edit";
            cmbxLangs.Enabled =  btnNewLang.Visible = !EditMode;

            if (!EditMode)
                SaveLangs(); 

        }

        private bool SaveLangs()
        {
            RykonLang SelectedLanguage = new RykonLang();
            SelectedLanguage.LangName = txbxLangName.Text;

            if (SelectedLanguage.LangName.Length < 2)
            {
                SetStatue("language should has a name");
                return false;
            }

            SelectedLanguage.ProcessArgs = txbxLangArgs.Text;
            if (SelectedLanguage.LangName.Length < 2)
                 SetStatue("Be careful no args for this compiler"); 

            SelectedLanguage.CompilerPath = txbxLangPath.Text;
            if(!AppHelper.IsFileExist (SelectedLanguage.CompilerPath))
            {
                SetStatue("language should has available compiler");
                return false;
            }

            SelectedLanguage.LangVersion = txbxLangVer.Text; 
            if (SelectedLanguage.LangName.Length < 1)
            {
                SetStatue("Language version unknown , i assigned a 1 for default");
                SelectedLanguage.LangVersion = "1";
            }

            SelectedLanguage.InitExts(lstbxExts.Items);
            if (SelectedLanguage.FileExts.Count < 1)
            {
                SetStatue("language should has at least one file extension");
                return false;
            }

            EmptyLangsFields(false);
            lstbxExts.Items.Clear();


            this._MainCompiler_.AddLanguage(SelectedLanguage);
            txbxLangName.Text = txbxLangArgs.Text = txbxLangPath.Text = txbxLangVer.Text = "";
            lstbxExts.Items.Clear();
            SetStatue("upated");

            _MainCompiler_.Save();
            LoadLanguages(false);
            cmbxLangs.SelectedIndex = -1;
            cmbxLangs.SelectedIndex = cmbxLangs.Items.Count-1;            
            return true;
        }

        private void cmbxLangs_SelectedIndexChanged(object sender, EventArgs e)
        {            
            cb_langEnabled.Visible = btnEditLangs.Visible =
            buttonRemoveSelectedLang.Visible = cmbxLangs.SelectedIndex > -1;
        }
        bool insertmode = false;
        
        private void btn_New_Lang_Click(object sender, EventArgs e)
        {
            if(insertmode)
                if(!this.cancelAdding)
                   if(! SaveLangs())
                       return ;

            insertmode = !insertmode;
            if (insertmode)
            {
                EmptyLangsFields();
                btnNewLang.Text = "Save";
            }
            else
            {
                cmbxLangs.Visible = true;
                btnNewLang.Text = "New";

            }
            txbxLangArgs.ReadOnly = txbxLangName.ReadOnly = txbxLangPath.ReadOnly = txbxLangVer.ReadOnly = !insertmode; 
            panel_lang_controls.Visible = !txbxLangVer.ReadOnly;
           
        }

        private void EmptyLangsFields(bool viewcmbx=true)
        {
            cmbxLangs.Visible = viewcmbx;
            lstbxExts.Items.Clear();
            cb_langEnabled.Checked = false;
            txbxLangName.Text = txbxLangVer.Text = txbxLangArgs.Text = txbxLangPath.Text = "";
        }

        private void lstbxLangs_SelectedValueChanged(object sender, EventArgs e)
        {

            EmptyLangsFields(true);
            try
            {
                RykonLang r = _MainCompiler_.LanguageList[cmbxLangs.SelectedIndex];
                txbxLangName.Text = r.LangName;
                txbxLangArgs.Text = r.ProcessArgs;
                txbxLangPath.Text = r.CompilerPath;
                txbxLangVer.Text = r.LangVersion;
                lstbxExts.Items.Clear();
                foreach (string c in r.FileExts)
                    lstbxExts.Items.Add(c);
                cb_langEnabled.Checked = r.Enabled;
            }
            catch { }
        }

        private void lnk_removeExtn_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lstbxExts.SelectedIndex > -1)
                lstbxExts.Items.RemoveAt(lstbxExts.SelectedIndex);
        }

        private void txbxRootDir_TextChanged_1(object sender, EventArgs e)
        {
            Servconf.RootDirectory = txbxRootDir.Text;
        }

        private void txbx_pass_video_TextChanged(object sender, EventArgs e)
        {
            Servconf.VideoPassword = txbx_pass_video.Text;
        }

        private void txbx_pass_Listen_TextChanged(object sender, EventArgs e)
        {
            Servconf.ListenPassword = txbx_pass_Listen.Text;
        }

        private void txbx_pass_Control_TextChanged(object sender, EventArgs e)
        {
            Servconf.ControlPassword = txbx_pass_Control.Text;
        }

        private void txbx_pass_Stream_TextChanged(object sender, EventArgs e)
        {
            Servconf.StreamPassword = txbx_pass_Stream.Text;
        }

        private void txbx_pass_Upload_TextChanged(object sender, EventArgs e)
        {
            Servconf.UploadPassword=            txbx_pass_Upload.Text;
        }

        private void cb_secure_video_CheckedChanged(object sender, EventArgs e)
        {
            Servconf.SecureVideo =txbx_pass_video.Enabled= cb_secure_video.Checked;
        }

        private void cb_secure_listen_CheckedChanged(object sender, EventArgs e)
        {
            Servconf.SecureListen= txbx_pass_Listen.Enabled = cb_secure_listen.Checked;

        }

        private void cb_secure_control_CheckedChanged(object sender, EventArgs e)
        {
            Servconf.SecureControl = txbx_pass_Control.Enabled = cb_secure_control.Checked;

        }

        private void cb_secure_stream_CheckedChanged(object sender, EventArgs e)
        {
            Servconf.SecureStream = txbx_pass_Stream.Enabled = cb_secure_stream.Checked;

        }

        private void cb_secure_upload_CheckedChanged(object sender, EventArgs e)
        {
            Servconf.SecureUpload = txbx_pass_Upload.Enabled = cb_secure_upload.Checked;

        }

        private void label3_Click(object sender, EventArgs e)
        {
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void comboBoxmainapp_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!formloaded)
                return;
            if (comboBoxmainapp.SelectedIndex > -1)
                Servconf.setMainApp ( comboBoxmainapp.SelectedItem.ToString());
        }



        public bool formloaded  =false;

        private void textBoxUrlMainAPP_TextChanged(object sender, EventArgs e)
        {
            lnk_copy_mainapp_Url.Visible = textBoxUrlMainAPP.TextLength > 1;
           
        }

        private void cmbxLangs_VisibleChanged(object sender, EventArgs e)
        {
            label_settings_langs_id.Visible = cmbxLangs.Visible;
        }

        public bool cancelAdding =false;

        private void bnt_cancel_add_newLang_Click(object sender, EventArgs e)
        {
            this.cancelAdding = true;
            btnNewLang.PerformClick();
        }

        private void panel11_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txbxLangPath_TextChanged(object sender, EventArgs e)
        {
            lbl_compiler_exist.Text="Executer "+((AppHelper.IsFileExist(txbxLangPath.Text))?"":" not")+" Existed";
            lbl_compiler_exist.Visible = txbxLangPath.TextLength > 2;
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            textBox_BrowserUrl.Text = webBrowser1.Url.ToString();
        }

        private void buttonRemoveSelectedLang_Click(object sender, EventArgs e)
        {
            int i = cmbxLangs.SelectedIndex;
            DialogResult d = MessageBox.Show("Are you sure that you want to unistall " + _MainCompiler_.LanguageList[i] + "?", "unistalling warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (d != System.Windows.Forms.DialogResult.Yes)
                return;
            cmbxLangs.Items.RemoveAt(i);
            _MainCompiler_.LanguageList.RemoveAt(i);
            LoadLanguages(false);
            label_saveSettings.Visible = true;

        }

        private void label9_VisibleChanged(object sender, EventArgs e)
        {
            linkLabel_no.Visible = linkLabel_yes.Visible = label_saveSettings.Visible;
        }

        private void linkLabel_no_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            label_saveSettings.Visible = false;
        }

        private void linkLabel_yes_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            saveData();
            label_saveSettings.Visible = false;
        }

        private void whatAreMyIPsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormIps f = new FormIps();
            f.Icon = this.Icon;
            f.locips = this._Prefixs_;
            f.port = NumPort.Value;
            f.ShowDialog();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsEditor.SetseparatedBrowser(cbTestInSeparated.Checked);
            this.separatedBrowser = cbTestInSeparated.Checked;
            switch(comboBox1.SelectedIndex)
            {
                case 0:
                this.AppConfiguration.Testingmode = TestingType.BuiltIn;
                break;

                case 1:
                this.AppConfiguration.Testingmode = TestingType.InBrowser;
                break;

                case 2:
                this.AppConfiguration.Testingmode = TestingType.Separeted;
                break;

            }   
        }
 
 }
}

