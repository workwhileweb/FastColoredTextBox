﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Popup menu for autocomplete
    /// </summary>
    [Browsable(false)]
    public class AutocompleteMenu : ToolStripDropDown
    {
        public ToolStripControlHost Host;

        public AutocompleteMenu(FastColoredTextBox tb)
        {
            // create a new popup and add the list view to it 
            AutoClose = false;
            AutoSize = false;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            BackColor = Color.White;
            Items = new AutocompleteListView(tb);
            Host = new ToolStripControlHost(Items);
            Host.Margin = new Padding(2, 2, 2, 2);
            Host.Padding = Padding.Empty;
            Host.AutoSize = false;
            Host.AutoToolTip = false;
            CalcSize();
            base.Items.Add(Host);
            Items.Parent = this;
            SearchPattern = @"[\w\.]";
            MinFragmentLength = 2;
        }

        public Range Fragment { get; internal set; }

        /// <summary>
        ///     Regex pattern for serach fragment around caret
        /// </summary>
        public string SearchPattern { get; set; }

        /// <summary>
        ///     Minimum fragment length for popup
        /// </summary>
        public int MinFragmentLength { get; set; }

        /// <summary>
        ///     Allow TAB for select menu item
        /// </summary>
        public bool AllowTabKey
        {
            get { return Items.AllowTabKey; }
            set { Items.AllowTabKey = value; }
        }

        /// <summary>
        ///     Interval of menu appear (ms)
        /// </summary>
        public int AppearInterval
        {
            get { return Items.AppearInterval; }
            set { Items.AppearInterval = value; }
        }

        /// <summary>
        ///     Back color of selected item
        /// </summary>
        [DefaultValue(typeof (Color), "Orange")]
        public Color SelectedColor
        {
            get { return Items.SelectedColor; }
            set { Items.SelectedColor = value; }
        }

        /// <summary>
        ///     Border color of hovered item
        /// </summary>
        [DefaultValue(typeof (Color), "Red")]
        public Color HoveredColor
        {
            get { return Items.HoveredColor; }
            set { Items.HoveredColor = value; }
        }

        public new Font Font
        {
            get { return Items.Font; }
            set { Items.Font = value; }
        }

        public new AutocompleteListView Items { get; }

        /// <summary>
        ///     Minimal size of menu
        /// </summary>
        public new Size MinimumSize
        {
            get { return Items.MinimumSize; }
            set { Items.MinimumSize = value; }
        }

        /// <summary>
        ///     Image list of menu
        /// </summary>
        public new ImageList ImageList
        {
            get { return Items.ImageList; }
            set { Items.ImageList = value; }
        }

        /// <summary>
        ///     Tooltip duration (ms)
        /// </summary>
        public int ToolTipDuration
        {
            get { return Items.ToolTipDuration; }
            set { Items.ToolTipDuration = value; }
        }

        /// <summary>
        ///     Tooltip
        /// </summary>
        public ToolTip ToolTip
        {
            get { return Items.ToolTip; }
            set { Items.ToolTip = value; }
        }

        /// <summary>
        ///     User selects item
        /// </summary>
        public event EventHandler<SelectingEventArgs> Selecting;

        /// <summary>
        ///     It fires after item inserting
        /// </summary>
        public event EventHandler<SelectedEventArgs> Selected;

        /// <summary>
        ///     Occurs when popup menu is opening
        /// </summary>
        public new event EventHandler<CancelEventArgs> Opening;

        internal new void OnOpening(CancelEventArgs args)
        {
            if (Opening != null)
                Opening(this, args);
        }

        public new void Close()
        {
            Items.ToolTip.Hide(Items);
            base.Close();
        }

        internal void CalcSize()
        {
            Host.Size = Items.Size;
            Size = new Size(Items.Size.Width + 4, Items.Size.Height + 4);
        }

        public virtual void OnSelecting()
        {
            Items.OnSelecting();
        }

        public void SelectNext(int shift)
        {
            Items.SelectNext(shift);
        }

        internal void OnSelecting(SelectingEventArgs args)
        {
            if (Selecting != null)
                Selecting(this, args);
        }

        public void OnSelected(SelectedEventArgs args)
        {
            if (Selected != null)
                Selected(this, args);
        }

        /// <summary>
        ///     Shows popup menu immediately
        /// </summary>
        /// <param name="forced">If True - MinFragmentLength will be ignored</param>
        public void Show(bool forced)
        {
            Items.DoAutocomplete(forced);
        }
    }

    [ToolboxItem(false)]
    public class AutocompleteListView : UserControl
    {
        private int _focussedItemIndex;
        private readonly int _hoveredItemIndex = -1;
        private int _oldItemCount;
        private IEnumerable<AutocompleteItem> _sourceItems = new List<AutocompleteItem>();
        private readonly FastColoredTextBox _tb;
        private readonly Timer _timer = new Timer();
        internal ToolTip ToolTip = new ToolTip();

        internal List<AutocompleteItem> VisibleItems;

        internal AutocompleteListView(FastColoredTextBox tb)
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Font = new Font(FontFamily.GenericSansSerif, 9);
            VisibleItems = new List<AutocompleteItem>();
            VerticalScroll.SmallChange = ItemHeight;
            MaximumSize = new Size(Size.Width, 180);
            ToolTip.ShowAlways = false;
            AppearInterval = 500;
            _timer.Tick += timer_Tick;
            SelectedColor = Color.Orange;
            HoveredColor = Color.Red;
            ToolTipDuration = 3000;

            _tb = tb;

            tb.KeyDown += tb_KeyDown;
            tb.SelectionChanged += tb_SelectionChanged;
            tb.KeyPressed += tb_KeyPressed;

            var form = tb.FindForm();
            if (form != null)
            {
                form.LocationChanged += (o, e) => Menu.Close();
                form.ResizeBegin += (o, e) => Menu.Close();
                form.FormClosing += (o, e) => Menu.Close();
                form.LostFocus += (o, e) => Menu.Close();
            }

            tb.LostFocus += (o, e) => { if (!Menu.Focused) Menu.Close(); };

            tb.Scroll += (o, e) => Menu.Close();

            VisibleChanged += (o, e) =>
            {
                if (Visible)
                    DoSelectedVisible();
            };
        }

        private int ItemHeight
        {
            get { return Font.Height + 2; }
        }

        private AutocompleteMenu Menu
        {
            get { return Parent as AutocompleteMenu; }
        }

        internal bool AllowTabKey { get; set; }
        public ImageList ImageList { get; set; }

        internal int AppearInterval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        internal int ToolTipDuration { get; set; }

        public Color SelectedColor { get; set; }
        public Color HoveredColor { get; set; }

        public int FocussedItemIndex
        {
            get { return _focussedItemIndex; }
            set
            {
                if (_focussedItemIndex != value)
                {
                    _focussedItemIndex = value;
                    if (FocussedItemIndexChanged != null)
                        FocussedItemIndexChanged(this, EventArgs.Empty);
                }
            }
        }

        public AutocompleteItem FocussedItem
        {
            get
            {
                if (FocussedItemIndex >= 0 && _focussedItemIndex < VisibleItems.Count)
                    return VisibleItems[_focussedItemIndex];
                return null;
            }
            set { FocussedItemIndex = VisibleItems.IndexOf(value); }
        }

        public int Count
        {
            get { return VisibleItems.Count; }
        }

        public event EventHandler FocussedItemIndexChanged;

        private void tb_KeyPressed(object sender, KeyPressEventArgs e)
        {
            var backspaceORdel = e.KeyChar == '\b' || e.KeyChar == 0xff;

            /*
            if (backspaceORdel)
                prevSelection = tb.Selection.Start;*/

            if (Menu.Visible && !backspaceORdel)
                DoAutocomplete(false);
            else
                ResetTimer(_timer);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            DoAutocomplete(false);
        }

        private void ResetTimer(Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        internal void DoAutocomplete()
        {
            DoAutocomplete(false);
        }

        internal void DoAutocomplete(bool forced)
        {
            if (!Menu.Enabled)
            {
                Menu.Close();
                return;
            }

            VisibleItems.Clear();
            FocussedItemIndex = 0;
            VerticalScroll.Value = 0;
            //some magic for update scrolls
            AutoScrollMinSize -= new Size(1, 0);
            AutoScrollMinSize += new Size(1, 0);
            //get fragment around caret
            var fragment = _tb.Selection.GetFragment(Menu.SearchPattern);
            var text = fragment.Text;
            //calc screen point for popup menu
            var point = _tb.PlaceToPoint(fragment.End);
            point.Offset(2, _tb.CharHeight);
            //
            if (forced || (text.Length >= Menu.MinFragmentLength
                           && _tb.Selection.IsEmpty /*pops up only if selected range is empty*/
                           &&
                           (_tb.Selection.Start > fragment.Start || text.Length == 0
                               /*pops up only if caret is after first letter*/)))
            {
                Menu.Fragment = fragment;
                var foundSelected = false;
                //build popup menu
                foreach (var item in _sourceItems)
                {
                    item.Parent = Menu;
                    var res = item.Compare(text);
                    if (res != CompareResult.Hidden)
                        VisibleItems.Add(item);
                    if (res == CompareResult.VisibleAndSelected && !foundSelected)
                    {
                        foundSelected = true;
                        FocussedItemIndex = VisibleItems.Count - 1;
                    }
                }

                if (foundSelected)
                {
                    AdjustScroll();
                    DoSelectedVisible();
                }
            }

            //show popup menu
            if (Count > 0)
            {
                if (!Menu.Visible)
                {
                    var args = new CancelEventArgs();
                    Menu.OnOpening(args);
                    if (!args.Cancel)
                        Menu.Show(_tb, point);
                }
                else
                    Invalidate();
            }
            else
                Menu.Close();
        }

        private void tb_SelectionChanged(object sender, EventArgs e)
        {
            /*
            FastColoredTextBox tb = sender as FastColoredTextBox;
            
            if (Math.Abs(prevSelection.iChar - tb.Selection.Start.iChar) > 1 ||
                        prevSelection.iLine != tb.Selection.Start.iLine)
                Menu.Close();
            prevSelection = tb.Selection.Start;*/
            if (Menu.Visible)
            {
                var needClose = false;

                if (!_tb.Selection.IsEmpty)
                    needClose = true;
                else if (!Menu.Fragment.Contains(_tb.Selection.Start))
                {
                    if (_tb.Selection.Start.ILine == Menu.Fragment.End.ILine &&
                        _tb.Selection.Start.IChar == Menu.Fragment.End.IChar + 1)
                    {
                        //user press key at end of fragment
                        var c = _tb.Selection.CharBeforeStart;
                        if (!Regex.IsMatch(c.ToString(), Menu.SearchPattern)) //check char
                            needClose = true;
                    }
                    else
                        needClose = true;
                }

                if (needClose)
                    Menu.Close();
            }
        }

        private void tb_KeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as FastColoredTextBox;

            if (Menu.Visible)
                if (ProcessKey(e.KeyCode, e.Modifiers))
                    e.Handled = true;

            if (!Menu.Visible)
            {
                if (tb.HotkeysMapping.ContainsKey(e.KeyData) &&
                    tb.HotkeysMapping[e.KeyData] == FctbAction.AutocompleteMenu)
                {
                    DoAutocomplete();
                    e.Handled = true;
                }
                else
                {
                    if (e.KeyCode == Keys.Escape && _timer.Enabled)
                        _timer.Stop();
                }
            }
        }

        private void AdjustScroll()
        {
            if (_oldItemCount == VisibleItems.Count)
                return;

            var needHeight = ItemHeight*VisibleItems.Count + 1;
            Height = Math.Min(needHeight, MaximumSize.Height);
            Menu.CalcSize();

            AutoScrollMinSize = new Size(0, needHeight);
            _oldItemCount = VisibleItems.Count;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            AdjustScroll();

            var itemHeight = ItemHeight;
            var startI = VerticalScroll.Value/itemHeight - 1;
            var finishI = (VerticalScroll.Value + ClientSize.Height)/itemHeight + 1;
            startI = Math.Max(startI, 0);
            finishI = Math.Min(finishI, VisibleItems.Count);
            var y = 0;
            var leftPadding = 18;
            for (var i = startI; i < finishI; i++)
            {
                y = i*itemHeight - VerticalScroll.Value;

                var item = VisibleItems[i];

                if (item.BackColor != Color.Transparent)
                    using (var brush = new SolidBrush(item.BackColor))
                        e.Graphics.FillRectangle(brush, 1, y, ClientSize.Width - 1 - 1, itemHeight - 1);

                if (ImageList != null && VisibleItems[i].ImageIndex >= 0)
                    e.Graphics.DrawImage(ImageList.Images[item.ImageIndex], 1, y);

                if (i == FocussedItemIndex)
                    using (
                        var selectedBrush = new LinearGradientBrush(new Point(0, y - 3), new Point(0, y + itemHeight),
                            Color.Transparent, SelectedColor))
                    using (var pen = new Pen(SelectedColor))
                    {
                        e.Graphics.FillRectangle(selectedBrush, leftPadding, y, ClientSize.Width - 1 - leftPadding,
                            itemHeight - 1);
                        e.Graphics.DrawRectangle(pen, leftPadding, y, ClientSize.Width - 1 - leftPadding, itemHeight - 1);
                    }

                if (i == _hoveredItemIndex)
                    using (var pen = new Pen(HoveredColor))
                        e.Graphics.DrawRectangle(pen, leftPadding, y, ClientSize.Width - 1 - leftPadding, itemHeight - 1);

                using (var brush = new SolidBrush(item.ForeColor != Color.Transparent ? item.ForeColor : ForeColor))
                    e.Graphics.DrawString(item.ToString(), Font, brush, leftPadding, y);
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == MouseButtons.Left)
            {
                FocussedItemIndex = PointToItemIndex(e.Location);
                DoSelectedVisible();
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            FocussedItemIndex = PointToItemIndex(e.Location);
            Invalidate();
            OnSelecting();
        }

        internal virtual void OnSelecting()
        {
            if (FocussedItemIndex < 0 || FocussedItemIndex >= VisibleItems.Count)
                return;
            _tb.TextSource.Manager.BeginAutoUndoCommands();
            try
            {
                var item = FocussedItem;
                var args = new SelectingEventArgs
                {
                    Item = item,
                    SelectedIndex = FocussedItemIndex
                };

                Menu.OnSelecting(args);

                if (args.Cancel)
                {
                    FocussedItemIndex = args.SelectedIndex;
                    Invalidate();
                    return;
                }

                if (!args.Handled)
                {
                    var fragment = Menu.Fragment;
                    DoAutocomplete(item, fragment);
                }

                Menu.Close();
                //
                var args2 = new SelectedEventArgs
                {
                    Item = item,
                    Tb = Menu.Fragment.Tb
                };
                item.OnSelected(Menu, args2);
                Menu.OnSelected(args2);
            }
            finally
            {
                _tb.TextSource.Manager.EndAutoUndoCommands();
            }
        }

        private void DoAutocomplete(AutocompleteItem item, Range fragment)
        {
            var newText = item.GetTextForReplace();

            //replace text of fragment
            var tb = fragment.Tb;

            tb.BeginAutoUndo();
            tb.TextSource.Manager.ExecuteCommand(new SelectCommand(tb.TextSource));
            if (tb.Selection.ColumnSelectionMode)
            {
                var start = tb.Selection.Start;
                var end = tb.Selection.End;
                start.IChar = fragment.Start.IChar;
                end.IChar = fragment.End.IChar;
                tb.Selection.Start = start;
                tb.Selection.End = end;
            }
            else
            {
                tb.Selection.Start = fragment.Start;
                tb.Selection.End = fragment.End;
            }
            tb.InsertText(newText);
            tb.TextSource.Manager.ExecuteCommand(new SelectCommand(tb.TextSource));
            tb.EndAutoUndo();
            tb.Focus();
        }

        private int PointToItemIndex(Point p)
        {
            return (p.Y + VerticalScroll.Value)/ItemHeight;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ProcessKey(keyData, Keys.None);

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool ProcessKey(Keys keyData, Keys keyModifiers)
        {
            if (keyModifiers == Keys.None)
                switch (keyData)
                {
                    case Keys.Down:
                        SelectNext(+1);
                        return true;
                    case Keys.PageDown:
                        SelectNext(+10);
                        return true;
                    case Keys.Up:
                        SelectNext(-1);
                        return true;
                    case Keys.PageUp:
                        SelectNext(-10);
                        return true;
                    case Keys.Enter:
                        OnSelecting();
                        return true;
                    case Keys.Tab:
                        if (!AllowTabKey)
                            break;
                        OnSelecting();
                        return true;
                    case Keys.Escape:
                        Menu.Close();
                        return true;
                }

            return false;
        }

        public void SelectNext(int shift)
        {
            FocussedItemIndex = Math.Max(0, Math.Min(FocussedItemIndex + shift, VisibleItems.Count - 1));
            DoSelectedVisible();
            //
            Invalidate();
        }

        private void DoSelectedVisible()
        {
            if (FocussedItem != null)
                SetToolTip(FocussedItem);

            var y = FocussedItemIndex*ItemHeight - VerticalScroll.Value;
            if (y < 0)
                VerticalScroll.Value = FocussedItemIndex*ItemHeight;
            if (y > ClientSize.Height - ItemHeight)
                VerticalScroll.Value = Math.Min(VerticalScroll.Maximum,
                    FocussedItemIndex*ItemHeight - ClientSize.Height + ItemHeight);
            //some magic for update scrolls
            AutoScrollMinSize -= new Size(1, 0);
            AutoScrollMinSize += new Size(1, 0);
        }

        private void SetToolTip(AutocompleteItem autocompleteItem)
        {
            var title = autocompleteItem.ToolTipTitle;
            var text = autocompleteItem.ToolTipText;

            if (string.IsNullOrEmpty(title))
            {
                ToolTip.ToolTipTitle = null;
                ToolTip.SetToolTip(this, null);
                return;
            }

            IWin32Window window = Parent ?? this;
            var location = new Point((window == this ? Width : Right) + 3, 0);

            if (string.IsNullOrEmpty(text))
            {
                ToolTip.ToolTipTitle = null;
                ToolTip.Show(title, window, location.X, location.Y, ToolTipDuration);
            }
            else
            {
                ToolTip.ToolTipTitle = title;
                ToolTip.Show(text, window, location.X, location.Y, ToolTipDuration);
            }
        }

        public void SetAutocompleteItems(ICollection<string> items)
        {
            var list = new List<AutocompleteItem>(items.Count);
            foreach (var item in items)
                list.Add(new AutocompleteItem(item));
            SetAutocompleteItems(list);
        }

        public void SetAutocompleteItems(IEnumerable<AutocompleteItem> items)
        {
            _sourceItems = items;
        }
    }

    public class SelectingEventArgs : EventArgs
    {
        public AutocompleteItem Item { get; internal set; }
        public bool Cancel { get; set; }
        public int SelectedIndex { get; set; }
        public bool Handled { get; set; }
    }

    public class SelectedEventArgs : EventArgs
    {
        public AutocompleteItem Item { get; internal set; }
        public FastColoredTextBox Tb { get; set; }
    }
}