using System;
using System.Drawing;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class LoggerSample : Form
    {
        private readonly TextStyle _errorStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
        private readonly TextStyle _infoStyle = new TextStyle(Brushes.Black, null, FontStyle.Regular);
        private readonly TextStyle _warningStyle = new TextStyle(Brushes.BurlyWood, null, FontStyle.Regular);

        public LoggerSample()
        {
            InitializeComponent();
        }

        private void tm_Tick(object sender, EventArgs e)
        {
            switch (DateTime.Now.Millisecond%3)
            {
                case 0:
                    Log(DateTime.Now + " Error\r\n", _errorStyle);
                    break;
                case 1:
                    Log(DateTime.Now + " Warning\r\n", _warningStyle);
                    break;
                case 2:
                    Log(DateTime.Now + " Info\r\n", _infoStyle);
                    break;
            }
        }

        private void Log(string text, Style style)
        {
            //some stuffs for best performance
            fctb.BeginUpdate();
            fctb.Selection.BeginUpdate();
            //remember user selection
            var userSelection = fctb.Selection.Clone();
            //add text with predefined style
            fctb.TextSource.CurrentTb = fctb;
            fctb.AppendText(text, style);
            //restore user selection
            if (!userSelection.IsEmpty || userSelection.Start.ILine < fctb.LinesCount - 2)
            {
                fctb.Selection.Start = userSelection.Start;
                fctb.Selection.End = userSelection.End;
            }
            else
                fctb.GoEnd(); //scroll to end of the text
            //
            fctb.Selection.EndUpdate();
            fctb.EndUpdate();
        }

        private void btGotToEnd_Click(object sender, EventArgs e)
        {
            fctb.GoEnd();
        }
    }
}