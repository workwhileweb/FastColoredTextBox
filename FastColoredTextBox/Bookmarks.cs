using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Base class for bookmark collection
    /// </summary>
    public abstract class BaseBookmarks : ICollection<Bookmark>, IDisposable
    {
        #region IDisposable

        public abstract void Dispose();

        #endregion

        #region ICollection

        public abstract void Add(Bookmark item);
        public abstract void Clear();
        public abstract bool Contains(Bookmark item);
        public abstract void CopyTo(Bookmark[] array, int arrayIndex);
        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; }
        public abstract bool Remove(Bookmark item);
        public abstract IEnumerator<Bookmark> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Additional properties

        public abstract void Add(int lineIndex, string bookmarkName);
        public abstract void Add(int lineIndex);
        public abstract bool Contains(int lineIndex);
        public abstract bool Remove(int lineIndex);
        public abstract Bookmark GetBookmark(int i);

        #endregion
    }

    /// <summary>
    ///     Collection of bookmarks
    /// </summary>
    public class Bookmarks : BaseBookmarks
    {
        protected int Counter;
        protected List<Bookmark> Items = new List<Bookmark>();
        protected FastColoredTextBox tb;

        public Bookmarks(FastColoredTextBox tb)
        {
            this.tb = tb;
            tb.LineInserted += tb_LineInserted;
            tb.LineRemoved += tb_LineRemoved;
        }

        public override int Count
        {
            get { return Items.Count; }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        protected virtual void tb_LineRemoved(object sender, LineRemovedEventArgs e)
        {
            for (var i = 0; i < Count; i++)
                if (Items[i].LineIndex >= e.Index)
                {
                    if (Items[i].LineIndex >= e.Index + e.Count)
                    {
                        Items[i].LineIndex = Items[i].LineIndex - e.Count;
                        continue;
                    }

                    var was = e.Index <= 0;
                    foreach (var b in Items)
                        if (b.LineIndex == e.Index - 1)
                            was = true;

                    if (was)
                    {
                        Items.RemoveAt(i);
                        i--;
                    }
                    else
                        Items[i].LineIndex = e.Index - 1;

                    //if (items[i].LineIndex == e.Index + e.Count - 1)
                    //{
                    //    items[i].LineIndex = items[i].LineIndex - e.Count;
                    //    continue;
                    //}
                    //
                    //items.RemoveAt(i);
                    //i--;
                }
        }

        protected virtual void tb_LineInserted(object sender, LineInsertedEventArgs e)
        {
            for (var i = 0; i < Count; i++)
                if (Items[i].LineIndex >= e.Index)
                {
                    Items[i].LineIndex = Items[i].LineIndex + e.Count;
                }
                else if (Items[i].LineIndex == e.Index - 1 && e.Count == 1)
                {
                    if (tb[e.Index - 1].StartSpacesCount == tb[e.Index - 1].Count)
                        Items[i].LineIndex = Items[i].LineIndex + e.Count;
                }
        }

        public override void Dispose()
        {
            tb.LineInserted -= tb_LineInserted;
            tb.LineRemoved -= tb_LineRemoved;
        }

        public override IEnumerator<Bookmark> GetEnumerator()
        {
            foreach (var item in Items)
                yield return item;
        }

        public override void Add(int lineIndex, string bookmarkName)
        {
            Add(new Bookmark(tb, bookmarkName ?? "Bookmark " + Counter, lineIndex));
        }

        public override void Add(int lineIndex)
        {
            Add(new Bookmark(tb, "Bookmark " + Counter, lineIndex));
        }

        public override void Clear()
        {
            Items.Clear();
            Counter = 0;
        }

        public override void Add(Bookmark bookmark)
        {
            foreach (var bm in Items)
                if (bm.LineIndex == bookmark.LineIndex)
                    return;

            Items.Add(bookmark);
            Counter++;
            tb.Invalidate();
        }

        public override bool Contains(Bookmark item)
        {
            return Items.Contains(item);
        }

        public override bool Contains(int lineIndex)
        {
            foreach (var item in Items)
                if (item.LineIndex == lineIndex)
                    return true;
            return false;
        }

        public override void CopyTo(Bookmark[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public override bool Remove(Bookmark item)
        {
            tb.Invalidate();
            return Items.Remove(item);
        }

        /// <summary>
        ///     Removes bookmark by line index
        /// </summary>
        public override bool Remove(int lineIndex)
        {
            var was = false;
            for (var i = 0; i < Count; i++)
                if (Items[i].LineIndex == lineIndex)
                {
                    Items.RemoveAt(i);
                    i--;
                    was = true;
                }
            tb.Invalidate();

            return was;
        }

        /// <summary>
        ///     Returns Bookmark by index.
        /// </summary>
        public override Bookmark GetBookmark(int i)
        {
            return Items[i];
        }
    }

    /// <summary>
    ///     Bookmark of FastColoredTextbox
    /// </summary>
    public class Bookmark
    {
        public Bookmark(FastColoredTextBox tb, string name, int lineIndex)
        {
            Tb = tb;
            Name = name;
            LineIndex = lineIndex;
            Color = tb.BookmarkColor;
        }

        public FastColoredTextBox Tb { get; }

        /// <summary>
        ///     Name of bookmark
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Line index
        /// </summary>
        public int LineIndex { get; set; }

        /// <summary>
        ///     Color of bookmark sign
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        ///     Scroll textbox to the bookmark
        /// </summary>
        public virtual void DoVisible()
        {
            Tb.Selection.Start = new Place(0, LineIndex);
            Tb.DoRangeVisible(Tb.Selection, true);
            Tb.Invalidate();
        }

        public virtual void Paint(Graphics gr, Rectangle lineRect)
        {
            var size = Tb.CharHeight - 1;
            using (
                var brush = new LinearGradientBrush(new Rectangle(0, lineRect.Top, size, size), Color.White, Color, 45))
                gr.FillEllipse(brush, 0, lineRect.Top, size, size);
            using (var pen = new Pen(Color))
                gr.DrawEllipse(pen, 0, lineRect.Top, size, size);
        }
    }
}