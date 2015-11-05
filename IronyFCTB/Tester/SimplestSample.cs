using System;
using System.Windows.Forms;
using Demo;
using Irony.Samples;
using Irony.Samples.CSharp;
using Irony.Samples.Java;
using Irony.Samples.Json;
using Irony.Samples.MiniPython;
using Irony.Samples.SQL;
using Irony.Samples.Scheme;

namespace Tester
{
    public partial class SimplestSample : Form
    {
        public SimplestSample()
        {
            InitializeComponent();
            cSharpToolStripMenuItem.PerformClick();
        }

        private void miClick(object sender, EventArgs e)
        {
            var langName = sender.ToString();
            miLanguage.Text = langName;
            switch(langName)
            {
                case "CSharp": ironyFCTB.Grammar = new CSharpGrammar(); break;
                case "Json": ironyFCTB.Grammar = new JsonGrammar(); break;
                case "Csv": ironyFCTB.Grammar = new SampleCsvGrammar(); break;
                case "GwBasic": ironyFCTB.Grammar = new GWBasicGrammar(); break;
                case "Java": ironyFCTB.Grammar = new JavaGrammar(); break;
                case "MiniPython": ironyFCTB.Grammar = new MiniPythonGrammar(); break;
                case "My C": ironyFCTB.Grammar = new MyCGrammar(); break;
                case "Scheme": ironyFCTB.Grammar = new SchemeGrammar(); break;
                case "SQL": ironyFCTB.Grammar = new SqlGrammar(); break;
                case "Wiki-Codeplex": ironyFCTB.Grammar = new WikiCodeplexGrammar(); break;
            }
        }
    }
}
