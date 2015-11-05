using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class PredefinedStylesSample : Form
    {
        public PredefinedStylesSample()
        {
            InitializeComponent();

            GenerateText();
        }

        private void GenerateText()
        {
            var rnd = new Random();

            fctb.BeginUpdate();
            fctb.Selection.BeginUpdate();

            for (var i = 0; i < 50000; i++)
            {
                switch (rnd.Next(4))
                {
                    case 0:
                        fctb.AppendText("This is simple text ");
                        break;
                    case 1:
                        fctb.AppendText("Some link", new BlockDesc {Url = "http://google.com?q=" + i});
                        break;
                    case 2:
                        fctb.AppendText("TooltipedText ", new BlockDesc {IsBold = true, ToolTip = "ToolTip " + i});
                        break;
                    case 3:
                        fctb.NewLine();
                        break;
                }
            }

            fctb.Selection.EndUpdate();
            fctb.EndUpdate();
        }
    }

    internal class ReadOnlyFctb : FastColoredTextBox
    {
        private readonly Place _emptyPlace = new Place(-1, -1);
        private readonly List<BlockDesc> _blockDescs = new List<BlockDesc>();
        private readonly TextStyle _boldStyle = new TextStyle(Brushes.Navy, null, FontStyle.Bold);

        private Point _lastMouseCoord;
        private Place _lastPlace;
        private readonly TextStyle _linkStyle = new TextStyle(Brushes.Blue, null, FontStyle.Underline);
        private readonly TextStyle _visitedLinkStyle = new TextStyle(Brushes.Brown, null, FontStyle.Underline);

        public ReadOnlyFctb()
        {
            ReadOnly = true;
        }

        public void NewLine()
        {
            AppendText("\n");
        }

        public void AppendText(string text, BlockDesc desc)
        {
            var oldPlace = new Place(GetLineLength(LinesCount - 1), LinesCount - 1);

            if (desc.IsBold)
                AppendText(text, _boldStyle);
            else if (!string.IsNullOrEmpty(desc.Url))
                AppendText(text, _linkStyle);
            else
                AppendText(text);

            //if descriptor contains some additional data ...
            if (!string.IsNullOrEmpty(desc.Url) || !string.IsNullOrEmpty(desc.ToolTip))
            {
                //save descriptor in sorted list
                desc.Start = oldPlace;
                desc.End = new Place(GetLineLength(LinesCount - 1), LinesCount - 1);
                _blockDescs.Add(desc);
            }
        }

        private BlockDesc GetDesc(Place place)
        {
            var index = _blockDescs.BinarySearch(new BlockDesc {Start = place, End = place});
            if (index >= 0)
                return _blockDescs[index];

            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _lastMouseCoord = e.Location;
            Cursor = Cursors.IBeam;

            //get place under mouse
            _lastPlace = PointToPlace(_lastMouseCoord);

            //check distance
            var p = PlaceToPoint(_lastPlace);
            if (Math.Abs(p.X - _lastMouseCoord.X) > CharWidth*2 || Math.Abs(p.Y - _lastMouseCoord.Y) > CharHeight*2)
                _lastPlace = _emptyPlace;

            //check link style
            if (_lastPlace != _emptyPlace)
            {
                var styles = GetStylesOfChar(_lastPlace);
                if (styles.Contains(_linkStyle) || styles.Contains(_visitedLinkStyle))
                    Cursor = Cursors.Hand;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var desc = GetDesc(_lastPlace);
            if (desc != null && !string.IsNullOrEmpty(desc.Url))
            {
                var r = new Range(this, desc.Start, desc.End);
                r.ClearStyle(_linkStyle);
                r.SetStyle(_visitedLinkStyle);
                BeginInvoke(new MethodInvoker(() => Process.Start(desc.Url)));
            }

            base.OnMouseDown(e);
        }

        protected override void OnToolTip()
        {
            if (ToolTip == null)
                return;

            //get descriptor for place
            var desc = GetDesc(_lastPlace);

            //show tooltip
            if (desc != null)
            {
                var toolTip = desc.ToolTip ?? desc.Url;
                ToolTip.SetToolTip(this, toolTip);
                ToolTip.Show(toolTip, this, new Point(_lastMouseCoord.X, _lastMouseCoord.Y + CharHeight));
            }
        }
    }

    public class BlockDesc : IComparable<BlockDesc>
    {
        internal Place End;
        public bool IsBold;

        internal Place Start;
        public string ToolTip;
        public string Url;

        public int CompareTo(BlockDesc other)
        {
            if (Start <= other.Start && End > other.End) return 0;
            if (Start <= other.Start) return -1;
            return 1;
        }
    }
}