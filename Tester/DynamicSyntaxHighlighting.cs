using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class DynamicSyntaxHighlighting : Form
    {
        private readonly Style _functionNameStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private readonly Style _keywordsStyle = new TextStyle(Brushes.Green, null, FontStyle.Regular);

        public DynamicSyntaxHighlighting()
        {
            InitializeComponent();
        }

        private void fctb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            //clear styles
            fctb.Range.ClearStyle(_keywordsStyle, _functionNameStyle);
            //highlight keywords of LISP
            fctb.Range.SetStyle(_keywordsStyle, @"\b(and|eval|else|if|lambda|or|set|defun)\b", RegexOptions.IgnoreCase);
            //find function declarations, highlight all of their entry into the code
            foreach (var found in fctb.GetRanges(@"\b(defun|DEFUN)\s+(?<range>\w+)\b"))
                fctb.Range.SetStyle(_functionNameStyle, @"\b" + found.Text + @"\b");
        }
    }
}