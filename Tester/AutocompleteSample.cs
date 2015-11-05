using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class AutocompleteSample : Form
    {
        private readonly AutocompleteMenu _popupMenu;

        public AutocompleteSample()
        {
            InitializeComponent();

            //create autocomplete popup menu
            _popupMenu = new AutocompleteMenu(fctb);
            _popupMenu.MinFragmentLength = 2;

            //generate 456976 words
            var randomWords = new List<string>();
            var codeA = Convert.ToInt32('a');
            for (var i = 0; i < 26; i++)
                for (var j = 0; j < 26; j++)
                    for (var k = 0; k < 26; k++)
                        for (var l = 0; l < 26; l++)
                            randomWords.Add(
                                new string(new[]
                                {
                                    Convert.ToChar(i + codeA), Convert.ToChar(j + codeA), Convert.ToChar(k + codeA),
                                    Convert.ToChar(l + codeA)
                                }));

            //set words as autocomplete source
            _popupMenu.Items.SetAutocompleteItems(randomWords);
            //size of popupmenu
            _popupMenu.Items.MaximumSize = new Size(200, 300);
            _popupMenu.Items.Width = 200;
        }

        private void fctb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.K | Keys.Control))
            {
                //forced show (MinFragmentLength will be ignored)
                _popupMenu.Show(true);
                e.Handled = true;
            }
        }
    }
}