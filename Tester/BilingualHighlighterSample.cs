using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class BilingualHighlighterSample : Form
    {
        public BilingualHighlighterSample()
        {
            InitializeComponent();
        }

        private void tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            var tb = (FastColoredTextBox) sender;

            //highlight html
            tb.SyntaxHighlighter.InitStyleSchema(Language.Html);
            tb.SyntaxHighlighter.HtmlSyntaxHighlight(tb.Range);
            tb.Range.ClearFoldingMarkers();
            //find PHP fragments
            foreach (var r in tb.GetRanges(@"<\?php.*?\?>", RegexOptions.Singleline))
            {
                //remove HTML highlighting from this fragment
                r.ClearStyle(StyleIndex.All);
                //do PHP highlighting
                tb.SyntaxHighlighter.InitStyleSchema(Language.Php);
                tb.SyntaxHighlighter.PhpSyntaxHighlight(r);
            }
        }
    }
}