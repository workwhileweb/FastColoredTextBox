using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class Sandbox : Form
    {
        private FastColoredTextBox _fctb;

        public Sandbox()
        {
            InitializeComponent();

            _fctb = new FastColoredTextBox
            {
                Dock = DockStyle.Fill,
                Parent = this,
                Language = Language.Xml,
                HighlightingRangeType = HighlightingRangeType.AllTextRange,
                ShowFoldingLines = true
            };
        }
    }
}