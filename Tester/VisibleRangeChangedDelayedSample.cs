using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class VisibleRangeChangedDelayedSample : Form
    {
        //styles
        private readonly Style _blueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private readonly Style _maroonStyle = new TextStyle(Brushes.Maroon, null, FontStyle.Regular);
        private readonly Style _redStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);

        public VisibleRangeChangedDelayedSample()
        {
            InitializeComponent();

            //generate 200,000 lines of HTML
            var html4Line =
                @"<li id=""ctl00_TopNavBar_AQL"">
<a id=""ctl00_TopNavBar_ArticleQuestion"" class=""fly highlight"" href=""#_comments"">Ask a Question about this article</a></li>
<li class=""heading"">Quick Answers</li>
<li><a id=""ctl00_TopNavBar_QAAsk"" class=""fly"" href=""/Questions/ask.aspx"">Ask a Question</a></li>";
            var sb = new StringBuilder();
            for (var i = 0; i < 50000; i++)
                sb.AppendLine(html4Line);

            //assign to FastColoredTextBox
            fctb.Text = sb.ToString();
            fctb.IsChanged = false;
            fctb.ClearUndo();
            //set delay interval (10 ms)
            fctb.DelayedEventsInterval = 10;
        }

        private void fctb_VisibleRangeChangedDelayed(object sender, EventArgs e)
        {
            //highlight only visible area of text
            HtmlSyntaxHighlight(fctb.VisibleRange);
        }

        private void HtmlSyntaxHighlight(Range range)
        {
            //clear style of changed range
            range.ClearStyle(_blueStyle, _maroonStyle, _redStyle);
            //tag brackets highlighting
            range.SetStyle(_blueStyle, @"<|/>|</|>");
            //tag name
            range.SetStyle(_maroonStyle, @"<(?<range>[!\w]+)");
            //end of tag
            range.SetStyle(_maroonStyle, @"</(?<range>\w+)>");
            //attributes
            range.SetStyle(_redStyle, @"(?<range>\S+?)='[^']*'|(?<range>\S+)=""[^""]*""|(?<range>\S+)=\S+");
            //attribute values
            range.SetStyle(_blueStyle, @"\S+?=(?<range>'[^']*')|\S+=(?<range>""[^""]*"")|\S+=(?<range>\S+)");
        }
    }
}