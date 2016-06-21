using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RykonServer.Forms
{
    public partial class FormIps : Form
    {
        public FormIps()
        {
            InitializeComponent();
        }

        private void linkLabelcopyip_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (textBoxIP.Text.Length >0)
                Clipboard.SetText(textBoxIP.Text);
        }

        private void linkLabel_GetIp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ResetIp();
        }

        private void ResetIp()
        {
            try
            {
                textBoxIP.Text = "";
                var req = (HttpWebRequest)WebRequest.Create("http://canyouseeme.org");
                var resp = (HttpWebResponse)req.GetResponse();
                string x = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                //  input type="hidden" name="IP" value="41.28.175.2"/>
                string[] sepd = x.Split(new char[] { '<' });
                foreach (string s in sepd)
                    if (s.Contains("input type=\"hidden\" name=\"IP\""))
                    {
                        string[] spdbyspace = s.Split(new char[] { ' ' });
                        foreach (string sen in spdbyspace)
                            if (sen.Contains("value"))
                            {
                                string[] spd_by_qoute = sen.Split(new char[] { '"' });
                                this.CurrentExIp = spd_by_qoute[1];
                                textBoxIP.Text = this.CurrentExIp;

                                this.ExIpDetetected = true;
                                return;
                            }
                    }
            }
            catch (Exception z)
            {
                textBoxIP.Text = "No internet";
            }
        }

        public string CurrentExIp = "";//{ get; set; }
        public bool ExIpDetetected = false;

        private void FormIps_Load(object sender, EventArgs e)
        {
            this.numericUpDown1.Value = port;
            ResetIp();
            ResetIpLoc();
            linkLabel1_LinkClicked(null, null);
        }

        private void ResetIpLoc()
        {
            richTextBox1.Text = "";
            foreach(var p in this.locips)
                richTextBox1.Text += (space(p.Item2)+" - "+p.Item1+"\r\n");
        }

        private string space(string p)
        {
            string x = "";
            for(int i =0;i<(15-p.Length);i++)
            {
                x += " ";
            }
            return p +" "+ x;
        }
        public List<Tuple<string, string>> locips = new List<Tuple<string, string>>();

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            labelPortStatue.Text = "";
            labelPortStatue.Text= DrNetwork.IsPortForwarded(textBoxIP.Text,numericUpDown1.Value);

        }

        public decimal port =80;

        private void textBoxIP_TextChanged(object sender, EventArgs e)
        {
            linkLabelcopyip.Visible = textBoxIP.Text.Length > 2 && textBoxIP.Text != "No internet";
        }
    }
}
