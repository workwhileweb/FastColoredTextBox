using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class CustomWordWrapSample : Form
    {
        private readonly Regex _regex = new Regex(@"&&|&|\|\||\|");

        public CustomWordWrapSample()
        {
            InitializeComponent();
        }

        private void fctb_WordWrapNeeded(object sender, WordWrapNeededEventArgs e)
        {
            //var max = (fctb.ClientSize.Width - fctb.LeftIndent)/fctb.CharWidth;
            //FastColoredTextBox.CalcCutOffs(e.CutOffPositions, max, max, e.ImeAllowed, true, e.Line);

            e.CutOffPositions.Clear();
            foreach (Match m in _regex.Matches(e.Line.Text))
                e.CutOffPositions.Add(m.Index);
        }
    }
}