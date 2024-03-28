using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;

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
        private static int width, height;
        private static bool f11switch = false;
        public WebView2 webView21 = new WebView2();
        private static int x, y, cx, cy;
        public static int vkCode, scanCode;
        public static bool KeyboardHookButtonDown, KeyboardHookButtonUp;
        public static bool starting = true;
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
            SetStyle();
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-gpu --disable-gpu-compositing", "en");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment);
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
            webView21.KeyDown += WebView21_KeyDown;
            webView21.DefaultBackgroundColor = Color.Black;
            this.Controls.Add(webView21);
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
                this.Close();
            }
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            SetStyle();
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
                string newpageUri = Microsoft.VisualBasic.Interaction.InputBox("Prompt", "Enter a new page Uri", "https://google.com", 0, 0);
                System.Threading.SynchronizationContext.Current.Post((_) =>
                {
                    if (newpageUri != "")
                        Navigate(newpageUri);
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
        private void pbmolotov_Click(object sender, EventArgs e)
        {
            webView21.Source = new Uri("https://app.molotov.tv/channels/");
            RemoveStyle();
        }
        private void pbcrunchyroll_Click(object sender, EventArgs e)
        {
            webView21.Source = new Uri("https://www.crunchyroll.com/fr/");
            RemoveStyle();
        }
        private void RemoveStyle()
        {
            this.webView21.Show();
            this.Controls.Remove(label1);
            this.Controls.Remove(pbmolotov);
            this.Controls.Remove(pbcrunchyroll);
        }
        private void AddStyle()
        {
            this.webView21.Hide();
            this.Controls.Add(label1);
            label1.BringToFront();
            this.Controls.Add(pbmolotov);
            pbmolotov.BringToFront();
            this.Controls.Add(pbcrunchyroll);
            pbcrunchyroll.BringToFront();
        }
        private void SetStyle()
        {
            pbmolotov.Cursor = Cursors.Hand;
            pbcrunchyroll.Cursor = Cursors.Hand;
            this.pbmolotov.Size = new System.Drawing.Size(cx / 16, cy / 9);
            this.pbcrunchyroll.Size = new System.Drawing.Size(cx / 16, cy / 9);
            this.pbmolotov.Location = new Point(cx / 50 + this.pbmolotov.Size.Width / 2, cy / 50 + this.pbmolotov.Size.Height / 2);
            this.pbcrunchyroll.Location = new Point(cx / 10 + this.pbcrunchyroll.Size.Width / 2, cy / 50 + this.pbcrunchyroll.Size.Height / 2);
        }
    }
}