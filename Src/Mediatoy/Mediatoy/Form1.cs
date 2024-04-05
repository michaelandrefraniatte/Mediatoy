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
using System.Text;
using Valuechanges;

namespace Mediatoy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool DrawMenuBar(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        private static string WINDOW_NAME = "";
        private const int GWL_STYLE = -16;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_TABSTOP = 0x00010000;
        private const uint WS_VISIBLE = 0x10000000;
        public static WebView2 webView21 = new WebView2();
        private static int x, y, cx, cy;
        public static int vkCode, scanCode;
        public static bool KeyboardHookButtonDown, KeyboardHookButtonUp;
        public static List<string> links = new List<string>(), pictures = new List<string>();
        public static List<PictureBox> pictureboxes = new List<PictureBox>();
        public static PictureBox pbmargin = new PictureBox(); 
        public static string lastsource = "";
        private static string historicpath;
        public static bool cutsound = false;
        private static IntPtr hwnd;
        public static Valuechange ValueChange = new Valuechange();
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
            webView21.CoreWebView2.AddHostObjectToScript("bridge", new Bridge());
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
            hwnd = this.Handle;
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
                webView21.Hide();
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
                WriteIntoFile("assets/historic.html", "<a style='color:white;' href='" + lastsource + "' title='" + lastsource + "'>" + webView21.CoreWebView2.DocumentTitle + "</a><br />");
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
                            var scripts = document.getElementsByTagName('script');
                            for (let i = 0; i < scripts.length; i++)
                            {
                                var content = scripts[i].innerHTML;
                                if (content.indexOf('ytp-ad') > -1) {
                                    scripts[i].innerHTML = '';
                                }
                                var src = scripts[i].getAttribute('src');
                                if (src.indexOf('ytp-ad') > -1) {
                                    scripts[i].setAttribute('src', '');
                                }
                            }
                        }
                        catch { }
                        try {
                            var iframes = document.getElementsByTagName('iframe');
                            for (let i = 0; i < iframes.length; i++)
                            {
                                var content = iframes[i].innerHTML;
                                if (content.indexOf('ytp-ad') > -1) {
                                    iframes[i].innerHTML = '';
                                }
                                var src = iframes[i].getAttribute('src');
                                if (src.indexOf('ytp-ad') > -1) {
                                    iframes[i].setAttribute('src', '');
                                }
                            }
                        }
                        catch { }
                        try {
                            var allelements = document.querySelectorAll('*');
                            for (var i = 0; i < allelements.length; i++) {
                                var classname = allelements[i].className;
                                if (classname.indexOf('ytp-ad') > -1 | classname.indexOf('-ad-') > -1 | classname.indexOf('ad-') > -1 | classname.indexOf('ads-') > -1 | classname.indexOf('ad-showing') > -1 | classname.indexOf('ad-container') > -1 | classname.indexOf('ytp-ad-overlay-open') > -1 | classname.indexOf('video-ads') > -1)  {
                                    allelements[i].innerHTML = '';
                                }
                            }
                        }
                        catch { }
                        try {
                            var players = document.getElementById('movie_player');
                            for (let i = 0; i < players.length; i++) {
                                players.classList.remove('ad-interrupting');
                                players.classList.remove('playing-mode');
                                players.classList.remove('ytp-autohide');
                                players.classList.add('ytp-hide-info-bar');
                                players.classList.add('playing-mode');
                                players.classList.add('ytp-autohide');
                            }
                        }
                        catch { }
                        try {
                            var fabelements = document.querySelectorAll('yt-reaction-control-panel-button-view-model');
                            for (var i = 0; i < fabelements.length; i++) {
                                    fabelements[i].innerHTML = '';
                            }
                        }
                        catch { }
                        try {
                            var fabelement = document.querySelector('#fab-container');
                            fabelement.innerHTML = '';
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-engagement-panel-section-list-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-player-legacy-desktop-watch-ads-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('yt-reaction-control-panel-button-view-model');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('tp-yt-paper-dialog');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-statement-banner-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-brand-video-singleton-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelector('#reaction-control-panel').style.display = 'none';
                        }
                        catch { }
                        try {
                            var contents = document.querySelector('#emoji-fountain').style.display = 'none';
                        }
                        catch { }
                        try {
                            var contents = document.querySelector('#fab-container').style.display = 'none';
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
                            var contents = document.querySelectorAll('.ad-showing');
                            contents.forEach(elem => elem.style.display = 'none');
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
                            var contents = document.querySelectorAll('.video-ads');
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
                        try {
                            var contents = document.querySelectorAll('.ytd-carousel-ad-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-ad-slot-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            var contents = document.querySelectorAll('ytd-action-companion-ad-renderer');
                            contents.forEach(elem => elem.style.display = 'none');
                        }
                        catch { }
                        try {
                            const bridge = chrome.webview.hostObjects.bridge;
                            var mute = document.querySelectorAll('.ad-showing');
                            if (mute.length > 0) {
                                bridge.CutSound('1');
                            }
                            else {
                                bridge.CutSound('0');
                            }
                        }
                        catch { }
                    ";
                    await execScriptHelper(stringinject);
                }
                catch { }
            }
        }
        private async static Task<String> execScriptHelper(String script)
        {
            var x = await webView21.ExecuteScriptAsync(script).ConfigureAwait(false);
            return x;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            webView21.Dispose();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (cutsound)
            {
                VolDown();
                VolUp();
            }
        }
        public static void Mute()
        {
            SendMessageW(hwnd, WM_APPCOMMAND, hwnd, (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }
        public static void VolDown()
        {
            SendMessageW(hwnd, WM_APPCOMMAND, hwnd, (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }
        public static void VolUp()
        {
            SendMessageW(hwnd, WM_APPCOMMAND, hwnd, (IntPtr)APPCOMMAND_VOLUME_UP);
        }
        public async static void CutSound(double param)
        {
            string stringinject = "";
            ValueChange[0] = param;
            if (Valuechange._ValueChange[0] > 0f)
            {
                cutsound = true;
                Mute();
                stringinject = @"
                        try {
                            var link = window.location.href;
                            link = link.split('&')[0];
                            var id = link.split('?v=')[1];
                            var player = document.getElementById('player');
                            if (player) {
                                player.style.backgroundImage = `url(\'` + 'https://i.ytimg.com/vi/' + id + '/hq720.jpg' + `\')`;
                                player.style.backgroundSize = 'cover';
                                player.style.backgroundRepeat = 'no-repeat';
                                player.style.backgroundPosition = 'center';
                            }
                        }
                        catch { }
                        try {
                            var button = document.querySelector('.ytp-ad-skip-button-modern');
                            var video = document.querySelector('#player');
                            video.after(button);
                        }
                        catch { }
                        try {
                            var button = document.querySelector('.ytp-ad-skip-button');
                            var video = document.querySelector('#player');
                            video.after(button);
                        }
                        catch { }
                        try {
                            var script = document.createElement('script');
                            script.src = 'https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js';
                            script.async = false;
                            var head = document.getElementsByTagName('head')[0];
                            head.appendChild(script);
                        }
                        catch { }
                        try {
                            (function() {
                                var adSkipButtonModern = setInterval(() => {
                                    try {
                                        $('.ytp-ad-skip-button-modern').trigger('click');
                                    }
                                    catch { }
                                }, 10000);
                                setTimeout(() => {
                                    clearInterval(adSkipButtonModern);
                                }, '120000');
                            })();
                        }
                        catch { }
                        try {
                            (function() {
                                var adSkipButton = setInterval(() => {
                                    try {
                                        $('.ytp-ad-skip-button').trigger('click');
                                    }
                                    catch { }
                                }, 10000);
                                setTimeout(() => {
                                    clearInterval(adSkipButton);
                                }, '120000');
                            })();
                        }
                        catch { }
                    ".Replace("\r\n", " ");
                execScriptHelper(stringinject);
            }
            if (Valuechange._ValueChange[0] < 0f)
            {
                cutsound = false;
                VolDown();
                VolUp();
                stringinject = @"
                        var player = document.getElementById('player');
                        if (player) {
                            player.style.backgroundImage = 'none';
                        }
                        var video = document.querySelector('.html5-video-player');
                        if (video) {
                            video.style.display = 'block';
                        }
                    ".Replace("\r\n", " ");
                execScriptHelper(stringinject);
            }
        }
        private void RemoveStyle()
        {
            webView21.Show();
            this.Controls.Remove(pbmargin);
            foreach (PictureBox picturebox in pictureboxes)
            {
                this.Controls.Remove(picturebox);
            }
        }
        private void AddStyle()
        {
            webView21.Hide();
            if (lastsource != "")
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
                int d = 50;
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
                    else if (links[index] != "back" | lastsource != "")
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
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class Bridge
    {
        public string CutSound(string param)
        {
            Form1.CutSound(Convert.ToSingle(param));
            return param;
        }
    }
}