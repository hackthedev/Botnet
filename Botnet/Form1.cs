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

        /// <summary>
        /// Trying to avoid some domains like google because it adds unnecessary time to the scan 
        /// and other tests like SSH testing and Bruteforce
        /// </summary>
        string[] blacklistedDomains = new string[] { "google", "youtube", "microsoft", "facebook", "gstatic", "w3" };
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ActiveControl = textbox_search;
            textbox_search.Focus();
        }

        /// <summary>
        /// Used to write (append) text into the output TextBox from another Task.
        /// </summary>
        /// <param name="text">
        /// A String that will be appended to the TextBox's Text
        /// </param>
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
            catch (Exception ex)
            { 
                // nothing yet
            }
        }

        /// <summary>
        /// Used to change the Label Text from another Task
        /// </summary>
        /// <param name="text">
        /// Text that should be displayed as label
        /// </param>
        public void setStatus(string text)
        {
            if (label_status.InvokeRequired)
            {
                label_status.Invoke(new Action<string>(setStatus), new object[] { text });
                return;
            }
            label_status.Text = text;
        }

        /// <summary>
        /// Adds an IP Address to the IP Address List if its not already listed.
        /// This is where I plan to add the SSH Check before adding the ip
        /// </summary>
        /// <param name="text"></param>
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

        /// <summary>
        /// Used to change the labels in the Statistics GroupBox
        /// </summary>
        /// <param name="text">
        /// Apparently unused lol
        /// </param>
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

        /// <summary>
        /// This is basically making a google search and going through all the listed urls on this page
        /// and through all the urls linked in the html code
        /// </summary>
        /// <param name="url"></param>
        private void searchIndex(string url)
        {
            try
            {
                // Load and Download Webpage content
                string pageContent = null;
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse myres = (HttpWebResponse)myReq.GetResponse();

                using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
                {
                    pageContent = sr.ReadToEnd();
                }

                // Search for IP Addresses
                var match = Regex.Match(pageContent, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                if (match.Success)
                {
                    // Add to Output Box
                    write("Found IP: " + match.Captures[0]);

                    // Add IP to IP ListBox
                    addIp(match.Captures[0].ToString());
                }

                // Search Page Content for other urls and for each url found load its content and redo procedure 
                foreach (Match item in Regex.Matches(pageContent, @"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
                {
                    // Get Host
                    Uri myUri = new Uri(item.Value);
                    string host = myUri.Host;                    

                    // Check if Host is blacklisted
                    foreach(string s in blacklistedDomains)
                    {
                        if (host.Contains(s))
                        {
                            // Blacklisted
                        }
                        else
                        {
                            // Add To output
                            write(" ");
                            write("=========[ " + host + " ]=========");

                            write("url: " + item.Value);

                            // Search the referred website's content as well
                            scanSites(item.Value);

                            // Update analytics
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
                        // On 404 Error 
                        write("404 Not Found");
                        NotFoundError++;
                    }
                    else
                    {
                        // Any other error. Will be added
                    }
                }
                else
                {
                    
                }

                write("====================================");
            }

            // Complete End of Search.

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

        /// <summary>
        /// Used to scan sub-sites found on the google search page
        /// </summary>
        /// <param name="url">
        /// website to scan its content.
        /// </param>
        private void scanSites(string url)
        {
            try
            {
                SitesScanned++;

                // Get host
                Uri myUri = new Uri(url);
                string host = myUri.Host;

                write(" ");
                write("=========[ " + host + " ]=========");

                // Load and get Website content
                string pageContent = null;
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse myres = (HttpWebResponse)myReq.GetResponse();

                using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
                {
                    pageContent = sr.ReadToEnd();
                }

                // Search for ips
                var match = Regex.Match(pageContent, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                if (match.Success)
                {
                    write("Found IP: " + match.Captures[0]);
                    addIp(match.Captures[0].ToString());
                }

                // Search for other weblinks
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

                            // Rescan all sub sites again
                            Task.Run(() => scanSubSites(item.Value));
                            updateStats();
                        }
                    }

                    
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        // On 404 Error
                        write("404 Not Found");
                        NotFoundError++;
                    }
                    else
                    {
                        // Implemented soon
                    }
                }
                else
                {
                    // Implemented soon
                }

                write("====================================");
            }

            updateStats();
        }

        /// <summary>
        /// Same as scanSites etc
        /// </summary>
        /// <param name="url">
        /// Site to scan
        /// </param>
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

            // Starts first scan of google search page
            Task.Run(() => searchIndex("https://www.google.com/search?q=" + textbox_search.Text));
        }

        /// <summary>
        /// This will be used to check if the host supports SSH. Important for later Bruteforce tests
        /// </summary>
        /// <param name="ip">
        /// IP Address of Target
        /// </param>
        /// <returns>
        /// Returns either true or false. Depends if Host supports SSH or not
        /// </returns>
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

        /// <summary>
        /// Some "cosmetics" when it comes to user friendly interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textbox_search_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
            }
        }
    }
}
