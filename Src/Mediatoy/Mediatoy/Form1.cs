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
using CSCore;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using WinformsVisualization.Visualization;

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
        private static bool f11switch = false;
        public int numBars = 100;
        public float[] barDataLeft = new float[100];
        public float[] barDataRight = new float[100];
        public int minFreq = 0;
        public int maxFreq = 23000;
        public int barSpacing = 0;
        public bool logScale = true;
        public bool isAverage = false;
        public float highScaleAverage = 1000f;
        public float highScaleNotAverage = 1000f;
        public LineSpectrum lineSpectrumLeft;
        public LineSpectrum lineSpectrumRight;
        public CSCore.SoundIn.WasapiCapture capture;
        public CSCore.DSP.FftSize fftSize;
        public float[] fftBuffer;
        public BasicSpectrumProvider spectrumProviderLeft;
        public BasicSpectrumProvider spectrumProviderRight;
        public CSCore.IWaveSource finalSource;
        public string backgroundcolor = "";
        public string frequencystickscolor = "";
        private bool youtubecookieson, spotifycookieson;
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
            if (!Directory.Exists("assets"))
                Directory.CreateDirectory("assets");
            if (!File.Exists(@"assets/historic.html"))
                File.Create(@"assets/historic.html");
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
            using (StreamReader file = new StreamReader("colors.txt"))
            {
                file.ReadLine();
                backgroundcolor = file.ReadLine();
                file.ReadLine();
                frequencystickscolor = file.ReadLine();
                file.Close();
            }
            youtubecookieson = File.Exists(Application.StartupPath + @"\Mediatoy.exe.WebView2\EBWebView\Default\IndexedDB\https_www.youtube.com_0.indexeddb.leveldb/LOG.old");
            spotifycookieson = File.Exists(Application.StartupPath + @"\Mediatoy.exe.WebView2\EBWebView\Default\IndexedDB\https_open.spotify.com_0.indexeddb.leveldb/LOG.old");
            Task.Run(() => GetAudioByteArray());
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
            if ((webView21.Source.ToString().Contains("youtube.com") | webView21.Source.ToString().Contains("youtu.be")) & youtubecookieson)
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
            capture.Stop();
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
        public void GetAudioByteArray()
        {
            capture = new CSCore.SoundIn.WasapiLoopbackCapture();
            capture.Initialize();
            CSCore.IWaveSource source = new CSCore.Streams.SoundInSource(capture);
            fftBuffer = new float[(int)CSCore.DSP.FftSize.Fft4096];
            spectrumProviderLeft = new BasicSpectrumProvider(capture.WaveFormat.Channels, capture.WaveFormat.SampleRate, CSCore.DSP.FftSize.Fft4096);
            lineSpectrumLeft = new LineSpectrum(CSCore.DSP.FftSize.Fft4096)
            {
                SpectrumProvider = spectrumProviderLeft,
                UseAverage = true,
                BarCount = numBars,
                BarSpacing = 2,
                IsXLogScale = false,
                ScalingStrategy = ScalingStrategy.Sqrt
            };
            spectrumProviderRight = new BasicSpectrumProvider(capture.WaveFormat.Channels, capture.WaveFormat.SampleRate, CSCore.DSP.FftSize.Fft4096);
            lineSpectrumRight = new LineSpectrum(CSCore.DSP.FftSize.Fft4096)
            {
                SpectrumProvider = spectrumProviderRight,
                UseAverage = true,
                BarCount = numBars,
                BarSpacing = 2,
                IsXLogScale = false,
                ScalingStrategy = ScalingStrategy.Sqrt
            };
            var notificationSource = new CSCore.Streams.SingleBlockNotificationStream(source.ToSampleSource());
            notificationSource.SingleBlockRead += NotificationSource_SingleBlockRead;
            finalSource = notificationSource.ToWaveSource();
            capture.DataAvailable += Capture_DataAvailable;
            capture.Start();
        }
        public void Capture_DataAvailable(object sender, CSCore.SoundIn.DataAvailableEventArgs e)
        {
            finalSource.Read(e.Data, e.Offset, e.ByteCount);
        }
        public void NotificationSource_SingleBlockRead(object sender, CSCore.Streams.SingleBlockReadEventArgs e)
        {
            spectrumProviderLeft.Add(e.Left, 0);
            spectrumProviderRight.Add(0, e.Right);
        }
        public float[] GetFFtDataLeft()
        {
            lock (barDataLeft)
            {
                lineSpectrumLeft.BarCount = numBars;
                if (numBars != barDataLeft.Length)
                {
                    barDataLeft = new float[numBars];
                }
            }
            if (spectrumProviderLeft.IsNewDataAvailable)
            {
                lineSpectrumLeft.MinimumFrequency = minFreq;
                lineSpectrumLeft.MaximumFrequency = maxFreq;
                lineSpectrumLeft.IsXLogScale = logScale;
                lineSpectrumLeft.BarSpacing = barSpacing;
                lineSpectrumLeft.SpectrumProvider.GetFftData(fftBuffer, this);
                return lineSpectrumLeft.GetSpectrumPoints(100.0f, fftBuffer);
            }
            else
            {
                return null;
            }
        }
        public float[] GetFFtDataRight()
        {
            lock (barDataRight)
            {
                lineSpectrumRight.BarCount = numBars;
                if (numBars != barDataRight.Length)
                {
                    barDataRight = new float[numBars];
                }
            }
            if (spectrumProviderRight.IsNewDataAvailable)
            {
                lineSpectrumRight.MinimumFrequency = minFreq;
                lineSpectrumRight.MaximumFrequency = maxFreq;
                lineSpectrumRight.IsXLogScale = logScale;
                lineSpectrumRight.BarSpacing = barSpacing;
                lineSpectrumRight.SpectrumProvider.GetFftData(fftBuffer, this);
                return lineSpectrumRight.GetSpectrumPoints(100.0f, fftBuffer);
            }
            else
            {
                return null;
            }
        }
        public void ComputeData()
        {
            try
            {
                float[] resData = GetFFtDataLeft();
                int numBars = barDataLeft.Length;
                if (resData == null)
                {
                    return;
                }
                lock (barDataLeft)
                {
                    for (int i = 0; i < numBars && i < resData.Length; i++)
                    {
                        barDataLeft[i] = resData[i] / 100.0f;
                        if (lineSpectrumLeft.UseAverage)
                        {
                            barDataLeft[i] = barDataLeft[i] + highScaleAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barDataLeft[i];
                        }
                        else
                        {
                            barDataLeft[i] = barDataLeft[i] + highScaleNotAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barDataLeft[i];
                        }
                    }
                }
            }
            catch { }
            try
            {
                float[] resData = GetFFtDataRight();
                int numBars = barDataRight.Length;
                if (resData == null)
                {
                    return;
                }
                lock (barDataRight)
                {
                    for (int i = 0; i < numBars && i < resData.Length; i++)
                    {
                        barDataRight[i] = resData[i] / 100.0f;
                        if (lineSpectrumRight.UseAverage)
                        {
                            barDataRight[i] = barDataRight[i] + highScaleAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barDataRight[i];
                        }
                        else
                        {
                            barDataRight[i] = barDataRight[i] + highScaleNotAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barDataRight[i];
                        }
                    }
                }
            }
            catch { }
        }
        private async void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                if (webView21.Source.ToString().Contains("spotify") & spotifycookieson)
                {
                    ComputeData();
                    string stringinject = @"
                        try {
                            var parentcanvas = document.getElementById('parentcanvas');
                            if (parentcanvas == null) {
                                parentcanvas = document.createElement('div');
                                document.getElementsByTagName('footer')[0].after(parentcanvas);
                                parentcanvas.id = 'parentcanvas';
                            }
                            parentcanvas.style.position = 'relative';
                            parentcanvas.style.display = 'inline-block';
                            parentcanvas.style.width = 'calc(100%)';
                            parentcanvas.style.height = '100px';
                            parentcanvas.style.left = '0px';
                            parentcanvas.style.top = '0px';
                            parentcanvas.style.backgroundColor = 'backgroundcolor';
                            var canvas = document.getElementsByTagName('canvas');
                            if (canvas.length == 0) {
                                canvas = document.createElement('canvas');
                                parentcanvas.append(canvas);
                                canvas.id = 'canvas';
                            }
                            canvas = document.getElementById('canvas');
                            canvas.width = parentcanvas.clientWidth;
                            canvas.height = parentcanvas.clientHeight;
                            var WIDTH = canvas.width;
                            var HEIGHT = canvas.height;
                            var ctx = canvas.getContext('2d');
                            ctx.fillStyle = 'backgroundcolor';
                            ctx.fillRect(0, 0, WIDTH, HEIGHT);
                            var audiorawdata = [rawdata100];
                            var barWidth = WIDTH / 199;
                            var barHeight = HEIGHT;
                            var x = 0;
                            for (var i = 1; i < 200; i += 1) {
                                barHeight = audiorawdata[i] / 2;
                                ctx.fillStyle = 'frequencystickscolor';
                                ctx.strokeStyle = 'frequencystickscolor';
                                ctx.fillRect(x, HEIGHT / 2 - barHeight / 2, barWidth, barHeight / 2 + 1);
                                ctx.fillRect(x, HEIGHT / 2 + barHeight / 2, barWidth, -barHeight / 2 - 1);
                                x += barWidth;
                            }
                            ctx.stroke();
                        }
                        catch {}
                    ";
                    await execScriptHelper(stringinject.Replace("backgroundcolor", backgroundcolor).Replace("frequencystickscolor", frequencystickscolor).Replace("rawdata100", (int)barDataLeft[0] + ", " + (int)barDataLeft[1] + ", " + (int)barDataLeft[2] + ", " + (int)barDataLeft[3] + ", " + (int)barDataLeft[4] + ", " + (int)barDataLeft[5] + ", " + (int)barDataLeft[6] + ", " + (int)barDataLeft[7] + ", " + (int)barDataLeft[8] + ", " + (int)barDataLeft[9] + ", " + (int)barDataLeft[10] + ", " + (int)barDataLeft[11] + ", " + (int)barDataLeft[12] + ", " + (int)barDataLeft[13] + ", " + (int)barDataLeft[14] + ", " + (int)barDataLeft[15] + ", " + (int)barDataLeft[16] + ", " + (int)barDataLeft[17] + ", " + (int)barDataLeft[18] + ", " + (int)barDataLeft[19] + ", " + (int)barDataLeft[20] + ", " + (int)barDataLeft[21] + ", " + (int)barDataLeft[22] + ", " + (int)barDataLeft[23] + ", " + (int)barDataLeft[24] + ", " + (int)barDataLeft[25] + ", " + (int)barDataLeft[26] + ", " + (int)barDataLeft[27] + ", " + (int)barDataLeft[28] + ", " + (int)barDataLeft[29] + ", " + (int)barDataLeft[30] + ", " + (int)barDataLeft[31] + ", " + (int)barDataLeft[32] + ", " + (int)barDataLeft[33] + ", " + (int)barDataLeft[34] + ", " + (int)barDataLeft[35] + ", " + (int)barDataLeft[36] + ", " + (int)barDataLeft[37] + ", " + (int)barDataLeft[38] + ", " + (int)barDataLeft[39] + ", " + (int)barDataLeft[40] + ", " + (int)barDataLeft[41] + ", " + (int)barDataLeft[42] + ", " + (int)barDataLeft[43] + ", " + (int)barDataLeft[44] + ", " + (int)barDataLeft[45] + ", " + (int)barDataLeft[46] + ", " + (int)barDataLeft[47] + ", " + (int)barDataLeft[48] + ", " + (int)barDataLeft[49] + ", " + (int)barDataLeft[50] + ", " + (int)barDataLeft[51] + ", " + (int)barDataLeft[52] + ", " + (int)barDataLeft[53] + ", " + (int)barDataLeft[54] + ", " + (int)barDataLeft[55] + ", " + (int)barDataLeft[56] + ", " + (int)barDataLeft[57] + ", " + (int)barDataLeft[58] + ", " + (int)barDataLeft[59] + ", " + (int)barDataLeft[60] + ", " + (int)barDataLeft[61] + ", " + (int)barDataLeft[62] + ", " + (int)barDataLeft[63] + ", " + (int)barDataLeft[64] + ", " + (int)barDataLeft[65] + ", " + (int)barDataLeft[66] + ", " + (int)barDataLeft[67] + ", " + (int)barDataLeft[68] + ", " + (int)barDataLeft[69] + ", " + (int)barDataLeft[70] + ", " + (int)barDataLeft[71] + ", " + (int)barDataLeft[72] + ", " + (int)barDataLeft[73] + ", " + (int)barDataLeft[74] + ", " + (int)barDataLeft[75] + ", " + (int)barDataLeft[76] + ", " + (int)barDataLeft[77] + ", " + (int)barDataLeft[78] + ", " + (int)barDataLeft[79] + ", " + (int)barDataLeft[80] + ", " + (int)barDataLeft[81] + ", " + (int)barDataLeft[82] + ", " + (int)barDataLeft[83] + ", " + (int)barDataLeft[84] + ", " + (int)barDataLeft[85] + ", " + (int)barDataLeft[86] + ", " + (int)barDataLeft[87] + ", " + (int)barDataLeft[88] + ", " + (int)barDataLeft[89] + ", " + (int)barDataLeft[90] + ", " + (int)barDataLeft[91] + ", " + (int)barDataLeft[92] + ", " + (int)barDataLeft[93] + ", " + (int)barDataLeft[94] + ", " + (int)barDataLeft[95] + ", " + (int)barDataLeft[96] + ", " + (int)barDataLeft[97] + ", " + (int)barDataLeft[98] + ", " + (int)barDataLeft[99] + ", " + (int)barDataRight[0] + ", " + (int)barDataRight[1] + ", " + (int)barDataRight[2] + ", " + (int)barDataRight[3] + ", " + (int)barDataRight[4] + ", " + (int)barDataRight[5] + ", " + (int)barDataRight[6] + ", " + (int)barDataRight[7] + ", " + (int)barDataRight[8] + ", " + (int)barDataRight[9] + ", " + (int)barDataRight[10] + ", " + (int)barDataRight[11] + ", " + (int)barDataRight[12] + ", " + (int)barDataRight[13] + ", " + (int)barDataRight[14] + ", " + (int)barDataRight[15] + ", " + (int)barDataRight[16] + ", " + (int)barDataRight[17] + ", " + (int)barDataRight[18] + ", " + (int)barDataRight[19] + ", " + (int)barDataRight[20] + ", " + (int)barDataRight[21] + ", " + (int)barDataRight[22] + ", " + (int)barDataRight[23] + ", " + (int)barDataRight[24] + ", " + (int)barDataRight[25] + ", " + (int)barDataRight[26] + ", " + (int)barDataRight[27] + ", " + (int)barDataRight[28] + ", " + (int)barDataRight[29] + ", " + (int)barDataRight[30] + ", " + (int)barDataRight[31] + ", " + (int)barDataRight[32] + ", " + (int)barDataRight[33] + ", " + (int)barDataRight[34] + ", " + (int)barDataRight[35] + ", " + (int)barDataRight[36] + ", " + (int)barDataRight[37] + ", " + (int)barDataRight[38] + ", " + (int)barDataRight[39] + ", " + (int)barDataRight[40] + ", " + (int)barDataRight[41] + ", " + (int)barDataRight[42] + ", " + (int)barDataRight[43] + ", " + (int)barDataRight[44] + ", " + (int)barDataRight[45] + ", " + (int)barDataRight[46] + ", " + (int)barDataRight[47] + ", " + (int)barDataRight[48] + ", " + (int)barDataRight[49] + ", " + (int)barDataRight[50] + ", " + (int)barDataRight[51] + ", " + (int)barDataRight[52] + ", " + (int)barDataRight[53] + ", " + (int)barDataRight[54] + ", " + (int)barDataRight[55] + ", " + (int)barDataRight[56] + ", " + (int)barDataRight[57] + ", " + (int)barDataRight[58] + ", " + (int)barDataRight[59] + ", " + (int)barDataRight[60] + ", " + (int)barDataRight[61] + ", " + (int)barDataRight[62] + ", " + (int)barDataRight[63] + ", " + (int)barDataRight[64] + ", " + (int)barDataRight[65] + ", " + (int)barDataRight[66] + ", " + (int)barDataRight[67] + ", " + (int)barDataRight[68] + ", " + (int)barDataRight[69] + ", " + (int)barDataRight[70] + ", " + (int)barDataRight[71] + ", " + (int)barDataRight[72] + ", " + (int)barDataRight[73] + ", " + (int)barDataRight[74] + ", " + (int)barDataRight[75] + ", " + (int)barDataRight[76] + ", " + (int)barDataRight[77] + ", " + (int)barDataRight[78] + ", " + (int)barDataRight[79] + ", " + (int)barDataRight[80] + ", " + (int)barDataRight[81] + ", " + (int)barDataRight[82] + ", " + (int)barDataRight[83] + ", " + (int)barDataRight[84] + ", " + (int)barDataRight[85] + ", " + (int)barDataRight[86] + ", " + (int)barDataRight[87] + ", " + (int)barDataRight[88] + ", " + (int)barDataRight[89] + ", " + (int)barDataRight[90] + ", " + (int)barDataRight[91] + ", " + (int)barDataRight[92] + ", " + (int)barDataRight[93] + ", " + (int)barDataRight[94] + ", " + (int)barDataRight[95] + ", " + (int)barDataRight[96] + ", " + (int)barDataRight[97] + ", " + (int)barDataRight[98] + ", " + (int)barDataRight[99]));
                }
            }
            catch { }
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
namespace WinformsVisualization.Visualization
{
    /// <summary>
    ///     BasicSpectrumProvider
    /// </summary>
    public class BasicSpectrumProvider : CSCore.DSP.FftProvider, ISpectrumProvider
    {
        public readonly int _sampleRate;
        public readonly List<object> _contexts = new List<object>();

        public BasicSpectrumProvider(int channels, int sampleRate, CSCore.DSP.FftSize fftSize)
            : base(channels, fftSize)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException("sampleRate");
            _sampleRate = sampleRate;
        }

        public int GetFftBandIndex(float frequency)
        {
            int fftSize = (int)FftSize;
            double f = _sampleRate / 2.0;
            // ReSharper disable once PossibleLossOfFraction
            return (int)((frequency / f) * (fftSize / 2));
        }

        public bool GetFftData(float[] fftResultBuffer, object context)
        {
            if (_contexts.Contains(context))
                return false;

            _contexts.Add(context);
            GetFftData(fftResultBuffer);
            return true;
        }

        public override void Add(float[] samples, int count)
        {
            base.Add(samples, count);
            if (count > 0)
                _contexts.Clear();
        }

        public override void Add(float left, float right)
        {
            base.Add(left, right);
            _contexts.Clear();
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public interface ISpectrumProvider
    {
        bool GetFftData(float[] fftBuffer, object context);
        int GetFftBandIndex(float frequency);
    }
}
namespace WinformsVisualization.Visualization
{
    internal class GradientCalculator
    {
        public Color[] _colors;

        public GradientCalculator()
        {
        }

        public GradientCalculator(params Color[] colors)
        {
            _colors = colors;
        }

        public Color[] Colors
        {
            get { return _colors ?? (_colors = new Color[] { }); }
            set { _colors = value; }
        }

        public Color GetColor(float perc)
        {
            if (_colors.Length > 1)
            {
                int index = Convert.ToInt32((_colors.Length - 1) * perc - 0.5f);
                float upperIntensity = (perc % (1f / (_colors.Length - 1))) * (_colors.Length - 1);
                if (index + 1 >= Colors.Length)
                    index = Colors.Length - 2;

                return Color.FromArgb(
                    255,
                    (byte)(_colors[index + 1].R * upperIntensity + _colors[index].R * (1f - upperIntensity)),
                    (byte)(_colors[index + 1].G * upperIntensity + _colors[index].G * (1f - upperIntensity)),
                    (byte)(_colors[index + 1].B * upperIntensity + _colors[index].B * (1f - upperIntensity)));
            }
            return _colors.FirstOrDefault();
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public class LineSpectrum : SpectrumBase
    {
        public int _barCount;
        public double _barSpacing;
        public double _barWidth;
        public Size _currentSize;

        public LineSpectrum(CSCore.DSP.FftSize fftSize)
        {
            FftSize = fftSize;
        }

        [Browsable(false)]
        public double BarWidth
        {
            get { return _barWidth; }
        }

        public double BarSpacing
        {
            get { return _barSpacing; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _barSpacing = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("BarSpacing");
                RaisePropertyChanged("BarWidth");
            }
        }

        public int BarCount
        {
            get { return _barCount; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");
                _barCount = value;
                SpectrumResolution = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("BarCount");
                RaisePropertyChanged("BarWidth");
            }
        }

        [BrowsableAttribute(false)]
        public Size CurrentSize
        {
            get { return _currentSize; }
            set
            {
                _currentSize = value;
                RaisePropertyChanged("CurrentSize");
            }
        }

        public Bitmap CreateSpectrumLine(Size size, Brush brush, Color background, bool highQuality)
        {
            if (!UpdateFrequencyMappingIfNessesary(size))
                return null;

            var fftBuffer = new float[(int)FftSize];

            //get the fft result from the spectrum provider
            if (SpectrumProvider.GetFftData(fftBuffer, this))
            {
                using (var pen = new Pen(brush, (float)_barWidth))
                {
                    var bitmap = new Bitmap(size.Width, size.Height);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        PrepareGraphics(graphics, highQuality);
                        graphics.Clear(background);

                        CreateSpectrumLineInternal(graphics, pen, fftBuffer, size);
                    }

                    return bitmap;
                }
            }
            return null;
        }

        public Bitmap CreateSpectrumLine(Size size, Color color1, Color color2, Color background, bool highQuality)
        {
            if (!UpdateFrequencyMappingIfNessesary(size))
                return null;

            using (
                Brush brush = new LinearGradientBrush(new RectangleF(0, 0, (float)_barWidth, size.Height), color2,
                    color1, LinearGradientMode.Vertical))
            {
                return CreateSpectrumLine(size, brush, background, highQuality);
            }
        }

        public void CreateSpectrumLineInternal(Graphics graphics, Pen pen, float[] fftBuffer, Size size)
        {
            int height = size.Height;
            //prepare the fft result for rendering 
            SpectrumPointData[] spectrumPoints = CalculateSpectrumPoints(height, fftBuffer);

            //connect the calculated points with lines
            for (int i = 0; i < spectrumPoints.Length; i++)
            {
                SpectrumPointData p = spectrumPoints[i];
                int barIndex = p.SpectrumPointIndex;
                double xCoord = BarSpacing * (barIndex + 1) + (_barWidth * barIndex) + _barWidth / 2;

                var p1 = new PointF((float)xCoord, height);
                var p2 = new PointF((float)xCoord, height - (float)p.Value - 1);

                graphics.DrawLine(pen, p1, p2);
            }
        }

        public override void UpdateFrequencyMapping()
        {
            _barWidth = Math.Max(((_currentSize.Width - (BarSpacing * (BarCount + 1))) / BarCount), 0.00001);
            base.UpdateFrequencyMapping();
        }

        public bool UpdateFrequencyMappingIfNessesary(Size newSize)
        {
            if (newSize != CurrentSize)
            {
                CurrentSize = newSize;
                UpdateFrequencyMapping();
            }

            return newSize.Width > 0 && newSize.Height > 0;
        }

        public void PrepareGraphics(Graphics graphics, bool highQuality)
        {
            if (highQuality)
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Default;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            }
            else
            {
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            }
        }
        public float[] GetSpectrumPoints(float height, float[] fftBuffer)
        {
            SpectrumPointData[] dats = CalculateSpectrumPoints(height, fftBuffer);
            float[] res = new float[dats.Length];
            for (int i = 0; i < dats.Length; i++)
            {
                res[i] = (float)dats[i].Value;
            }

            return res;
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public class SpectrumBase : INotifyPropertyChanged
    {
        public const int ScaleFactorLinear = 9;
        public const int ScaleFactorSqr = 2;
        public const double MinDbValue = -90;
        public const double MaxDbValue = 0;
        public const double DbScale = (MaxDbValue - MinDbValue);

        public int _fftSize;
        public bool _isXLogScale;
        public int _maxFftIndex;
        public int _maximumFrequency = 20000;
        public int _maximumFrequencyIndex;
        public int _minimumFrequency = 20; //Default spectrum from 20Hz to 20kHz
        public int _minimumFrequencyIndex;
        public ScalingStrategy _scalingStrategy;
        public int[] _spectrumIndexMax;
        public int[] _spectrumLogScaleIndexMax;
        public ISpectrumProvider _spectrumProvider;

        public int SpectrumResolution;
        public bool _useAverage;

        public int MaximumFrequency
        {
            get { return _maximumFrequency; }
            set
            {
                if (value <= MinimumFrequency)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "Value must not be less or equal the MinimumFrequency.");
                }
                _maximumFrequency = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("MaximumFrequency");
            }
        }

        public int MinimumFrequency
        {
            get { return _minimumFrequency; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _minimumFrequency = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("MinimumFrequency");
            }
        }

        [BrowsableAttribute(false)]
        public ISpectrumProvider SpectrumProvider
        {
            get { return _spectrumProvider; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _spectrumProvider = value;

                RaisePropertyChanged("SpectrumProvider");
            }
        }

        public bool IsXLogScale
        {
            get { return _isXLogScale; }
            set
            {
                _isXLogScale = value;
                UpdateFrequencyMapping();
                RaisePropertyChanged("IsXLogScale");
            }
        }

        public ScalingStrategy ScalingStrategy
        {
            get { return _scalingStrategy; }
            set
            {
                _scalingStrategy = value;
                RaisePropertyChanged("ScalingStrategy");
            }
        }

        public bool UseAverage
        {
            get { return _useAverage; }
            set
            {
                _useAverage = value;
                RaisePropertyChanged("UseAverage");
            }
        }

        [BrowsableAttribute(false)]
        public CSCore.DSP.FftSize FftSize
        {
            get { return (CSCore.DSP.FftSize)_fftSize; }
            set
            {
                if ((int)Math.Log((int)value, 2) % 1 != 0)
                    throw new ArgumentOutOfRangeException("value");

                _fftSize = (int)value;
                _maxFftIndex = _fftSize / 2 - 1;

                RaisePropertyChanged("FFTSize");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void UpdateFrequencyMapping()
        {
            _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MaximumFrequency) + 1, _maxFftIndex);
            _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MinimumFrequency), _maxFftIndex);

            int actualResolution = SpectrumResolution;

            int indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
            double linearIndexBucketSize = Math.Round(indexCount / (double)actualResolution, 3);

            _spectrumIndexMax = _spectrumIndexMax.CheckBuffer(actualResolution, true);
            _spectrumLogScaleIndexMax = _spectrumLogScaleIndexMax.CheckBuffer(actualResolution, true);

            double maxLog = Math.Log(actualResolution, actualResolution);
            for (int i = 1; i < actualResolution; i++)
            {
                int logIndex =
                    (int)((maxLog - Math.Log((actualResolution + 1) - i, (actualResolution + 1))) * indexCount) +
                    _minimumFrequencyIndex;

                _spectrumIndexMax[i - 1] = _minimumFrequencyIndex + (int)(i * linearIndexBucketSize);
                _spectrumLogScaleIndexMax[i - 1] = logIndex;
            }

            if (actualResolution > 0)
            {
                _spectrumIndexMax[_spectrumIndexMax.Length - 1] =
                    _spectrumLogScaleIndexMax[_spectrumLogScaleIndexMax.Length - 1] = _maximumFrequencyIndex;
            }
        }

        public virtual SpectrumPointData[] CalculateSpectrumPoints(double maxValue, float[] fftBuffer)
        {
            var dataPoints = new List<SpectrumPointData>();

            double value0 = 0, value = 0;
            double lastValue = 0;
            double actualMaxValue = maxValue;
            int spectrumPointIndex = 0;

            for (int i = _minimumFrequencyIndex; i <= _maximumFrequencyIndex; i++)
            {
                switch (ScalingStrategy)
                {
                    case ScalingStrategy.Decibel:
                        value0 = (((20 * Math.Log10(fftBuffer[i])) - MinDbValue) / DbScale) * actualMaxValue;
                        break;
                    case ScalingStrategy.Linear:
                        value0 = (fftBuffer[i] * ScaleFactorLinear) * actualMaxValue;
                        break;
                    case ScalingStrategy.Sqrt:
                        value0 = ((Math.Sqrt(fftBuffer[i])) * ScaleFactorSqr) * actualMaxValue;
                        break;
                }

                bool recalc = true;

                value = Math.Max(0, Math.Max(value0, value));

                while (spectrumPointIndex <= _spectrumIndexMax.Length - 1 &&
                       i ==
                       (IsXLogScale
                           ? _spectrumLogScaleIndexMax[spectrumPointIndex]
                           : _spectrumIndexMax[spectrumPointIndex]))
                {
                    if (!recalc)
                        value = lastValue;

                    if (value > maxValue)
                        value = maxValue;

                    if (_useAverage && spectrumPointIndex > 0)
                        value = (lastValue + value) / 2.0;

                    dataPoints.Add(new SpectrumPointData { SpectrumPointIndex = spectrumPointIndex, Value = value });

                    lastValue = value;
                    value = 0.0;
                    spectrumPointIndex++;
                    recalc = false;
                }

                //value = 0;
            }

            return dataPoints.ToArray();
        }

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !String.IsNullOrEmpty(propertyName))
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [DebuggerDisplay("{Value}")]
        public struct SpectrumPointData
        {
            public int SpectrumPointIndex;
            public double Value;
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public enum ScalingStrategy
    {
        Decibel,
        Linear,
        Sqrt
    }
}