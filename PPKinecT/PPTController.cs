using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace PPKinecT
{
    class PPTController
    {
        #region private variables

        private DispatcherTimer timer;

        /// <summary>
        /// Mouse control constants
        /// </summary>
        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;

        /// <summary>
        /// Screen size
        /// </summary>
        private readonly int screenWidth = 1024;
		private readonly int screenHeight = 768;
        private static Size screenBufferSize;

        /// <summary>
        /// Zoom level
        /// </summary>
        private int zoomLevel = -1;
        private const int MaxLevel = 4;

        /// <summary>
        /// Zoom sizes
        /// </summary>
        private readonly Size[] zoomSizes;

        /// <summary>
        /// Window to show magnified contents
        /// </summary>
        private MagnifyWindow magnifyWindow = new MagnifyWindow();

        /// <summary>
        /// Point that is the left top point of magnify area
        /// </summary>
        private Point magnifyPoint = new Point(0, 0);

        private int markRadius = 25;
        private int markReserveRadius = 50;

        public int markLeftReserve,
            markUpReserve,
            markRightReserve,
            markDownReserve;

        /// <summary>
        /// Mark reserve rect
        /// </summary>
        private Rectangle markRect;

        /// <summary>
        /// The image on screen which is used to erase marker
        /// </summary>
        private Image markReserve = null;

        private Image markBuffer = null;

        #endregion private variables

        #region windows API
        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", EntryPoint = "GetDCEx", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, int flags);
        #endregion windows API

        #region PPT page moves

        /// <summary>
        /// PPT scroll to next page.
        /// </summary>
        public void ToNextPage()
        {
            if (!presentCooling)
            {
                System.Windows.Forms.SendKeys.SendWait("{Right}");
                presentCooling = true;
                timer.Start();
            }
        }

        /// <summary>
        /// PPT scroll to previous page.
        /// </summary>
        public void ToPreviousPage()
        {
            if (!presentCooling)
            {
                System.Windows.Forms.SendKeys.SendWait("{Left}");
                presentCooling = true;
                timer.Start();
            }
        }

        #endregion PPT page moves

        #region mark on screen

        /// <summary>
        /// Show mark on screen
        /// </summary>
        /// <param name="x">x position on screen</param>
        /// <param name="y">y position on screen</param>
        public void ShowMark(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr deskDC = GetDCEx(desk, IntPtr.Zero, 0x403);
            Graphics g = Graphics.FromHdc(deskDC);

            Graphics bufferG = Graphics.FromImage(markBuffer);
            if (markReserve == null)
            {
                // first time to draw screen
                markReserve = new Bitmap(markRect.Width, markRect.Height);
                Graphics graphics = Graphics.FromImage(markReserve);
                graphics.CopyFromScreen(x, y, 0, 0, markRect.Size);
                graphics.Dispose();
                bufferG.FillPie(Brushes.Red, new Rectangle(x, y, markRadius, markRadius), 0, 360);
                g.DrawImage(markBuffer, 0, 0);
            }
            else
            {
                markReserve = new Bitmap(markRect.Width, markRect.Height);
                Graphics graphics = Graphics.FromImage(markReserve);
                //graphics.CopyFromScreen(x, y, 0, 0, markRect.Size);
                //graphics.DrawImage(markBuffer, x - markRect.X + markLeftReserve, y - markRect.Y + markUpReserve, markRect.Width, markRect.Height);
                //markBuffer.Save("buffer.jpg");
                graphics.DrawImage(markBuffer, 0, 0, new Rectangle(x - markRect.X + markLeftReserve, y - markRect.Y + markUpReserve, markBuffer.Width, markBuffer.Height), GraphicsUnit.Pixel);
                graphics.Dispose();
                //markReserve.Save("reserve.jpg");
                bufferG.FillPie(Brushes.Red, new Rectangle(x - markRect.X + markLeftReserve,
                    y - markRect.Y + markUpReserve, markRadius, markRadius), 0, 360);
                g.DrawImage(markBuffer, markRect.X - markLeftReserve, markRect.Y - markUpReserve);
            }
            markRect.X = x;
            markRect.Y = y;

            bufferG.Dispose();
            g.Dispose();
            //IntPtr desk = GetDesktopWindow();
            //IntPtr deskDC = GetDCEx(desk, IntPtr.Zero, 0x403);
            //Graphics g = Graphics.FromHdc(deskDC);
            //markReserve = new Bitmap(markRect.Width, markRect.Height);
            //Graphics graphics = Graphics.FromImage(markReserve);
            //graphics.CopyFromScreen(x, y, 0, 0, markRect.Size);
            

            //Graphics bufferG = Graphics.FromImage(markBuffer);
            //bufferG.FillPie(Brushes.Red, new Rectangle(x - markRect.X + markReserveRadius, y - markRect.Y + markReserveRadius, markRadius, markRadius), 0, 360);

            //g.DrawImage(markBuffer, markRect.X - markReserveRadius, markRect.Y - markReserveRadius);

            //markRect.X = x;
            //markRect.Y = y;

            //bufferG.Dispose();
            //graphics.Dispose();
            //g.Dispose();
        }

        /// <summary>
        /// Clear mark on screen
        /// </summary>
        public void RemoveMark()
        {
            if (markBuffer == null)
            {
                // first time buffer the whole screen
                markBuffer = new Bitmap(screenWidth, screenHeight);
                Graphics bufferG = Graphics.FromImage(markBuffer);
                bufferG.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));
                bufferG.Dispose();
            }
            else
            {
                if (markRect.X < markReserveRadius)
                {
                    markLeftReserve = markRect.X;
                    markRightReserve = markReserveRadius;
                }
                else if (markRect.X + markRadius > screenWidth - markReserveRadius)
                {
                    markRightReserve = screenWidth - markRect.X;
                    markLeftReserve = markReserveRadius;
                }
                else
                {
                    markRightReserve = markLeftReserve = markReserveRadius;
                }
                if (markRect.Y < markReserveRadius)
                {
                    markUpReserve = markRect.Y;
                    markDownReserve = markReserveRadius;
                }
                else if (markRect.Y + markRadius > screenHeight - markReserveRadius)
                {
                    markDownReserve = screenHeight - markRect.Y;
                    markLeftReserve = markReserveRadius;
                }
                else
                {
                    markRightReserve = markLeftReserve = markReserveRadius;
                }
                screenBufferSize.Height = markUpReserve + markDownReserve + markRadius;
                screenBufferSize.Width = markLeftReserve + markRightReserve + markRadius;
                markBuffer = new Bitmap(screenBufferSize.Width, screenBufferSize.Height);
                Graphics bufferG = Graphics.FromImage(markBuffer);
                bufferG.CopyFromScreen(markRect.X - markLeftReserve, markRect.Y - markUpReserve, 0, 0, screenBufferSize);
                //markBuffer.Save("Buffer2.jpg");
                if (markReserve != null)
                    bufferG.DrawImage(markReserve, markLeftReserve, markUpReserve);
                //markBuffer.Save("buffer1.jpg");
                bufferG.Dispose();
            }
            //markBuffer = new Bitmap(screenBufferSize.Width, screenBufferSize.Height);
            //Graphics graphics = Graphics.FromImage(markBuffer);
            //graphics.CopyFromScreen(0, 0, markRect.X - markReserveRadius, markRect.Y - markReserveRadius, screenBufferSize);
            ////IntPtr desk = GetDesktopWindow();
            ////IntPtr deskDC = GetDCEx(desk, IntPtr.Zero, 0x403);
            ////Graphics graphics = Graphics.FromHdc(deskDC);
            //if (markReserve != null)
            //    graphics.DrawImage(markReserve, markReserveRadius, markReserveRadius);
            ////markBuffer.Save("removeMark.jpg");
            //graphics.Dispose();
        }

        #endregion mark on screen

        #region screen zoom

        /// <summary>
        /// Whether the screen is zommed now
        /// </summary>
        /// <returns>true means zoomed</returns>
        public bool isZoomed()
        {
            return zoomLevel != -1;
        }

        public void ZoomIn()
        {
            if (zoomLevel == -1)
                magnifyWindow.CreateScreenCapture();
            if (zoomLevel < MaxLevel)
            {
                zoomLevel++;
                magnifyWindow.DisplayRect = new Rectangle(magnifyPoint, zoomSizes[zoomLevel]);
                magnifyWindow.Show();
                magnifyWindow.Activate();
                magnifyWindow.reDraw();
            }
        }

        public void ZoomOut()
        {
            if (zoomLevel >= 0)
            {
                zoomLevel--;
                if (zoomLevel == -1)
                    magnifyWindow.Hide();
                else
                {
                    magnifyWindow.DisplayRect = new Rectangle(magnifyPoint, zoomSizes[zoomLevel]);
                    magnifyWindow.Show();
                    magnifyWindow.reDraw();
                }
            }
        }

        /// <summary>
        /// Move zoomed screen
        /// </summary>
        /// <param name="xDelta">delta value of x, move left if x lt 0, right if x gt 0</param>
        /// <param name="yDelta">delta value of y, move up if y lt 0, down if y gt 0</param>
        public void MoveScreen(int xDelta, int yDelta)
        {
            if (!(zoomLevel > -1 && zoomLevel <= MaxLevel))
                return;
            magnifyPoint.X += xDelta;
            if (magnifyPoint.X + xDelta + zoomSizes[zoomLevel].Width > screenWidth)
                magnifyPoint.X = screenWidth - zoomSizes[zoomLevel].Width;
            if (magnifyPoint.X + xDelta < 0)
                magnifyPoint.X = 0;

            magnifyPoint.Y += yDelta;
            if (magnifyPoint.Y + yDelta + zoomSizes[zoomLevel].Height > screenHeight)
                magnifyPoint.Y = screenHeight - zoomSizes[zoomLevel].Height;
            if (magnifyPoint.Y + yDelta < 0)
                magnifyPoint.Y = 0;

            magnifyWindow.DisplayRect = new Rectangle(magnifyPoint, zoomSizes[zoomLevel]);
            magnifyWindow.reDraw();
        }

        #endregion screen zoom

        #region mouse click simulation

        /// <summary>
        /// Simulate a click on the point (x, y).
        /// Used in ppt hyperlinks
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Click(int x, int y)
        {
            SetCursorPos(x, y);
            System.Console.WriteLine("按下鼠标左键 X:{0}, Y:{1}", x, y);
            // left mouse down
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            Thread.Sleep(500);
            System.Console.WriteLine("释放鼠标左键 X:{0}, Y:{1}", x, y);
            // left mouse up
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }

        #endregion mouse click simulation

        public PPTController()
        {
            this.screenWidth = Screen.PrimaryScreen.Bounds.Width;
            this.screenHeight = Screen.PrimaryScreen.Bounds.Height;
            markRect = new Rectangle(500, 500, markRadius, markRadius);
            screenBufferSize = new Size(2 * markReserveRadius + markRadius, 2 * markReserveRadius + markRadius);
            zoomSizes = new Size[] {new Size(screenWidth / 10 * 8, screenHeight / 10 * 8),
                                    new Size(screenWidth / 10 * 6, screenHeight / 10 * 6),
                                    new Size(screenWidth / 10 * 5, screenHeight / 10 * 5),
                                    new Size(screenWidth / 10 * 3, screenHeight / 10 * 4),
                                    new Size(screenWidth / 10 * 3, screenHeight / 10 * 3),};

            timer  = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(5000);
            timer.Tick += PresentTimeOut;
            presentCooling = false;
        }

        private bool presentCooling;
        private void PresentTimeOut(object source, EventArgs e)
        {
            presentCooling = false;
            timer.Stop();
        }
    }
}
