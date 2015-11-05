using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using Char = FastColoredTextBoxNS.Char;

namespace Tester
{
    public partial class CustomTextSourceSample : Form
    {
        public CustomTextSourceSample()
        {
            InitializeComponent();
        }

        private string CreateExtraLargeString()
        {
            var sb = new StringBuilder();
            var dt = DateTime.Now;
            for (var i = 0; i < 500000; i++)
            {
                sb.AppendFormat("{0}  POST http://mysite.com/{1}.aspx HTTP1.0\n", dt.AddSeconds(i), i%20);
                sb.AppendFormat("{0}  GET http://myothersite.com/{1}.aspx HTTP1.1\n", dt.AddSeconds(i), i%20);
            }

            return sb.ToString();
        }

        private void CustomTextSourceSample_Shown(object sender, EventArgs e)
        {
            var s = CreateExtraLargeString();
            MessageBox.Show("Extralarge string is created.\nPress OK to assign string to the FastColoredTextbox");
            //create our custom TextSource
            var ts = new StringTextSource(fctb);
            //open source string
            ts.OpenString(s);
            //assign TextSource to the component
            fctb.TextSource = ts;
        }

        private void fctb_VisibleRangeChanged(object sender, EventArgs e)
        {
            var range = fctb.VisibleRange;
            range.ClearStyle(StyleIndex.All);
            fctb.VisibleRange.SetStyle(fctb.SyntaxHighlighter.BrownStyle, "^.+?  ", RegexOptions.Multiline);
            fctb.VisibleRange.SetStyle(fctb.SyntaxHighlighter.BlueBoldStyle, @"POST|GET", RegexOptions.Multiline);
        }
    }


    /// <summary>
    ///     Text source for displaying readonly text, given as string.
    /// </summary>
    public class StringTextSource : TextSource, IDisposable
    {
        private string _sourceString;
        private readonly List<int> _sourceStringLinePositions = new List<int>();
        private readonly Timer _timer = new Timer();

        public StringTextSource(FastColoredTextBox tb)
            : base(tb)
        {
            _timer.Interval = 10000;
            _timer.Tick += timer_Tick;
            _timer.Enabled = true;
        }

        public override Line this[int i]
        {
            get
            {
                if (Lines[i] != null)
                    return Lines[i];
                LoadLineFromSourceString(i);

                return Lines[i];
            }
            set { throw new NotImplementedException(); }
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                UnloadUnusedLines();
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private void UnloadUnusedLines()
        {
            const int margin = 2000;
            var iStartVisibleLine = CurrentTb.VisibleRange.Start.ILine;
            var iFinishVisibleLine = CurrentTb.VisibleRange.End.ILine;

            var count = 0;
            for (var i = 0; i < Count; i++)
                if (Lines[i] != null && !Lines[i].IsChanged && Math.Abs(i - iFinishVisibleLine) > margin)
                {
                    Lines[i] = null;
                    count++;
                }
#if debug
            Console.WriteLine("UnloadUnusedLines: " + count);
#endif
        }

        public void OpenString(string sourceString)
        {
            Clear();

            _sourceString = sourceString;

            //parse lines
            var index = -1;
            do
            {
                _sourceStringLinePositions.Add(index + 1);
                Lines.Add(null);
                index = sourceString.IndexOf('\n', index + 1);
            } while (index >= 0);

            OnLineInserted(0, Count);

            //load first lines for calc width of the text
            var linesCount = Math.Min(Lines.Count, CurrentTb.Height/CurrentTb.CharHeight);
            for (var i = 0; i < linesCount; i++)
                LoadLineFromSourceString(i);

            NeedRecalc(new TextChangedEventArgs(0, linesCount - 1));
            if (CurrentTb.WordWrap)
                OnRecalcWordWrap(new TextChangedEventArgs(0, linesCount - 1));
        }

        public override void ClearIsChanged()
        {
            foreach (var line in Lines)
                if (line != null)
                    line.IsChanged = false;
        }

        private void LoadLineFromSourceString(int i)
        {
            var line = CreateLine();

            string s;
            if (i == Count - 1)
                s = _sourceString.Substring(_sourceStringLinePositions[i]);
            else
                s = _sourceString.Substring(_sourceStringLinePositions[i],
                    _sourceStringLinePositions[i + 1] - _sourceStringLinePositions[i] - 1);

            foreach (var c in s)
                line.Add(new Char(c));

            Lines[i] = line;

            if (CurrentTb.WordWrap)
                OnRecalcWordWrap(new TextChangedEventArgs(i, i));
        }

        public override void InsertLine(int index, Line line)
        {
            throw new NotImplementedException();
        }

        public override void RemoveLine(int index, int count)
        {
            if (count == 0) return;
            throw new NotImplementedException();
        }

        public override int GetLineLength(int i)
        {
            if (Lines[i] == null)
                return 0;
            return Lines[i].Count;
        }

        public override bool LineHasFoldingStartMarker(int iLine)
        {
            if (Lines[iLine] == null)
                return false;
            return !string.IsNullOrEmpty(Lines[iLine].FoldingStartMarker);
        }

        public override bool LineHasFoldingEndMarker(int iLine)
        {
            if (Lines[iLine] == null)
                return false;
            return !string.IsNullOrEmpty(Lines[iLine].FoldingEndMarker);
        }

        internal void UnloadLine(int iLine)
        {
            if (Lines[iLine] != null && !Lines[iLine].IsChanged)
                Lines[iLine] = null;
        }
    }
}