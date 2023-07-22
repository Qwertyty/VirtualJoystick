using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace VirtualJoystick
{
    public enum dir { N, NE, E, SE, S, SW, W, NW, NONE };
    public delegate void MouseStickMovedEventHandler(object sender, MouseStickEventArgs e);
    [DefaultEventAttribute("MouseStickMoved")]

    public partial class VirtualJoystick : Control
    {
        public event MouseStickMovedEventHandler MouseStickMoved;
        private Image background;
        private Image joystick;
        private float joystickPosX;
        private float joystickPosY;
        private float joystickCenterX;
        private float joystickCenterY;
        private float joystickMaxDistance;
        private RectangleF backgroundBounds = RectangleF.Empty;
        private bool isDragging = false;
        private float joystickScale = 0.4f;
        private float joystickMoveScale = 0.78f;
        private dir direction = dir.NONE;

        public VirtualJoystick()
        {
            InitializeComponent();

            // 加载背景图片和摇杆图片
            this.background = Properties.Resource1.JoyStick_Base;
            this.joystick = Properties.Resource1.JoyStick_stick;

            // 计算摇杆的初始位置和中心点
            this.joystickPosX = this.Width / 2f - this.joystick.Width * this.joystickScale / 2f;
            this.joystickPosY = this.Height / 2f - this.joystick.Height * this.joystickScale / 2f;
            this.joystickCenterX = this.joystickPosX + this.joystick.Width * this.joystickScale / 2f;
            this.joystickCenterY = this.joystickPosY + this.joystick.Height * this.joystickScale / 2f;

            // 计算摇杆的最大距离和背景图片的范围
            this.joystickMaxDistance = this.background.Width / 2f * joystickMoveScale;
            this.backgroundBounds.X = (this.Width - this.background.Width) / 2f;
            this.backgroundBounds.Y = (this.Height - this.background.Height) / 2f;
            this.backgroundBounds.Width = this.background.Width;
            this.backgroundBounds.Height = this.background.Height;

            // 调用OnResize方法计算摇杆初始位置和中心点
            this.OnResize(EventArgs.Empty); 

            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.background != null && this.joystick != null)
            {
                // 计算背景图片和摇杆图片的范围和位置
                float aspectRatio = (float)this.background.Width / this.background.Height;
                int width = (int)(this.Height * aspectRatio);
                int height = this.Height;

                if (width > this.Width)
                {
                    width = this.Width;
                    height = (int)(this.Width / aspectRatio);
                }

                int x = (this.Width - width) / 2;
                int y = (this.Height - height) / 2;

                this.backgroundBounds.X = x;
                this.backgroundBounds.Y = y;
                this.backgroundBounds.Width = width;
                this.backgroundBounds.Height = height;

                this.joystickMaxDistance = this.backgroundBounds.Width / 2f * joystickMoveScale;

                this.joystickPosX = this.backgroundBounds.X + this.backgroundBounds.Width / 2f - this.joystick.Width / 2f * this.joystickScale;
                this.joystickPosY = this.backgroundBounds.Y + this.backgroundBounds.Height / 2f - this.joystick.Height / 2f * this.joystickScale;
                this.joystickCenterX = this.backgroundBounds.X + this.backgroundBounds.Width / 2f - this.joystick.Width / 2f * this.joystickScale;
                this.joystickCenterY = this.backgroundBounds.Y + this.backgroundBounds.Height / 2f - this.joystick.Height / 2f * this.joystickScale;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (this.background != null && this.joystick != null)
            {
                // 创建一个临时的画布，用于双缓冲绘制
                Bitmap buffer = new Bitmap(this.Width, this.Height);
                Graphics g = Graphics.FromImage(buffer);

                // 创建一个透明的画刷
                SolidBrush transparentBrush = new SolidBrush(Color.Transparent);
                // 使用透明画刷绘制控件的背景
                g.FillRectangle(transparentBrush, this.ClientRectangle);


                // 绘制背景图片
                float aspectRatio = (float)this.background.Width / this.background.Height;
                int width = (int)(this.Height * aspectRatio);
                int height = this.Height;

                if (width > this.Width)
                {
                    width = this.Width;
                    height = (int)(this.Width / aspectRatio);
                }

                int x = (this.Width - width) / 2;
                int y = (this.Height - height) / 2;
                g.DrawImage(this.background, new Rectangle(x, y, width, height));

                // 绘制摇杆图片
                aspectRatio = (float)this.joystick.Width / this.joystick.Height;
                height = (int)(this.Height * this.joystickScale);
                width = (int)(height * aspectRatio);
                x = (int)(this.joystickPosX + (this.joystick.Width * this.joystickScale - width) / 2f);
                y = (int)(this.joystickPosY + (this.joystick.Height * this.joystickScale - height) / 2f);
                g.DrawImage(this.joystick, new Rectangle(x, y, width, height));

                // 将缓冲区中的图像绘制到控件的画布上
                e.Graphics.DrawImage(buffer, 0, 0);

                // 释放临时画布和图像
                g.Dispose();
                buffer.Dispose();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 如果鼠标点击在摇杆图片内，则开始拖动摇杆
            if (this.joystickPosX <= e.X && e.X <= this.joystickPosX + this.joystick.Width * this.joystickScale &&
        this.joystickPosY <= e.Y && e.Y <= this.joystickPosY + this.joystick.Height * this.joystickScale)
            {
                this.isDragging = true;
            }
        }

        private dir GetDirection(float x, float y)
        {
            //Changing cartesian coordinates from control surface to  
            //more usable polar coordinates
            double theta;
            if (x >= 0 && y > 0)
                theta = (Math.Atan(y / x) * (180 / Math.PI));
            else if (x < 0)
                theta = ((Math.PI + Math.Atan(y / x)) * (180 / Math.PI));
            else theta = (((2 * Math.PI) + Math.Atan(y / x)) * (180 / Math.PI));

            //Changing from degrees to direction.
            if (theta <= 26 || theta > 341)
                return dir.E;
            else if (theta > 26 && theta <= 71)
                return dir.NE;
            else if (theta > 71 && theta <= 116)
                return dir.N;
            else if (theta > 116 && theta <= 161)
                return dir.NW;
            else if (theta > 161 && theta <= 206)
                return dir.W;
            else if (theta > 206 && theta <= 251)
                return dir.SW;
            else if (theta > 251 && theta <= 296)
                return dir.S;
            else return dir.SE;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.isDragging)
            {
                // 移动摇杆图片，并限制其范围在背景图片的圆内
                float dx = e.X - this.joystickCenterX - this.joystick.Width * this.joystickScale / 2f;
                float dy = e.Y - this.joystickCenterY - this.joystick.Height * this.joystickScale / 2f;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance > this.joystickMaxDistance * this.joystickMoveScale)
                {
                    dx *= this.joystickMaxDistance * this.joystickMoveScale / distance;
                    dy *= this.joystickMaxDistance * this.joystickMoveScale / distance;
                }

                float percent = (float)Math.Sqrt(dx * dx + dy * dy) / (this.joystickMaxDistance * this.joystickMoveScale);
                direction = GetDirection(dx, -dy);

                this.joystickPosX = this.joystickCenterX + dx;
                this.joystickPosY = this.joystickCenterY + dy;

                if (!this.backgroundBounds.Contains(this.joystickPosX, this.joystickPosY))
                {
                    float angle = (float)Math.Atan2(dy, dx);
                    this.joystickPosX = this.joystickCenterX + this.joystickMaxDistance * this.joystickMoveScale * (float)Math.Cos(angle);
                    this.joystickPosY = this.joystickCenterY + this.joystickMaxDistance * this.joystickMoveScale * (float)Math.Sin(angle);
                }

                // 重新绘制控件
                this.Invalidate();
                if (MouseStickMoved != null)
                    MouseStickMoved(this, new MouseStickEventArgs(percent, direction));
            }



        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (this.isDragging)
            {
                // 如果鼠标释放，则将摇杆图片居中，并开始自动居中定时器
                this.isDragging = false;

                // 将摇杆图片居中
                this.joystickPosX = this.backgroundBounds.X + this.backgroundBounds.Width / 2f - this.joystick.Width / 2f * this.joystickScale;
                this.joystickPosY = this.backgroundBounds.Y + this.backgroundBounds.Height / 2f - this.joystick.Height / 2f * this.joystickScale;
                this.joystickCenterX = this.backgroundBounds.X + this.backgroundBounds.Width / 2f - this.joystick.Width / 2f * this.joystickScale;
                this.joystickCenterY = this.backgroundBounds.Y + this.backgroundBounds.Height / 2f - this.joystick.Height / 2f * this.joystickScale;

                // 重新绘制控件
                this.Invalidate();

                if (MouseStickMoved != null)
                    MouseStickMoved(this, new MouseStickEventArgs(0, dir.NONE));
            }
        }
    }


    public class MouseStickEventArgs : System.EventArgs
    {
        public MouseStickEventArgs(float Percent, dir Direction)
        {
            direction = Direction;
            P = Percent;
        }

        public float Percent
        {
            get { return P; }
        }
        public dir Direction
        {
            get { return direction; }
        }

        float P;
        dir direction;
    }
}
