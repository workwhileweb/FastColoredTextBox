using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tester
{
    public partial class CustomScrollBarsSample : Form
    {
        public CustomScrollBarsSample()
        {
            InitializeComponent();
        }

        private void AdjustScrollbars()
        {
            AdjustScrollbar(vMyScrollBar, fctb.VerticalScroll.Maximum, fctb.VerticalScroll.Value, fctb.ClientSize.Height);
            AdjustScrollbar(hMyScrollBar, fctb.HorizontalScroll.Maximum, fctb.HorizontalScroll.Value,
                fctb.ClientSize.Width);

            AdjustScrollbar(vScrollBar, fctb.VerticalScroll.Maximum, fctb.VerticalScroll.Value, fctb.ClientSize.Height);
            AdjustScrollbar(hScrollBar, fctb.HorizontalScroll.Maximum, fctb.HorizontalScroll.Value,
                fctb.ClientSize.Width);
        }

        /// <summary>
        ///     This method for MyScrollBar
        /// </summary>
        private void AdjustScrollbar(MyScrollBar scrollBar, int max, int value, int clientSize)
        {
            scrollBar.Maximum = max;
            scrollBar.Visible = max > 0;
            scrollBar.Value = Math.Min(scrollBar.Maximum, value);
        }

        /// <summary>
        ///     This method for System.Windows.Forms.ScrollBar and inherited classes
        /// </summary>
        private void AdjustScrollbar(ScrollBar scrollBar, int max, int value, int clientSize)
        {
            scrollBar.LargeChange = clientSize/3;
            scrollBar.SmallChange = clientSize/11;
            scrollBar.Maximum = max + scrollBar.LargeChange;
            scrollBar.Visible = max > 0;
            scrollBar.Value = Math.Min(scrollBar.Maximum, value);
        }

        private void fctb_ScrollbarsUpdated(object sender, EventArgs e)
        {
            AdjustScrollbars();
        }

        private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            fctb.OnScroll(e, e.Type != ScrollEventType.ThumbTrack && e.Type != ScrollEventType.ThumbPosition);
        }
    }

    #region MyScrollBar

    public class MyScrollBar : Control
    {
        private Color _borderColor = Color.Silver;

        private int _maximum = 100;

        private ScrollOrientation _orientation;

        private Color _thumbColor = Color.Gray;

        private int _thumbSize = 10;
        private int _value;

        public MyScrollBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public int Value
        {
            get { return _value; }
            set
            {
                if (_value == value)
                    return;
                _value = value;
                Invalidate();
                OnScroll();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                Invalidate();
            }
        }

        public int ThumbSize
        {
            get { return _thumbSize; }
            set
            {
                _thumbSize = value;
                Invalidate();
            }
        }

        public Color ThumbColor
        {
            get { return _thumbColor; }
            set
            {
                _thumbColor = value;
                Invalidate();
            }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        public ScrollOrientation Orientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
                Invalidate();
            }
        }

        public event ScrollEventHandler Scroll;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                MouseScroll(e);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                MouseScroll(e);
            base.OnMouseMove(e);
        }

        /*
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                OnScroll(ScrollEventType.EndScroll);
            base.OnMouseUp(e);
        }*/

        private void MouseScroll(MouseEventArgs e)
        {
            var v = 0;
            switch (Orientation)
            {
                case ScrollOrientation.VerticalScroll:
                    v = Maximum*(e.Y - _thumbSize/2)/(Height - _thumbSize);
                    break;
                case ScrollOrientation.HorizontalScroll:
                    v = Maximum*(e.X - _thumbSize/2)/(Width - _thumbSize);
                    break;
            }
            Value = Math.Max(0, Math.Min(Maximum, v));
        }

        public virtual void OnScroll(ScrollEventType type = ScrollEventType.ThumbPosition)
        {
            if (Scroll != null)
                Scroll(this, new ScrollEventArgs(type, Value, Orientation));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Maximum <= 0)
                return;

            var thumbRect = Rectangle.Empty;
            switch (Orientation)
            {
                case ScrollOrientation.HorizontalScroll:
                    thumbRect = new Rectangle(_value*(Width - _thumbSize)/Maximum, 2, _thumbSize, Height - 4);
                    break;
                case ScrollOrientation.VerticalScroll:
                    thumbRect = new Rectangle(2, _value*(Height - _thumbSize)/Maximum, Width - 4, _thumbSize);
                    break;
            }

            using (var brush = new SolidBrush(_thumbColor))
                e.Graphics.FillRectangle(brush, thumbRect);

            using (var pen = new Pen(_borderColor))
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }

    #endregion
}