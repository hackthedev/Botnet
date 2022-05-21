using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Botnet
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region

        int NotFoundError = 0;
        int SitesScanned= 0;
        int IPsAdded = 0;

        bool searching = false;

        string[] blacklistedDomains = new string[] { "google", "youtube", "microsoft", "facebook", "gstatic", "w3" };
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ActiveControl = textbox_search;
            textbox_search.Focus();
        }

        public void write(string text)
        {
            if (textbox_log.InvokeRequired)
            {
                textbox_log.Invoke(new Action<string>(write), new object[] { text });
                return;
            }
            try 
            { 
                if (textbox_log.Text.Length > 9000) { textbox_log.Clear(); }
                textbox_log.AppendText(text + Environment.NewLine); 
            } 
            catch
            { 

            }
        }

        public void setStatus(string text)
        {
            if (label_status.InvokeRequired)
            {
                label_status.Invoke(new Action<string>(setStatus), new object[] { text });
                return;
            }
            label_status.Text = text;
        }

        public void addIp(string text)
        {
            bool found = false;
            if (textBox_ips.InvokeRequired)
            {
                textBox_ips.Invoke(new Action<string>(addIp), new object[] { text });
                return;
            }

            foreach (string line in textBox_ips.Lines)
            {
                if(line == text) { found = true; }
            }

            if (found == false) // && checkSSH(text) == true 
            {
                IPsAdded++;
                textBox_ips.AppendText(text + Environment.NewLine);

                // ip label counter
                if (label_ips.InvokeRequired)
                {
                    label_ips.Invoke(new Action<string>(addIp), new object[] { text });
                    return;
                }
                label_ips.Text = "IPs (" + IPsAdded.ToString() + ")";
            }
        }

        public void updateStats(string text = "")
        {
            // 404 error label
            if (label_404error.InvokeRequired)
            {
                label_404error.Invoke(new Action<string>(updateStats), new object[] { text });
                return;
            }
            label_404error.Text = NotFoundError.ToString();

            // sites scanend label
            if (label_scanned.InvokeRequired)
            {
                label_scanned.Invoke(new Action<string>(updateStats), new object[] { text });
                return;
            }
            label_scanned.Text = SitesScanned.ToString();            
        }

        private void searchIndex(string url)
        {
            try
            {
                string pageContent = null;
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse myres = (HttpWebResponse)myReq.GetResponse();

                using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
                {
                    pageContent = sr.ReadToEnd();
                }

                var match = Regex.Match(pageContent, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                if (match.Success)
                {
                    write("Found IP: " + match.Captures[0]);

                    addIp(match.Captures[0].ToString());
                }

                foreach (Match item in Regex.Matches(pageContent, @"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
                {
                    Uri myUri = new Uri(item.Value);
                    string host = myUri.Host;

                    

                    foreach(string s in blacklistedDomains)
                    {
                        if (host.Contains(s))
                        {

                        }
                        else
                        {
                            write(" ");
                            write("=========[ " + host + " ]=========");

                            write("url: " + item.Value);
                            scanSites(item.Value);
                            updateStats();
                        }
                    }
                }
            }
            catch(WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        write("404 Not Found");
                        NotFoundError++;
                    }
                    else
                    {
                        // Do something else
                    }
                }
                else
                {
                    
                }

                write("====================================");
            }
            updateStats();

            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            write("Done");
            write(" ");

            setStatus("Done");
            searching = false;
            //button1.Enabled = true;
        }

        private void scanSites(string url)
        {
            try
            {
                SitesScanned++;

                Uri myUri = new Uri(url);
                string host = myUri.Host;

                write(" ");
                write("=========[ " + host + " ]=========");


                string pageContent = null;
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse myres = (HttpWebResponse)myReq.GetResponse();

                using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
                {
                    pageContent = sr.ReadToEnd();
                }

                var match = Regex.Match(pageContent, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                if (match.Success)
                {
                    write("Found IP: " + match.Captures[0]);
                    addIp(match.Captures[0].ToString());
                }

                foreach (Match item in Regex.Matches(pageContent, @"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
                {

                    foreach (string s in blacklistedDomains)
                    {
                        if (host.Contains(s))
                        {

                        }
                        else
                        {
                            write("url: " + item.Value);


                            Task.Run(() => scanSubSites(item.Value));
                            updateStats();
                        }
                    }

                    
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError &&
        ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        write("404 Not Found");
                        NotFoundError++;
                    }
                    else
                    {
                        // Do something else
                    }
                }
                else
                {
                    // Do something else
                }

                write("====================================");
            }

            updateStats();
        }

        private void scanSubSites(string url)
        {
            try
            {
                SitesScanned++;

                Uri myUri = new Uri(url);
                string host = myUri.Host;

                write(" ");
                write("=========[ " + host + " ]=========");


                string pageContent = null;
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse myres = (HttpWebResponse)myReq.GetResponse();

                using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
                {
                    pageContent = sr.ReadToEnd();
                }

                var match = Regex.Match(pageContent, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                if (match.Success)
                {
                    write("Found IP: " + match.Captures[0]);
                    addIp(match.Captures[0].ToString());
                }

                updateStats();
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError &&
        ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        write("404 Not Found");
                        NotFoundError++;
                    }
                    else
                    {
                        // Do something else
                    }
                }
                else
                {
                    // Do something else
                }

                write("====================================");
            }

            updateStats();
        }



        private void button1_Click(object sender, EventArgs e)
        {
            label_status.Text = "Running";
            searching = true;
            //button1.Enabled = false;

            textbox_log.Clear();
            write("Search term: " + textbox_search.Text);
            write(" ");
            write(" ");
            write(" ");
            write(" ");
            Task.Run(() => searchIndex("https://www.google.com/search?q=" + textbox_search.Text));
        }

        private bool checkSSH(string ip)
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                tcpClient.Connect(ip, 22);
                write(ip + " is accepting SSH");
                return true;
            }
            catch (Exception ex)
            {
                write(ex.Message);
                return false;
            }
        }

        private void textbox_search_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
            }
        }
    }
}
