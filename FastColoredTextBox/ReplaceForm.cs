using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    public partial class ReplaceForm : Form
    {
        private bool _firstSearch = true;
        private Place _startPlace;
        private readonly FastColoredTextBox _tb;

        public ReplaceForm(FastColoredTextBox tb)
        {
            InitializeComponent();
            _tb = tb;
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btFindNext_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Find(tbFind.Text))
                    MessageBox.Show("Not found");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public List<Range> FindAll(string pattern)
        {
            var opt = cbMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
            if (!cbRegex.Checked)
                pattern = Regex.Escape(pattern);
            if (cbWholeWord.Checked)
                pattern = "\\b" + pattern + "\\b";
            //
            var range = _tb.Selection.IsEmpty ? _tb.Range.Clone() : _tb.Selection.Clone();
            //
            var list = new List<Range>();
            foreach (var r in range.GetRangesByLines(pattern, opt))
                list.Add(r);

            return list;
        }

        public bool Find(string pattern)
        {
            var opt = cbMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
            if (!cbRegex.Checked)
                pattern = Regex.Escape(pattern);
            if (cbWholeWord.Checked)
                pattern = "\\b" + pattern + "\\b";
            //
            var range = _tb.Selection.Clone();
            range.Normalize();
            //
            if (_firstSearch)
            {
                _startPlace = range.Start;
                _firstSearch = false;
            }
            //
            range.Start = range.End;
            if (range.Start >= _startPlace)
                range.End = new Place(_tb.GetLineLength(_tb.LinesCount - 1), _tb.LinesCount - 1);
            else
                range.End = _startPlace;
            //
            foreach (var r in range.GetRangesByLines(pattern, opt))
            {
                _tb.Selection.Start = r.Start;
                _tb.Selection.End = r.End;
                _tb.DoSelectionVisible();
                _tb.Invalidate();
                return true;
            }
            if (range.Start >= _startPlace && _startPlace > Place.Empty)
            {
                _tb.Selection.Start = new Place(0, 0);
                return Find(pattern);
            }
            return false;
        }

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                btFindNext_Click(sender, null);
            if (e.KeyChar == '\x1b')
                Hide();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) // David
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ReplaceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            _tb.Focus();
        }

        private void btReplace_Click(object sender, EventArgs e)
        {
            try
            {
                if (_tb.SelectionLength != 0)
                    if (!_tb.Selection.ReadOnly)
                        _tb.InsertText(tbReplace.Text);
                btFindNext_Click(sender, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btReplaceAll_Click(object sender, EventArgs e)
        {
            try
            {
                _tb.Selection.BeginUpdate();

                //search
                var ranges = FindAll(tbFind.Text);
                //check readonly
                var ro = false;
                foreach (var r in ranges)
                    if (r.ReadOnly)
                    {
                        ro = true;
                        break;
                    }
                //replace
                if (!ro)
                    if (ranges.Count > 0)
                    {
                        _tb.TextSource.Manager.ExecuteCommand(new ReplaceTextCommand(_tb.TextSource, ranges,
                            tbReplace.Text));
                        _tb.Selection.Start = new Place(0, 0);
                    }
                //
                _tb.Invalidate();
                MessageBox.Show(ranges.Count + " occurrence(s) replaced");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            _tb.Selection.EndUpdate();
        }

        protected override void OnActivated(EventArgs e)
        {
            tbFind.Focus();
            ResetSerach();
        }

        private void ResetSerach()
        {
            _firstSearch = true;
        }

        private void cbMatchCase_CheckedChanged(object sender, EventArgs e)
        {
            ResetSerach();
        }
    }
}