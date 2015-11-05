using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class ConsoleSample : Form
    {
        private bool _stop;

        public ConsoleSample()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            var text = "";
            _stop = false;
            do
            {
                consoleTextBox1.WriteLine("Enter some line: ");
                text = consoleTextBox1.ReadLine();
            } while (text != "" && !_stop);

            consoleTextBox1.WriteLine("End of enetering.");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Stop();
            base.OnClosing(e);
        }

        private void Stop()
        {
            _stop = true;
            consoleTextBox1.IsReadLineMode = false;
        }
    }

    /// <summary>
    ///     Console emulator.
    /// </summary>
    public class ConsoleTextBox : FastColoredTextBox
    {
        private volatile bool _isReadLineMode;
        private volatile bool _isUpdating;
        private Place StartReadPlace { get; set; }

        /// <summary>
        ///     Control is waiting for line entering.
        /// </summary>
        public bool IsReadLineMode
        {
            get { return _isReadLineMode; }
            set { _isReadLineMode = value; }
        }

        /// <summary>
        ///     Append line to end of text.
        /// </summary>
        /// <param name="text"></param>
        public void WriteLine(string text)
        {
            IsReadLineMode = false;
            _isUpdating = true;
            try
            {
                AppendText(text);
                GoEnd();
            }
            finally
            {
                _isUpdating = false;
                ClearUndo();
            }
        }

        /// <summary>
        ///     Wait for line entering.
        ///     Set IsReadLineMode to false for break of waiting.
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            GoEnd();
            StartReadPlace = Range.End;
            IsReadLineMode = true;
            try
            {
                while (IsReadLineMode)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
            }
            finally
            {
                IsReadLineMode = false;
                ClearUndo();
            }

            return new Range(this, StartReadPlace, Range.End).Text.TrimEnd('\r', '\n');
        }

        public override void OnTextChanging(ref string text)
        {
            if (!IsReadLineMode && !_isUpdating)
            {
                text = ""; //cancel changing
                return;
            }

            if (IsReadLineMode)
            {
                if (Selection.Start < StartReadPlace || Selection.End < StartReadPlace)
                    GoEnd(); //move caret to entering position

                if (Selection.Start == StartReadPlace || Selection.End == StartReadPlace)
                    if (text == "\b") //backspace
                    {
                        text = ""; //cancel deleting of last char of readonly text
                        return;
                    }

                if (text != null && text.Contains('\n'))
                {
                    text = text.Substring(0, text.IndexOf('\n') + 1);
                    IsReadLineMode = false;
                }
            }

            base.OnTextChanging(ref text);
        }

        public override void Clear()
        {
            var oldIsReadMode = _isReadLineMode;

            _isReadLineMode = false;
            _isUpdating = true;

            base.Clear();

            _isUpdating = false;
            _isReadLineMode = oldIsReadMode;

            StartReadPlace = Place.Empty;
        }
    }
}