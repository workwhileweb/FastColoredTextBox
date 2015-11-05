using System.Drawing;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    public class VisualMarker
    {
        public readonly Rectangle Rectangle;

        public VisualMarker(Rectangle rectangle)
        {
            Rectangle = rectangle;
        }

        public virtual Cursor Cursor
        {
            get { return Cursors.Hand; }
        }

        public virtual void Draw(Graphics gr, Pen pen)
        {
        }
    }

    public class CollapseFoldingMarker : VisualMarker
    {
        public readonly int ILine;

        public CollapseFoldingMarker(int line, Rectangle rectangle)
            : base(rectangle)
        {
            ILine = line;
        }

        public void Draw(Graphics gr, Pen pen, Brush backgroundBrush, Pen forePen)
        {
            //draw minus
            gr.FillRectangle(backgroundBrush, Rectangle);
            gr.DrawRectangle(pen, Rectangle);
            gr.DrawLine(forePen, Rectangle.Left + 2, Rectangle.Top + Rectangle.Height/2, Rectangle.Right - 2,
                Rectangle.Top + Rectangle.Height/2);
        }
    }

    public class ExpandFoldingMarker : VisualMarker
    {
        public readonly int ILine;

        public ExpandFoldingMarker(int line, Rectangle rectangle)
            : base(rectangle)
        {
            ILine = line;
        }

        public void Draw(Graphics gr, Pen pen, Brush backgroundBrush, Pen forePen)
        {
            //draw plus
            gr.FillRectangle(backgroundBrush, Rectangle);
            gr.DrawRectangle(pen, Rectangle);
            gr.DrawLine(forePen, Rectangle.Left + 2, Rectangle.Top + Rectangle.Height/2, Rectangle.Right - 2,
                Rectangle.Top + Rectangle.Height/2);
            gr.DrawLine(forePen, Rectangle.Left + Rectangle.Width/2, Rectangle.Top + 2,
                Rectangle.Left + Rectangle.Width/2, Rectangle.Bottom - 2);
        }
    }

    public class FoldedAreaMarker : VisualMarker
    {
        public readonly int ILine;

        public FoldedAreaMarker(int line, Rectangle rectangle)
            : base(rectangle)
        {
            ILine = line;
        }

        public override void Draw(Graphics gr, Pen pen)
        {
            gr.DrawRectangle(pen, Rectangle);
        }
    }

    public class StyleVisualMarker : VisualMarker
    {
        public StyleVisualMarker(Rectangle rectangle, Style style)
            : base(rectangle)
        {
            Style = style;
        }

        public Style Style { get; private set; }
    }

    public class VisualMarkerEventArgs : MouseEventArgs
    {
        public VisualMarkerEventArgs(Style style, StyleVisualMarker marker, MouseEventArgs args)
            : base(args.Button, args.Clicks, args.X, args.Y, args.Delta)
        {
            Style = style;
            Marker = marker;
        }

        public Style Style { get; private set; }
        public StyleVisualMarker Marker { get; private set; }
    }
}