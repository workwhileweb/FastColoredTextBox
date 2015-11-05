using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class MarkerToolSample : Form
    {
        private readonly MarkerStyle _greenStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(180, Color.Green)));
        private readonly MarkerStyle _redStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(180, Color.Red)));
        //Shortcut style
        private readonly ShortcutStyle _shortCutStyle = new ShortcutStyle(Pens.Maroon);
        //Marker styles
        private readonly MarkerStyle _yellowStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(180, Color.Yellow)));

        public MarkerToolSample()
        {
            InitializeComponent();
            //
            BuildBackBrush();
            //add style explicitly to control for define priority of style drawing
            fctb.AddStyle(_yellowStyle); //render first
            fctb.AddStyle(_redStyle); //red will be rendering over yellow
            fctb.AddStyle(_greenStyle); //green will be rendering over yellow and red
            fctb.AddStyle(_shortCutStyle); //render last, over all other styles
        }

        private void fctb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            //here we draw shortcut for selection area
            var selection = fctb.Selection;
            //clear previous shortcuts
            fctb.VisibleRange.ClearStyle(_shortCutStyle);
            //create shortcuts
            if (!selection.IsEmpty) //user selected one or more chars?
            {
                //find last char
                var r = selection.Clone();
                r.Normalize();
                r.Start = r.End; //go to last char
                r.GoLeft(true); //select last char
                //apply ShortCutStyle
                r.SetStyle(_shortCutStyle);
            }
        }


        private void fctb_VisualMarkerClick(object sender, VisualMarkerEventArgs e)
        {
            //is it our style ?
            if (e.Style == _shortCutStyle)
            {
                //show popup menu
                cmMark.Show(fctb.PointToScreen(e.Location));
            }
        }

        private void markAsYellowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TrimSelection();
            //set background style
            switch ((string) ((sender as ToolStripMenuItem).Tag))
            {
                case "yellow":
                    fctb.Selection.SetStyle(_yellowStyle);
                    break;
                case "red":
                    fctb.Selection.SetStyle(_redStyle);
                    break;
                case "green":
                    fctb.Selection.SetStyle(_greenStyle);
                    break;
                case "lineBackground":
                    fctb[fctb.Selection.Start.ILine].BackgroundBrush = Brushes.Pink;
                    break;
            }
            //clear shortcut style
            fctb.Selection.ClearStyle(_shortCutStyle);
        }

        private void TrimSelection()
        {
            var sel = fctb.Selection;

            //trim left
            sel.Normalize();
            while (char.IsWhiteSpace(sel.CharAfterStart) && sel.Start < sel.End)
                sel.GoRight(true);
            //trim right
            sel.Inverse();
            while (char.IsWhiteSpace(sel.CharBeforeStart) && sel.Start > sel.End)
                sel.GoLeft(true);
        }

        private void clearMarkedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fctb.Selection.ClearStyle(_yellowStyle, _redStyle, _greenStyle);
            fctb[fctb.Selection.Start.ILine].BackgroundBrush = null;
        }

        private void fctb_PaintLine(object sender, PaintLineEventArgs e)
        {
            //draw current line marker
            if (e.LineIndex == fctb.Selection.Start.ILine)
                using (
                    var brush = new LinearGradientBrush(new Rectangle(0, e.LineRect.Top, 15, 15), Color.LightPink,
                        Color.Red, 45))
                    e.Graphics.FillEllipse(brush, 0, e.LineRect.Top, 15, 15);
        }

        private void fctb_Resize(object sender, EventArgs e)
        {
            BuildBackBrush();
        }

        private void BuildBackBrush()
        {
            fctb.BackBrush = new LinearGradientBrush(fctb.ClientRectangle, Color.White, Color.Silver,
                LinearGradientMode.Vertical);
        }
    }
}