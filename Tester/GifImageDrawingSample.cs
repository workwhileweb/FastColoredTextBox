using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using Tester.Properties;

namespace Tester
{
    public partial class GifImageDrawingSample : Form
    {
        private static readonly string _regexSpecSymbolsPattern = @"[\^\$\[\]\(\)\.\\\*\+\|\?\{\}]";
        private readonly GifImageStyle _style;

        public GifImageDrawingSample()
        {
            InitializeComponent();

            _style = new GifImageStyle(fctb);
            _style.ImagesByText.Add(@":bb", Resources.bye);
            _style.ImagesByText.Add(@":D", Resources.lol);
            _style.ImagesByText.Add(@"8)", Resources.rolleyes);
            _style.ImagesByText.Add(@":@", Resources.unsure);
            _style.ImagesByText.Add(@":)", Resources.smile_16x16);
            _style.ImagesByText.Add(@":(", Resources.sad_16x16);

            _style.StartAnimation();

            fctb.OnTextChanged();
        }

        private void fctb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_style == null) return;
            e.ChangedRange.ClearStyle(StyleIndex.All);
            foreach (var key in _style.ImagesByText.Keys)
            {
                var pattern = Regex.Replace(key, _regexSpecSymbolsPattern, "\\$0");
                e.ChangedRange.SetStyle(_style, pattern);
            }
        }
    }

    /// <summary>
    ///     This class is used as text renderer for smiles
    /// </summary>
    internal class GifImageStyle : TextStyle
    {
        private FastColoredTextBox _parent;
        private readonly Timer _timer;

        public GifImageStyle(FastColoredTextBox parent)
            : base(null, null, FontStyle.Regular)
        {
            ImagesByText = new Dictionary<string, Image>();
            _parent = parent;

            //create timer
            _timer = new Timer();
            _timer.Interval = 100;
            _timer.Tick += (EventHandler) delegate
            {
                ImageAnimator.UpdateFrames();
                parent.Invalidate();
            };
            _timer.Start();
        }

        public Dictionary<string, Image> ImagesByText { get; }

        public void StartAnimation()
        {
            foreach (var image in ImagesByText.Values)
                if (ImageAnimator.CanAnimate(image))
                    ImageAnimator.Animate(image, OnFrameChanged);
        }

        private void OnFrameChanged(object sender, EventArgs args)
        {
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            var text = range.Text;
            var iChar = range.Start.IChar;

            while (text != "")
            {
                var replaced = false;
                foreach (var pair in ImagesByText)
                {
                    if (text.StartsWith(pair.Key))
                    {
                        var k = (float) (pair.Key.Length*range.Tb.CharWidth)/pair.Value.Width;
                        if (k > 1)
                            k = 1f;
                        //
                        text = text.Substring(pair.Key.Length);
                        var rect =
                            new RectangleF(position.X + range.Tb.CharWidth*pair.Key.Length/2 - pair.Value.Width*k/2,
                                position.Y, pair.Value.Width*k, pair.Value.Height*k);
                        gr.DrawImage(pair.Value, rect);
                        position.Offset(range.Tb.CharWidth*pair.Key.Length, 0);
                        replaced = true;
                        iChar += pair.Key.Length;
                        break;
                    }
                }
                if (!replaced && text.Length > 0)
                {
                    var r = new Range(range.Tb, iChar, range.Start.ILine, iChar + 1, range.Start.ILine);
                    base.Draw(gr, position, r);
                    position.Offset(range.Tb.CharWidth, 0);
                    text = text.Substring(1);
                }
            }
        }
    }
}