/*
 *  Mudspike Screen Saver
 *  https://opensource.org/licenses/MIT
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;

namespace MudspikeScreenSaver
{
    public partial class ScreenSaverForm : Form
    {
        #region Win32 API functions

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        #endregion


        private Point mouseLocation;
        private bool previewMode = false;        
        private MudspikeImageFinder imageFinder;
        private MudspikeImageFinderResult currentResult;
        private string searchSetting;

        public ScreenSaverForm()
        {
            InitializeComponent();
        }

        public ScreenSaverForm(Rectangle Bounds)
        {
            InitializeComponent();
            this.Bounds = Bounds;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Mudspike_ScreenSaver");
            if (key == null)
                searchSetting = "screens";
            else
                searchSetting = (string)key.GetValue("text");
        }

        public ScreenSaverForm(IntPtr PreviewWndHandle)
        {
            InitializeComponent();

            // Set the preview window as the parent of this window
            SetParent(this.Handle, PreviewWndHandle);

            // Make this a child window so it will close when the parent dialog closes
            SetWindowLong(this.Handle, -16, new IntPtr(GetWindowLong(this.Handle, -16) | 0x40000000));

            // Place our window inside the parent
            Rectangle ParentRect;
            GetClientRect(PreviewWndHandle, out ParentRect);
            Size = ParentRect.Size;
            Location = new Point(0, 0);

            // Make text smaller
            textLabel.Font = new System.Drawing.Font("Stencil", 6);

            previewMode = true;
        }

        private void ScreenSaverForm_Load(object sender, EventArgs e)
        {            
            LoadSettings();

            Cursor.Hide();            
            TopMost = true;

            // Helper for comms and finding
            imageFinder = new MudspikeImageFinder();
            currentResult = imageFinder.DefaultImage();

            // The logo as first thing to show
            pictureBox1.Load(currentResult.url);
            
            textLabel.Parent= pictureBox1; // for transparency of background
            textLabel.Text = $"{currentResult.author} - {currentResult.topic_slug} {currentResult.created_at.Split('T')[0]}";

            moveTimer.Interval = 20000; // 20 secs not to hammer poor forum boxen
            moveTimer.Tick += new EventHandler(moveTimer_Tick);
            moveTimer.Start();

            // Why wait, lets go..
            moveTimer_Tick(null, null);
        }

        private void moveTimer_Tick(object sender, System.EventArgs e)
        {
            // Where the magic happens
            currentResult = imageFinder.FindCoolImage(searchSetting);

            textLabel.Text = ""; // Takes some time to load off thread, so blank it while we wait

            // Display it if we can
            pictureBox1.Load(currentResult.url);

            // Set textlabel to post author and topic, with enter to open in browser?
            textLabel.Text = $"{currentResult.author} - {currentResult.topic_slug} {currentResult.created_at.Split('T')[0]}";

        }

        private void LoadSettings()
        {
            // Use the string from the Registry if it exists
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Mudspike_ScreenSaver");
            if (key == null)
                textLabel.Text = "Screens";
            else
                textLabel.Text = (string)key.GetValue("text");
        }

        private void ScreenSaverForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!previewMode)
            {
                if (!mouseLocation.IsEmpty)
                {
                    // Terminate if mouse is moved a significant distance
                    if (Math.Abs(mouseLocation.X - e.X) > 5 ||
                        Math.Abs(mouseLocation.Y - e.Y) > 5)
                        Application.Exit();
                }

                // Update current mouse location
                mouseLocation = e.Location;
            }
        }

        private void ScreenSaverForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Open the shown image in the browser if space is pressed
            if (e.KeyChar == ' ')
            {
                Process.Start($"https://forums.mudspike.com/t/{currentResult.topic_slug}");
                Application.Exit();
            }
            if (!previewMode)
                Application.Exit();
        }

        private void ScreenSaverForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (!previewMode)
                Application.Exit();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            ScreenSaverForm_MouseMove(sender, e);
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            ScreenSaverForm_MouseClick(sender, e);
        }
    }
}
