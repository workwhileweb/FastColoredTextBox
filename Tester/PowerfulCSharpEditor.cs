using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FarsiLibrary.Win;
using FastColoredTextBoxNS;
using Tester.Properties;

namespace Tester
{
    public partial class PowerfulCSharpEditor : Form
    {
        private readonly Color _changedLineColor = Color.FromArgb(255, 230, 230, 255);
        private readonly Color _currentLineColor = Color.FromArgb(100, 210, 210, 255);

        private readonly string[] _declarationSnippets =
        {
            "public class ^\n{\n}", "private class ^\n{\n}", "internal class ^\n{\n}",
            "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
            "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}",
            "protected void ^()\n{\n;\n}",
            "public ^{ get; set; }", "private ^{ get; set; }", "internal ^{ get; set; }", "protected ^{ get; set; }"
        };

        private List<ExplorerItem> _explorerList = new List<ExplorerItem>();
        private readonly Style _invisibleCharsStyle = new InvisibleCharsRenderer(Pens.Gray);

        private readonly string[] _keywords =
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return",
            "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while", "add", "alias", "ascending", "descending", "dynamic", "from", "get", "global", "group",
            "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield"
        };

        private DateTime _lastNavigatedDateTime = DateTime.Now;
        private readonly string[] _methods = {"Equals()", "GetHashCode()", "GetType()", "ToString()"};

        private readonly Style _sameWordsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(50, Color.Gray)));

        private readonly string[] _snippets =
        {
            "if(^)\n{\n;\n}", "if(^)\n{\n;\n}\nelse\n{\n;\n}", "for(^;;)\n{\n;\n}",
            "while(^)\n{\n;\n}", "do\n{\n^;\n}while();", "switch(^)\n{\ncase : break;\n}"
        };

        private bool _tbFindChanged;


        public PowerfulCSharpEditor()
        {
            InitializeComponent();

            //init menu images
            var resources = new ComponentResourceManager(typeof (PowerfulCSharpEditor));
            copyToolStripMenuItem.Image = ((Image) (resources.GetObject("copyToolStripButton.Image")));
            cutToolStripMenuItem.Image = ((Image) (resources.GetObject("cutToolStripButton.Image")));
            pasteToolStripMenuItem.Image = ((Image) (resources.GetObject("pasteToolStripButton.Image")));
        }

        private FastColoredTextBox CurrentTb
        {
            get
            {
                if (tsFiles.SelectedItem == null)
                    return null;
                return (tsFiles.SelectedItem.Controls[0] as FastColoredTextBox);
            }

            set
            {
                tsFiles.SelectedItem = (value.Parent as FATabStripItem);
                value.Focus();
            }
        }


        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateTab(null);
        }

        private void CreateTab(string fileName)
        {
            try
            {
                var tb = new FastColoredTextBox();
                tb.Font = new Font("Consolas", 9.75f);
                tb.ContextMenuStrip = cmMain;
                tb.Dock = DockStyle.Fill;
                tb.BorderStyle = BorderStyle.Fixed3D;
                //tb.VirtualSpace = true;
                tb.LeftPadding = 17;
                tb.Language = Language.CSharp;
                tb.AddStyle(_sameWordsStyle); //same words style
                var tab = new FATabStripItem(fileName != null ? Path.GetFileName(fileName) : "[new]", tb);
                tab.Tag = fileName;
                if (fileName != null)
                    tb.OpenFile(fileName);
                tb.Tag = new TbInfo();
                tsFiles.AddTab(tab);
                tsFiles.SelectedItem = tab;
                tb.Focus();
                tb.DelayedTextChangedInterval = 1000;
                tb.DelayedEventsInterval = 500;
                tb.TextChangedDelayed += tb_TextChangedDelayed;
                tb.SelectionChangedDelayed += tb_SelectionChangedDelayed;
                tb.KeyDown += tb_KeyDown;
                tb.MouseMove += tb_MouseMove;
                tb.ChangedLineColor = _changedLineColor;
                if (btHighlightCurrentLine.Checked)
                    tb.CurrentLineColor = _currentLineColor;
                tb.ShowFoldingLines = btShowFoldingLines.Checked;
                tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
                //create autocomplete popup menu
                var popupMenu = new AutocompleteMenu(tb);
                popupMenu.Items.ImageList = ilAutocomplete;
                popupMenu.Opening += popupMenu_Opening;
                BuildAutocompleteMenu(popupMenu);
                (tb.Tag as TbInfo).PopupMenu = popupMenu;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                    DialogResult.Retry)
                    CreateTab(fileName);
            }
        }

        private void popupMenu_Opening(object sender, CancelEventArgs e)
        {
            //---block autocomplete menu for comments
            //get index of green style (used for comments)
            var iGreenStyle = CurrentTb.GetStyleIndex(CurrentTb.SyntaxHighlighter.GreenStyle);
            if (iGreenStyle >= 0)
                if (CurrentTb.Selection.Start.IChar > 0)
                {
                    //current char (before caret)
                    var c = CurrentTb[CurrentTb.Selection.Start.ILine][CurrentTb.Selection.Start.IChar - 1];
                    //green Style
                    var greenStyleIndex = Range.ToStyleIndex(iGreenStyle);
                    //if char contains green style then block popup menu
                    if ((c.Style & greenStyleIndex) != 0)
                        e.Cancel = true;
                }
        }

        private void BuildAutocompleteMenu(AutocompleteMenu popupMenu)
        {
            var items = new List<AutocompleteItem>();

            foreach (var item in _snippets)
                items.Add(new SnippetAutocompleteItem(item) {ImageIndex = 1});
            foreach (var item in _declarationSnippets)
                items.Add(new DeclarationSnippet(item) {ImageIndex = 0});
            foreach (var item in _methods)
                items.Add(new MethodAutocompleteItem(item) {ImageIndex = 2});
            foreach (var item in _keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            //set as autocomplete source
            popupMenu.Items.SetAutocompleteItems(items);
            popupMenu.SearchPattern = @"[\w\.:=!<>]";
        }

        private void tb_MouseMove(object sender, MouseEventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            var place = tb.PointToPlace(e.Location);
            var r = new Range(tb, place, place);

            var text = r.GetFragment("[a-zA-Z]").Text;
            lbWordUnderMouse.Text = text;
        }

        private void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.OemMinus)
            {
                NavigateBackward();
                e.Handled = true;
            }

            if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.OemMinus)
            {
                NavigateForward();
                e.Handled = true;
            }

            if (e.KeyData == (Keys.K | Keys.Control))
            {
                //forced show (MinFragmentLength will be ignored)
                (CurrentTb.Tag as TbInfo).PopupMenu.Show(true);
                e.Handled = true;
            }
        }

        private void tb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            //remember last visit time
            if (tb.Selection.IsEmpty && tb.Selection.Start.ILine < tb.LinesCount)
            {
                if (_lastNavigatedDateTime != tb[tb.Selection.Start.ILine].LastVisit)
                {
                    tb[tb.Selection.Start.ILine].LastVisit = DateTime.Now;
                    _lastNavigatedDateTime = tb[tb.Selection.Start.ILine].LastVisit;
                }
            }

            //highlight same words
            tb.VisibleRange.ClearStyle(_sameWordsStyle);
            if (!tb.Selection.IsEmpty)
                return; //user selected diapason
            //get fragment around caret
            var fragment = tb.Selection.GetFragment(@"\w");
            var text = fragment.Text;
            if (text.Length == 0)
                return;
            //highlight same words
            var ranges = tb.VisibleRange.GetRanges("\\b" + text + "\\b").ToArray();

            if (ranges.Length > 1)
                foreach (var r in ranges)
                    r.SetStyle(_sameWordsStyle);
        }

        private void tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            var tb = (sender as FastColoredTextBox);
            //rebuild object explorer
            var text = (sender as FastColoredTextBox).Text;
            ThreadPool.QueueUserWorkItem(
                o => ReBuildObjectExplorer(text)
                );

            //show invisible chars
            HighlightInvisibleChars(e.ChangedRange);
        }

        private void HighlightInvisibleChars(Range range)
        {
            range.ClearStyle(_invisibleCharsStyle);
            if (btInvisibleChars.Checked)
                range.SetStyle(_invisibleCharsStyle, @".$|.\r\n|\s");
        }

        private void ReBuildObjectExplorer(string text)
        {
            try
            {
                var list = new List<ExplorerItem>();
                var lastClassIndex = -1;
                //find classes, methods and properties
                var regex =
                    new Regex(
                        @"^(?<range>[\w\s]+\b(class|struct|enum|interface)\s+[\w<>,\s]+)|^\s*(public|private|internal|protected)[^\n]+(\n?\s*{|;)?",
                        RegexOptions.Multiline);
                foreach (Match r in regex.Matches(text))
                    try
                    {
                        var s = r.Value;
                        var i = s.IndexOfAny(new[] {'=', '{', ';'});
                        if (i >= 0)
                            s = s.Substring(0, i);
                        s = s.Trim();

                        var item = new ExplorerItem {Title = s, Position = r.Index};
                        if (Regex.IsMatch(item.Title, @"\b(class|struct|enum|interface)\b"))
                        {
                            item.Title = item.Title.Substring(item.Title.LastIndexOf(' ')).Trim();
                            item.Type = ExplorerItemType.Class;
                            list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());
                            lastClassIndex = list.Count;
                        }
                        else if (item.Title.Contains(" event "))
                        {
                            var ii = item.Title.LastIndexOf(' ');
                            item.Title = item.Title.Substring(ii).Trim();
                            item.Type = ExplorerItemType.Event;
                        }
                        else if (item.Title.Contains("("))
                        {
                            var parts = item.Title.Split('(');
                            item.Title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "(" + parts[1];
                            item.Type = ExplorerItemType.Method;
                        }
                        else if (item.Title.EndsWith("]"))
                        {
                            var parts = item.Title.Split('[');
                            if (parts.Length < 2) continue;
                            item.Title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "[" + parts[1];
                            item.Type = ExplorerItemType.Method;
                        }
                        else
                        {
                            var ii = item.Title.LastIndexOf(' ');
                            item.Title = item.Title.Substring(ii).Trim();
                            item.Type = ExplorerItemType.Property;
                        }
                        list.Add(item);
                    }
                    catch
                    {
                        ;
                    }

                list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());

                BeginInvoke(
                    new Action(() =>
                    {
                        _explorerList = list;
                        dgvObjectExplorer.RowCount = _explorerList.Count;
                        dgvObjectExplorer.Invalidate();
                    })
                    );
            }
            catch
            {
                ;
            }
        }

        private void tsFiles_TabStripItemClosing(TabStripItemClosingEventArgs e)
        {
            if ((e.Item.Controls[0] as FastColoredTextBox).IsChanged)
            {
                switch (
                    MessageBox.Show("Do you want save " + e.Item.Title + " ?", "Save", MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Information))
                {
                    case DialogResult.Yes:
                        if (!Save(e.Item))
                            e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private bool Save(FATabStripItem tab)
        {
            var tb = (tab.Controls[0] as FastColoredTextBox);
            if (tab.Tag == null)
            {
                if (sfdMain.ShowDialog() != DialogResult.OK)
                    return false;
                tab.Title = Path.GetFileName(sfdMain.FileName);
                tab.Tag = sfdMain.FileName;
            }

            try
            {
                File.WriteAllText(tab.Tag as string, tb.Text);
                tb.IsChanged = false;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                    DialogResult.Retry)
                    return Save(tab);
                return false;
            }

            tb.Invalidate();

            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
                Save(tsFiles.SelectedItem);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
            {
                var oldFile = tsFiles.SelectedItem.Tag as string;
                tsFiles.SelectedItem.Tag = null;
                if (!Save(tsFiles.SelectedItem))
                    if (oldFile != null)
                    {
                        tsFiles.SelectedItem.Tag = oldFile;
                        tsFiles.SelectedItem.Title = Path.GetFileName(oldFile);
                    }
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdMain.ShowDialog() == DialogResult.OK)
                CreateTab(ofdMain.FileName);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.Selection.SelectAll();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTb.UndoEnabled)
                CurrentTb.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTb.RedoEnabled)
                CurrentTb.Redo();
        }

        private void tmUpdateInterface_Tick(object sender, EventArgs e)
        {
            try
            {
                if (CurrentTb != null && tsFiles.Items.Count > 0)
                {
                    var tb = CurrentTb;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = tb.UndoEnabled;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = tb.RedoEnabled;
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = tb.IsChanged;
                    saveAsToolStripMenuItem.Enabled = true;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = true;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                        copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = !tb.Selection.IsEmpty;
                    printToolStripButton.Enabled = true;
                }
                else
                {
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                        copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = false;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = false;
                    printToolStripButton.Enabled = false;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = false;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = false;
                    dgvObjectExplorer.RowCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTb != null)
            {
                var settings = new PrintDialogSettings();
                settings.Title = tsFiles.SelectedItem.Title;
                settings.Header = "&b&w&b";
                settings.Footer = "&b&p";
                CurrentTb.Print(settings);
            }
        }

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && CurrentTb != null)
            {
                var r = _tbFindChanged ? CurrentTb.Range.Clone() : CurrentTb.Selection.Clone();
                _tbFindChanged = false;
                r.End = new Place(CurrentTb[CurrentTb.LinesCount - 1].Count, CurrentTb.LinesCount - 1);
                var pattern = Regex.Escape(tbFind.Text);
                foreach (var found in r.GetRanges(pattern))
                {
                    found.Inverse();
                    CurrentTb.Selection = found;
                    CurrentTb.DoSelectionVisible();
                    return;
                }
                MessageBox.Show("Not found.");
            }
            else
                _tbFindChanged = true;
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.ShowFindDialog();
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.ShowReplaceDialog();
        }

        private void PowerfulCSharpEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            var list = new List<FATabStripItem>();
            foreach (FATabStripItem tab in  tsFiles.Items)
                list.Add(tab);
            foreach (var tab in list)
            {
                var args = new TabStripItemClosingEventArgs(tab);
                tsFiles_TabStripItemClosing(args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                tsFiles.RemoveTab(tab);
            }
        }

        private void dgvObjectExplorer_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (CurrentTb != null)
            {
                var item = _explorerList[e.RowIndex];
                CurrentTb.GoEnd();
                CurrentTb.SelectionStart = item.Position;
                CurrentTb.DoSelectionVisible();
                CurrentTb.Focus();
            }
        }

        private void dgvObjectExplorer_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                var item = _explorerList[e.RowIndex];
                if (e.ColumnIndex == 1)
                    e.Value = item.Title;
                else
                    switch (item.Type)
                    {
                        case ExplorerItemType.Class:
                            e.Value = Resources.class_libraries;
                            return;
                        case ExplorerItemType.Method:
                            e.Value = Resources.box;
                            return;
                        case ExplorerItemType.Event:
                            e.Value = Resources.lightning;
                            return;
                        case ExplorerItemType.Property:
                            e.Value = Resources.property;
                            return;
                    }
            }
            catch
            {
                ;
            }
        }

        private void tsFiles_TabStripItemSelectionChanged(TabStripItemChangedEventArgs e)
        {
            if (CurrentTb != null)
            {
                CurrentTb.Focus();
                var text = CurrentTb.Text;
                ThreadPool.QueueUserWorkItem(
                    o => ReBuildObjectExplorer(text)
                    );
            }
        }

        private void backStripButton_Click(object sender, EventArgs e)
        {
            NavigateBackward();
        }

        private void forwardStripButton_Click(object sender, EventArgs e)
        {
            NavigateForward();
        }

        private bool NavigateBackward()
        {
            var max = new DateTime();
            var iLine = -1;
            FastColoredTextBox tb = null;
            for (var iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (var i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit < _lastNavigatedDateTime && t[i].LastVisit > max)
                    {
                        max = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                _lastNavigatedDateTime = tb[iLine].LastVisit;
                Console.WriteLine("Backward: " + _lastNavigatedDateTime);
                tb.Focus();
                tb.Invalidate();
                return true;
            }
            return false;
        }

        private bool NavigateForward()
        {
            var min = DateTime.Now;
            var iLine = -1;
            FastColoredTextBox tb = null;
            for (var iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (var i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit > _lastNavigatedDateTime && t[i].LastVisit < min)
                    {
                        min = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                _lastNavigatedDateTime = tb[iLine].LastVisit;
                Console.WriteLine("Forward: " + _lastNavigatedDateTime);
                tb.Focus();
                tb.Invalidate();
                return true;
            }
            return false;
        }

        private void autoIndentSelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.DoAutoIndent();
        }

        private void btInvisibleChars_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                HighlightInvisibleChars((tab.Controls[0] as FastColoredTextBox).Range);
            if (CurrentTb != null)
                CurrentTb.Invalidate();
        }

        private void btHighlightCurrentLine_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
            {
                if (btHighlightCurrentLine.Checked)
                    (tab.Controls[0] as FastColoredTextBox).CurrentLineColor = _currentLineColor;
                else
                    (tab.Controls[0] as FastColoredTextBox).CurrentLineColor = Color.Transparent;
            }
            if (CurrentTb != null)
                CurrentTb.Invalidate();
        }

        private void commentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.InsertLinePrefix("//");
        }

        private void uncommentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTb.RemoveLinePrefix("//");
        }

        private void cloneLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //expand selection
            CurrentTb.Selection.Expand();
            //get text of selected lines
            var text = Environment.NewLine + CurrentTb.Selection.Text;
            //move caret to end of selected lines
            CurrentTb.Selection.Start = CurrentTb.Selection.End;
            //insert text
            CurrentTb.InsertText(text);
        }

        private void cloneLinesAndCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //start autoUndo block
            CurrentTb.BeginAutoUndo();
            //expand selection
            CurrentTb.Selection.Expand();
            //get text of selected lines
            var text = Environment.NewLine + CurrentTb.Selection.Text;
            //comment lines
            CurrentTb.InsertLinePrefix("//");
            //move caret to end of selected lines
            CurrentTb.Selection.Start = CurrentTb.Selection.End;
            //insert text
            CurrentTb.InsertText(text);
            //end of autoUndo block
            CurrentTb.EndAutoUndo();
        }

        private void bookmarkPlusButton_Click(object sender, EventArgs e)
        {
            if (CurrentTb == null)
                return;
            CurrentTb.BookmarkLine(CurrentTb.Selection.Start.ILine);
        }

        private void bookmarkMinusButton_Click(object sender, EventArgs e)
        {
            if (CurrentTb == null)
                return;
            CurrentTb.UnbookmarkLine(CurrentTb.Selection.Start.ILine);
        }

        private void gotoButton_DropDownOpening(object sender, EventArgs e)
        {
            gotoButton.DropDownItems.Clear();
            foreach (Control tab in tsFiles.Items)
            {
                var tb = tab.Controls[0] as FastColoredTextBox;
                foreach (var bookmark in tb.Bookmarks)
                {
                    var item =
                        gotoButton.DropDownItems.Add(bookmark.Name + " [" +
                                                     Path.GetFileNameWithoutExtension(tab.Tag as string) + "]");
                    item.Tag = bookmark;
                    item.Click += (o, a) =>
                    {
                        var b = (Bookmark) (o as ToolStripItem).Tag;
                        try
                        {
                            CurrentTb = b.Tb;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            return;
                        }
                        b.DoVisible();
                    };
                }
            }
        }

        private void btShowFoldingLines_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                (tab.Controls[0] as FastColoredTextBox).ShowFoldingLines = btShowFoldingLines.Checked;
            if (CurrentTb != null)
                CurrentTb.Invalidate();
        }

        private void Zoom_click(object sender, EventArgs e)
        {
            if (CurrentTb != null)
                CurrentTb.Zoom = int.Parse((sender as ToolStripItem).Tag.ToString());
        }

        private enum ExplorerItemType
        {
            Class,
            Method,
            Property,
            Event
        }

        private class ExplorerItem
        {
            public int Position;
            public string Title;
            public ExplorerItemType Type;
        }

        private class ExplorerItemComparer : IComparer<ExplorerItem>
        {
            public int Compare(ExplorerItem x, ExplorerItem y)
            {
                return x.Title.CompareTo(y.Title);
            }
        }

        /// <summary>
        ///     This item appears when any part of snippet text is typed
        /// </summary>
        private class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        ///     Divides numbers and words: "123AND456" -> "123 AND 456"
        ///     Or "i=2" -> "i = 2"
        /// </summary>
        private class InsertSpaceSnippet : AutocompleteItem
        {
            private readonly string _pattern;

            public InsertSpaceSnippet(string pattern)
                : base("")
            {
                _pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override string ToolTipTitle
            {
                get { return Text; }
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, _pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, _pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }
        }

        /// <summary>
        ///     Inerts line break after '}'
        /// </summary>
        private class InsertEnterSnippet : AutocompleteItem
        {
            private Place _enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override string ToolTipTitle
            {
                get { return "Insert line break after '}'"; }
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.IChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        _enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                var r = Parent.Fragment;
                var end = r.End;
                r.Start = _enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.Tb.AutoIndent)
                    Parent.Fragment.Tb.DoAutoIndent();
            }
        }
    }

    public class InvisibleCharsRenderer : Style
    {
        private readonly Pen _pen;

        public InvisibleCharsRenderer(Pen pen)
        {
            _pen = pen;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            var tb = range.Tb;
            using (Brush brush = new SolidBrush(_pen.Color))
                foreach (var place in range)
                {
                    switch (tb[place].C)
                    {
                        case ' ':
                            var point = tb.PlaceToPoint(place);
                            point.Offset(tb.CharWidth/2, tb.CharHeight/2);
                            gr.DrawLine(_pen, point.X, point.Y, point.X + 1, point.Y);
                            break;
                    }

                    if (tb[place.ILine].Count - 1 == place.IChar)
                    {
                        var point = tb.PlaceToPoint(place);
                        point.Offset(tb.CharWidth, 0);
                        gr.DrawString("¶", tb.Font, brush, point);
                    }
                }
        }
    }

    public class TbInfo
    {
        public AutocompleteMenu PopupMenu;
    }
}