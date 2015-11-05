using System;
using System.Windows.Forms;

namespace TesterIrony
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new SimplestSample().ShowDialog();
        }
    }
}