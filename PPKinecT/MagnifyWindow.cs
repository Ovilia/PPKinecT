using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScreenMagnifier;

namespace PPKinecT
{
    public partial class MagnifyWindow : Form
    {
        #region 私有常量

		private readonly int m_ScreenWidth = 1024;

		private readonly int m_ScreenHeight = 768;

		#endregion 私有常量

		#region 私有变量

		/// <summary>
		/// 用于存在屏幕捕获位图
		/// </summary>
		private Bitmap m_ScreenCapture;

		/// <summary>
		/// 屏幕捕获点X坐标
		/// </summary>
		private int m_CaptureX = 0;

		/// <summary>
		/// 屏幕捕获点Y坐标
		/// </summary>
		private int m_CaptureY = 0;

		/// <summary>
		/// 锁定对象,用于加锁
		/// </summary>
		private object m_LockObj = new object();

		#endregion 私有变量

		#region 私有方法

		/// <summary>
		/// 手动释放资源
		/// </summary>
		private void CustomDispose()
		{
			this.m_ScreenCapture.Dispose();
		}

		#endregion 私有方法

		#region 控件事件

		/// <summary>
		/// 窗体绘制
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form_Paint ( object sender, PaintEventArgs e )
		{
			lock ( this.m_LockObj )
			{
                m_ScreenCapture.Save("test.jpg");
				e.Graphics.DrawImage( this.m_ScreenCapture, new Rectangle( 0, 0, this.Width, this.Height ), DisplayRect, GraphicsUnit.Pixel );
			}
            //ControlPaint.DrawBorder( e.Graphics, new Rectangle( 0, 0, this.Width, this.Height ), Color.Black, ButtonBorderStyle.Solid );
		}

		#endregion 控件事件

        #region 公有变量

        public Rectangle DisplayRect { set; get; }

        #endregion 公有变量

        #region 公有方法

        /// <summary>
        /// 捕获屏幕图像到位图
        /// </summary>
        public void CreateScreenCapture()
        {
            lock (this.m_LockObj)
            {
                using (Graphics g = Graphics.FromImage(this.m_ScreenCapture))
                {
                    g.CopyFromScreen(this.m_CaptureX, this.m_CaptureY, 0, 0, new Size(m_ScreenWidth, m_ScreenHeight));
                }
            }
            //reDraw();
        }

        public void reDraw()
        {
            if (this.InvokeRequired)
            {
                VoidCallback InvalidateCallback = new VoidCallback(this.Invalidate);
                this.Invoke(InvalidateCallback, null);
            }
            else
                this.Invalidate();
        }

        #endregion 公有方法

        public MagnifyWindow()
        {
            InitializeComponent();
            this.TopMost = true;
            this.m_ScreenWidth = Screen.PrimaryScreen.Bounds.Width;
            this.m_ScreenHeight = Screen.PrimaryScreen.Bounds.Height;

            m_ScreenCapture = new Bitmap(m_ScreenWidth, m_ScreenHeight);

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}
