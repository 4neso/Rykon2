using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace RykonServer
{
    class DrNetwork
    {
        internal static List<Tuple<string, string>> GetAllIPv4Addresses()
        {
            List<Tuple<string, string>> ipList = new List<Tuple<string, string>>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = ua.Address.ToString();
                        if (ip.StartsWith("127.0.0")) continue;
                        ipList.Add(Tuple.Create(ni.Name,ip));
                    }
                }
            }
            return ipList;
        }
        private string GetIPv4Address()
        {
            string IP4Address = "";

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }

            return IP4Address;
        }

        internal static string IsPortForwarded(string ip , decimal p)
        {

            string port = p.ToString();
            string url = "http://canyouseeme.org";
            var request = (HttpWebRequest)WebRequest.Create(url);
            string Text = "Fail";
            try
            {
                var postData = "port=" + port;
                postData += "&IP=" + ip;
                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Referer = "http://canyouseeme.org/";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:39.0)";
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                string[] spd = responseString.Split(new string[] { "<p" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in spd)
                    if (s.Contains("style=\"padding-left:15px\">"))
                    {
                        if (s.Contains("<form"))
                        {
                            string[] xx = s.Split(new string[] { "<form" }, StringSplitOptions.RemoveEmptyEntries);
                            Text = AnalyzPortOpenResult(xx[0]);
                        }
                        else
                            Text = s;
                        break;
                    }
            }
            catch (Exception ph)
            {

            }
            return Text;

        }

        private static string  AnalyzPortOpenResult(string p)
        {
            if (p.Contains("Error"))
                return "failed";
            else if (p.Contains("uccess"))
                return "Success";
            else
                return "Error";
        }
    }
}
