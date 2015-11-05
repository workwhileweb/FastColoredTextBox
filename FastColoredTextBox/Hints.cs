using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Collection of Hints.
    ///     This is temporary buffer for currently displayed hints.
    /// </summary>
    public class Hints : ICollection<Hint>, IDisposable
    {
        private readonly List<Hint> _items = new List<Hint>();
        private readonly FastColoredTextBox _tb;

        public Hints(FastColoredTextBox tb)
        {
            _tb = tb;
            tb.TextChanged += OnTextBoxTextChanged;
            tb.KeyDown += OnTextBoxKeyDown;
            tb.VisibleRangeChanged += OnTextBoxVisibleRangeChanged;
        }

        public IEnumerator<Hint> GetEnumerator()
        {
            foreach (var item in _items)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Clears all displayed hints
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            if (_tb.Controls.Count != 0)
            {
                var toDelete = new List<Control>();
                foreach (Control item in _tb.Controls)
                    if (item is UnfocusablePanel)
                        toDelete.Add(item);

                foreach (var item in toDelete)
                    _tb.Controls.Remove(item);

                for (var i = 0; i < _tb.LineInfos.Count; i++)
                {
                    var li = _tb.LineInfos[i];
                    li.BottomPadding = 0;
                    _tb.LineInfos[i] = li;
                }
                _tb.NeedRecalc();
                _tb.Invalidate();
                _tb.Select();
                _tb.ActiveControl = null;
            }
        }

        /// <summary>
        ///     Add and shows the hint
        /// </summary>
        /// <param name="hint"></param>
        public void Add(Hint hint)
        {
            _items.Add(hint);

            if (hint.Inline || hint.Range.Start.ILine >= _tb.LinesCount - 1)
            {
                var li = _tb.LineInfos[hint.Range.Start.ILine];
                hint.TopPadding = li.BottomPadding;
                li.BottomPadding += hint.HostPanel.Height;
                _tb.LineInfos[hint.Range.Start.ILine] = li;
                _tb.NeedRecalc(true);
            }

            LayoutHint(hint);

            _tb.OnVisibleRangeChanged();

            hint.HostPanel.Parent = _tb;

            _tb.Select();
            _tb.ActiveControl = null;
            _tb.Invalidate();
        }

        /// <summary>
        ///     Is collection contains the hint?
        /// </summary>
        public bool Contains(Hint item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(Hint[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Count of hints
        /// </summary>
        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Hint item)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _tb.TextChanged -= OnTextBoxTextChanged;
            _tb.KeyDown -= OnTextBoxKeyDown;
            _tb.VisibleRangeChanged -= OnTextBoxVisibleRangeChanged;
        }

        protected virtual void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && e.Modifiers == Keys.None)
                Clear();
        }

        protected virtual void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            Clear();
        }

        private void OnTextBoxVisibleRangeChanged(object sender, EventArgs e)
        {
            if (_items.Count == 0)
                return;

            _tb.NeedRecalc(true);
            foreach (var item in _items)
            {
                LayoutHint(item);
                item.HostPanel.Invalidate();
            }
        }

        private void LayoutHint(Hint hint)
        {
            if (hint.Inline || hint.Range.Start.ILine >= _tb.LinesCount - 1)
            {
                if (hint.Range.Start.ILine < _tb.LineInfos.Count - 1)
                    hint.HostPanel.Top = _tb.LineInfos[hint.Range.Start.ILine + 1].StartY - hint.TopPadding -
                                         hint.HostPanel.Height - _tb.VerticalScroll.Value;
                else
                    hint.HostPanel.Top = _tb.TextHeight + _tb.Paddings.Top - hint.HostPanel.Height -
                                         _tb.VerticalScroll.Value;
            }
            else
            {
                hint.HostPanel.Top = _tb.LineInfos[hint.Range.Start.ILine + 1].StartY - _tb.VerticalScroll.Value;
            }

            if (hint.Dock == DockStyle.Fill)
            {
                hint.Width = _tb.ClientSize.Width - _tb.LeftIndent - 2;
                hint.HostPanel.Left = _tb.LeftIndent;
            }
            else
            {
                var p1 = _tb.PlaceToPoint(hint.Range.Start);
                var p2 = _tb.PlaceToPoint(hint.Range.End);
                var cx = (p1.X + p2.X)/2;
                hint.HostPanel.Left = Math.Max(_tb.LeftIndent, cx - hint.HostPanel.Width/2);
            }
        }
    }

    /// <summary>
    ///     Hint of FastColoredTextbox
    /// </summary>
    public class Hint
    {
        private Hint(Range range, Control innerControl, string text, bool inline, bool dock)
        {
            Range = range;
            Inline = inline;
            InnerControl = innerControl;

            Init();

            Dock = dock ? DockStyle.Fill : DockStyle.None;
            Text = text;
        }

        /// <summary>
        ///     Creates Hint
        /// </summary>
        /// <param name="range">Linked range</param>
        /// <param name="text">Text for simple hint</param>
        /// <param name="inline">Inlining. If True then hint will moves apart text</param>
        /// <param name="dock">Docking. If True then hint will fill whole line</param>
        public Hint(Range range, string text, bool inline, bool dock)
            : this(range, null, text, inline, dock)
        {
        }

        /// <summary>
        ///     Creates Hint
        /// </summary>
        /// <param name="range">Linked range</param>
        /// <param name="text">Text for simple hint</param>
        public Hint(Range range, string text)
            : this(range, null, text, true, true)
        {
        }

        /// <summary>
        ///     Creates Hint
        /// </summary>
        /// <param name="range">Linked range</param>
        /// <param name="innerControl">Inner control</param>
        /// <param name="inline">Inlining. If True then hint will moves apart text</param>
        /// <param name="dock">Docking. If True then hint will fill whole line</param>
        public Hint(Range range, Control innerControl, bool inline, bool dock)
            : this(range, innerControl, null, inline, dock)
        {
        }

        /// <summary>
        ///     Creates Hint
        /// </summary>
        /// <param name="range">Linked range</param>
        /// <param name="innerControl">Inner control</param>
        public Hint(Range range, Control innerControl)
            : this(range, innerControl, null, true, true)
        {
        }

        /// <summary>
        ///     Text of simple hint
        /// </summary>
        public string Text
        {
            get { return HostPanel.Text; }
            set { HostPanel.Text = value; }
        }

        /// <summary>
        ///     Linked range
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        ///     Backcolor
        /// </summary>
        public Color BackColor
        {
            get { return HostPanel.BackColor; }
            set { HostPanel.BackColor = value; }
        }

        /// <summary>
        ///     Second backcolor
        /// </summary>
        public Color BackColor2
        {
            get { return HostPanel.BackColor2; }
            set { HostPanel.BackColor2 = value; }
        }

        /// <summary>
        ///     Border color
        /// </summary>
        public Color BorderColor
        {
            get { return HostPanel.BorderColor; }
            set { HostPanel.BorderColor = value; }
        }

        /// <summary>
        ///     Fore color
        /// </summary>
        public Color ForeColor
        {
            get { return HostPanel.ForeColor; }
            set { HostPanel.ForeColor = value; }
        }

        /// <summary>
        ///     Text alignment
        /// </summary>
        public StringAlignment TextAlignment
        {
            get { return HostPanel.TextAlignment; }
            set { HostPanel.TextAlignment = value; }
        }

        /// <summary>
        ///     Font
        /// </summary>
        public Font Font
        {
            get { return HostPanel.Font; }
            set { HostPanel.Font = value; }
        }

        /// <summary>
        ///     Inner control
        /// </summary>
        public Control InnerControl { get; set; }

        /// <summary>
        ///     Docking (allows None and Fill only)
        /// </summary>
        public DockStyle Dock { get; set; }

        /// <summary>
        ///     Width of hint (if Dock is None)
        /// </summary>
        public int Width
        {
            get { return HostPanel.Width; }
            set { HostPanel.Width = value; }
        }

        /// <summary>
        ///     Height of hint
        /// </summary>
        public int Height
        {
            get { return HostPanel.Height; }
            set { HostPanel.Height = value; }
        }

        /// <summary>
        ///     Host panel
        /// </summary>
        public UnfocusablePanel HostPanel { get; private set; }

        internal int TopPadding { get; set; }

        /// <summary>
        ///     Tag
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     Cursor
        /// </summary>
        public Cursor Cursor
        {
            get { return HostPanel.Cursor; }
            set { HostPanel.Cursor = value; }
        }

        /// <summary>
        ///     Inlining. If True then hint will moves apart text.
        /// </summary>
        public bool Inline { get; set; }

        /// <summary>
        ///     Occurs when user click on simple hint
        /// </summary>
        public event EventHandler Click
        {
            add { HostPanel.Click += value; }
            remove { HostPanel.Click -= value; }
        }

        /// <summary>
        ///     Scroll textbox to the hint
        /// </summary>
        public virtual void DoVisible()
        {
            Range.Tb.DoRangeVisible(Range, true);
            Range.Tb.Invalidate();
        }

        protected virtual void Init()
        {
            HostPanel = new UnfocusablePanel();
            HostPanel.Click += OnClick;

            if (InnerControl != null)
            {
                HostPanel.Controls.Add(InnerControl);
                HostPanel.Width = InnerControl.Width + 2;
                HostPanel.Height = InnerControl.Height + 2;
                InnerControl.Dock = DockStyle.Fill;
                InnerControl.Visible = true;
                BackColor = SystemColors.Control;
            }
            else
            {
                HostPanel.Height = Range.Tb.CharHeight + 5;
            }

            Cursor = Cursors.Default;
            BorderColor = Color.Silver;
            BackColor2 = Color.White;
            BackColor = InnerControl == null ? Color.Silver : SystemColors.Control;
            ForeColor = Color.Black;
            TextAlignment = StringAlignment.Near;
            Font = Range.Tb.Parent == null ? Range.Tb.Font : Range.Tb.Parent.Font;
        }

        protected virtual void OnClick(object sender, EventArgs e)
        {
            Range.Tb.OnHintClick(this);
        }
    }
}