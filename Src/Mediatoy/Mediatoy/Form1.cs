using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mediatoy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        public WebView2 webView21 = new WebView2();
        private static int x, y, cx, cy;
        public static int vkCode, scanCode;
        public static bool KeyboardHookButtonDown, KeyboardHookButtonUp;
        public static List<string> links = new List<string>(), pictures = new List<string>();
        public static List<PictureBox> pictureboxes = new List<PictureBox>();
        public static PictureBox pbmargin = new PictureBox(); 
        public static string lastsource = "about:blank";
        public static bool started = false;
        private static string historicpath;
        public static int[] wd = { 2, 2, 2, 2 };
        public static int[] wu = { 2, 2, 2, 2 };
        public static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            x = 0;
            y = 0;
            cx = Screen.PrimaryScreen.Bounds.Width;
            cy = Screen.PrimaryScreen.Bounds.Height;
            this.Size = new Size(cx, cy);
            this.Location = new Point(x, y);
            this.BackgroundImage = Bitmap.FromFile("background.jpg");
            using (StreamReader file = new StreamReader("mediatoy.txt"))
            {
                while (true)
                {
                    string link = file.ReadLine();
                    string picture = file.ReadLine();
                    if (link == "" | picture == "")
                    {
                        file.Close();
                        break;
                    }
                    else
                    {
                        links.Add(link);
                        pictures.Add(picture);
                        pictureboxes.Add(new PictureBox());
                    }
                }
            }
            FillBox();
            SetStyle(); 
            AddStyle();
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-gpu --disable-gpu-compositing", "en");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment);
            webView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", "assets", CoreWebView2HostResourceAccessKind.DenyCors);
            historicpath = @"file:///" + System.Reflection.Assembly.GetEntryAssembly().Location.Replace("\\", "/").Replace("Mediatoy.exe", "") + "assets/historic.html";
            webView21.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            webView21.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView21.CoreWebView2.ContainsFullScreenElementChanged += (obj, args) =>
            {
                this.FullScreen = webView21.CoreWebView2.ContainsFullScreenElement;
            };
            webView21.Dock = DockStyle.Fill;
            webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webView21.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            webView21.NavigationCompleted += WebView21_NavigationCompleted;
            webView21.SourceChanged += WebView21_SourceChanged;
            webView21.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            webView21.KeyDown += WebView21_KeyDown;
            webView21.DefaultBackgroundColor = Color.Black;
            this.Controls.Add(webView21);
            webView21.Source = new Uri("about:blank");
            string stringinject = @"
                        window.location.href = 'about:blank';
                    ".Replace("\r\n", " ");
            execScriptHelper(stringinject);
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            OnKeyDown(keyData);
            return true;
        }
        private void WebView21_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        private void OnKeyDown(Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                const string message = "• Author: Michaël André Franiatte.\n\r\n\r• Contact: michael.franiatte@gmail.com.\n\r\n\r• Publisher: https://github.com/michaelandrefraniatte.\n\r\n\r• Copyrights: All rights reserved, no permissions granted.\n\r\n\r• License: Not open source, not free of charge to use.";
                const string caption = "About";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (keyData == Keys.Escape)
            {
                AddStyle();
            }
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            SetStyle();
        }
        private void WebView21_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (webView21.Source.ToString() == "about:blank")
            {
                this.webView21.Hide();
                this.Controls.Add(pbmargin);
                pbmargin.BringToFront();
                foreach (PictureBox picturebox in pictureboxes)
                {
                    this.Controls.Add(picturebox);
                    picturebox.BringToFront();
                }
            }
        }
        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            if (webView21.Source.ToString() != "about:blank" & webView21.Source.ToString() != historicpath)
            {
                lastsource = webView21.Source.ToString();
                WriteIntoFile("assets/historic.html", "<a style='color:white;' href='" + lastsource + "'>" + webView21.CoreWebView2.DocumentTitle + "</a><br />");
                started = true;
            }
        }
        private void WebView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
        }
        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            CoreWebView2HttpRequestHeaders requestHeaders = e.Request.Headers;
            requestHeaders.SetHeader("Access-Control-Max-Age", "0");
            requestHeaders.SetHeader("Cache-Control", "max-age=0, public");
            requestHeaders.SetHeader("Access-Control-Allow-Origin", "*");
            requestHeaders.SetHeader("Access-Control-Allow-Methods", "POST, GET, PUT, OPTIONS, DELETE");
            requestHeaders.SetHeader("Access-Control-Allow-Headers", "x-requested-with, content-type");
        }
        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            webView21.Source = new System.Uri(e.Uri);
        }
        private void CoreWebView2_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            IList<CoreWebView2ContextMenuItem> menuList = e.MenuItems;
            CoreWebView2ContextMenuItem newItem;
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Copy page Uri", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                string pageUri = e.ContextMenuTarget.PageUri;
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    Clipboard.SetText(pageUri);
                    System.Diagnostics.Process.Start(pageUri);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Go to menu", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    AddStyle();
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Go to Uri", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                this.TopMost = false;
                string newpageUri = Microsoft.VisualBasic.Interaction.InputBox("Prompt", "Enter a new page Uri", "https://google.com", 0, 0);
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    if (newpageUri != "")
                    {
                        Navigate(newpageUri);
                    }
                    this.TopMost = true;
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Back", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    string stringinject = @"
                        history.back();
                    ".Replace("\r\n", " ");
                    execScriptHelper(stringinject);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Forward", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    string stringinject = @"
                        history.forward();
                    ".Replace("\r\n", " ");
                    execScriptHelper(stringinject);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
            newItem = webView21.CoreWebView2.Environment.CreateContextMenuItem("Reload", null, CoreWebView2ContextMenuItemKind.Command);
            newItem.CustomItemSelected += delegate (object send, Object ex)
            {
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    string stringinject = @"
                        window.location.reload(false);
                    ".Replace("\r\n", " ");
                    execScriptHelper(stringinject);
                }, null);
            };
            menuList.Insert(menuList.Count, newItem);
        }
        private void Navigate(string address)
        {
            if (String.IsNullOrEmpty(address))
                return;
            if (address.Equals("about:blank"))
                return;
            if (!address.StartsWith("http://") & !address.StartsWith("https://"))
                address = "https://" + address;
            try
            {
                webView21.Source = new System.Uri(address);
            }
            catch (System.UriFormatException)
            {
                return;
            }
        }
        private bool fullScreen = false;
        [DefaultValue(false)]
        public bool FullScreen
        {
            get { return fullScreen; }
            set
            {
                fullScreen = value;
                if (value)
                {
                    this.WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    this.Activate();
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Normal;
                }
            }
        }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (webView21.Source.ToString().Contains("youtube.com") | webView21.Source.ToString().Contains("youtu.be"))
            {
                try
                {
                    string stringinject = @"
                        try {
                            document.cookie = 'VISITOR_INFO1_LIVE = oKckVSqvaGw; path =/; domain =.youtube.com';
                            var cookies = document.cookie.split('; ');
                            for (var i = 0; i < cookies.length; i++)
                            {
                                var cookie = cookies[i];
                                var eqPos = cookie.indexOf('=');
                                var name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
                                document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT';
                            }
                        }
                        catch { }
                        try {
                            var els = document.getElementsByClassName('video-ads ytp-ad-module');
                            for (var i=0;i<els.length; i++) {
                                els[i].click();
                            }
                        }
                        catch { }
                        try {
                            var el = document.getElementsByClassName('ytp-ad-skip-button');
                            for (var i=0;i<el.length; i++) {
                                el[i].click();
                            }
                        }
                        catch { }
                        try {
                            var elements = document.getElementsByClassName('ytp-ad-overlay-close-button');
                            for (var i=0;i<elements.length; i++) {
                                elements[i].click();
                            }
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-player-legacy-desktop-watch-ads-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var closeButton = document.querySelector('.ytp-ad-overlay-close-button');
                            if (closeButton) {
                                closeButton.click();
                            }
                        }
                        catch { }
                        try {
                            var playButton = document.querySelector('.ytp-large-play-button:visible');
                            if (playButton) {
                                playButton.click();
                            }
                        }
                        catch { }
                        try {
                            var skipButton = document.querySelectorAll('.ytp-ad-skip-button');
                            skipButton.forEach(elem => elem.click());
                            skipButton.forEach(elem => elem.style.zIndex = '10');
                        }
                        catch { }
                        try {
                            var skipButton = document.querySelectorAll('.ytp-ad-skip-button-modern');
                            skipButton.forEach(elem => elem.click());
                            skipButton.forEach(elem => elem.style.zIndex = '10');
                        }
                        catch { }
                        try {
                            var skipButton = document.getElementsByClassName('ytp-ad-skip-button');
                            skipButton.forEach(elem => elem.click());
                            skipButton.forEach(elem => elem.style.zIndex = '10');
                        }
                        catch { }
                        try {
                            var skipButton = document.getElementsByClassName('ytp-ad-skip-button-modern');
                            skipButton.forEach(elem => elem.click());
                            skipButton.forEach(elem => elem.style.zIndex = '10');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('.ad-container');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('.ytp-ad-overlay-open');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('.ytp-ad-overlay-image');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('.ytp-ad-overlay-container');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                    ";
                    await execScriptHelper(stringinject);
                }
                catch { }
            }
        }
        private async Task<String> execScriptHelper(String script)
        {
            var x = await webView21.ExecuteScriptAsync(script).ConfigureAwait(false);
            return x;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            webView21.Dispose();
        }
        private void RemoveStyle()
        {
            this.webView21.Show();
            this.Controls.Remove(pbmargin);
            foreach (PictureBox picturebox in pictureboxes)
            {
                this.Controls.Remove(picturebox);
            }
        }
        private void AddStyle()
        {
            this.webView21.Hide();
            if (started)
            {
                webView21.Source = new Uri("about:blank");
            }
            this.Controls.Add(pbmargin);
            pbmargin.BringToFront();
            foreach (PictureBox picturebox in pictureboxes)
            {
                this.Controls.Add(picturebox);
                picturebox.BringToFront();
            }
        }
        private void SetStyle()
        {
            bool justnewline = false;
            int lx = 0;
            int ly = 0;
            foreach (PictureBox picturebox in pictureboxes)
            {
                picturebox.Cursor = Cursors.Hand;
                picturebox.Size = new System.Drawing.Size(cx / 16, cy / 9);
                picturebox.BackColor = System.Drawing.Color.Transparent;
                picturebox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
                picturebox.Location = new Point((cx - picturebox.Size.Width * 8) * (lx + 1) / 9 + picturebox.Size.Width * lx, (cy - picturebox.Size.Height * 5) * (ly + 1) / 6 + picturebox.Size.Height * ly);
                Rectangle r = new Rectangle(0, 0, picturebox.Width, picturebox.Height);
                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                int d = 22;
                gp.AddArc(r.X, r.Y, d, d, 180, 90);
                gp.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
                gp.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
                gp.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
                picturebox.Region = new Region(gp);
                lx++;
                if (lx > 7) 
                {
                    lx = 0;
                    ly++;
                    justnewline = true;
                }
                else
                    justnewline = false;
            }
            lx = 0;
            if (!justnewline)
                ly++;
            pbmargin.Size = new System.Drawing.Size(cx / 16, cy / 9);
            pbmargin.BackColor = System.Drawing.Color.Transparent;
            pbmargin.Location = new Point((cx - pbmargin.Size.Width * 8) * (lx + 1) / 9 + pbmargin.Size.Width * lx, (cy - pbmargin.Size.Height * 5) * (ly + 1) / 6 + pbmargin.Size.Height * ly);
        }
        private void FillBox()
        {
            foreach (var (picturebox, index) in pictureboxes.WithIndex())
            {
                picturebox.Click += (sender, e) =>
                {
                    if (links[index] == "search")
                    {
                        this.TopMost = false;
                        string newpageUri = Microsoft.VisualBasic.Interaction.InputBox("Prompt", "Enter a new page Uri", "https://google.com", 0, 0);
                        System.Threading.SynchronizationContext.Current.Post((_) =>
                        {
                            if (newpageUri != "")
                            {
                                Navigate(newpageUri);
                                RemoveStyle();
                            }
                            this.TopMost = true;
                        }, null);
                    }
                    else if (links[index] != "back" | started)
                    {
                        webView21.Source = new Uri(links[index] == "back" ? lastsource : (links[index] == "historic" ? historicpath : links[index]));
                        RemoveStyle();
                    }
                };
                picturebox.MouseHover += (sender, e) =>
                {
                    ToolTip tooltip = new ToolTip();
                    tooltip.SetToolTip(picturebox, links[index]);
                };
                picturebox.BackgroundImage = Bitmap.FromFile(pictures[index]);
            }
        }
        private static void WriteIntoFile(string filename, string text)
        {
            string tempfile = Path.GetTempFileName();
            using (var writer = new FileStream(tempfile, FileMode.Create))
            using (var reader = new FileStream(filename, FileMode.Open))
            {
                var stringBytes = Encoding.UTF8.GetBytes(text + Environment.NewLine);
                writer.Write(stringBytes, 0, stringBytes.Length);
                reader.CopyTo(writer);
            }
            File.Copy(tempfile, filename, true);
            File.Delete(tempfile);
        }
    }
    public static class IEnumerableExtensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self) => self.Select((item, index) => (item, index));
    }
}