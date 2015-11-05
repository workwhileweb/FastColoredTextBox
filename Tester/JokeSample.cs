using System;
using System.Drawing;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class JokeSample : Form
    {
        public JokeSample()
        {
            InitializeComponent();
            fctb.DefaultStyle = new JokeStyle();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            fctb.Invalidate();
        }
    }

    /// <summary>
    ///     This class is used as text renderer
    /// </summary>
    internal class JokeStyle : TextStyle
    {
        public JokeStyle() : base(null, null, FontStyle.Regular)
        {
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            foreach (var p in range)
            {
                var time = (int) (DateTime.Now.TimeOfDay.TotalMilliseconds/2);
                var angle = (int) (time%360L);
                var angle2 = (int) ((time - (p.IChar - range.Start.IChar)*20)%360L)*2;
                var x = position.X + (p.IChar - range.Start.IChar)*range.Tb.CharWidth;
                var r = range.Tb.GetRange(p, new Place(p.IChar + 1, p.ILine));
                var point = new Point(x, position.Y + (int) (5 + 5*Math.Sin(Math.PI*angle2/180)));
                gr.ResetTransform();
                gr.TranslateTransform(point.X + range.Tb.CharWidth/2, point.Y + range.Tb.CharHeight/2);
                gr.RotateTransform(angle);
                gr.ScaleTransform(0.8f, 0.8f);
                gr.TranslateTransform(-range.Tb.CharWidth/2, -range.Tb.CharHeight/2);
                base.Draw(gr, new Point(0, 0), r);
            }
            gr.ResetTransform();
        }
    }
}