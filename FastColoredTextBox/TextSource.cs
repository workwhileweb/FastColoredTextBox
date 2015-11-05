using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     This class contains the source text (chars and styles).
    ///     It stores a text lines, the manager of commands, undo/redo stack, styles.
    /// </summary>
    public class TextSource : IList<Line>, IDisposable
    {
        protected readonly List<Line> Lines = new List<Line>();

        /// <summary>
        ///     Styles
        /// </summary>
        public readonly Style[] Styles;

        private FastColoredTextBox _currentTb;
        private int _lastLineUniqueId;
        protected LinesAccessor LinesAccessor;

        public TextSource(FastColoredTextBox currentTb)
        {
            CurrentTb = currentTb;
            LinesAccessor = new LinesAccessor(this);
            Manager = new CommandManager(this);

            if (Enum.GetUnderlyingType(typeof (StyleIndex)) == typeof (uint))
                Styles = new Style[32];
            else
                Styles = new Style[16];

            InitDefaultStyle();
        }

        public CommandManager Manager { get; set; }

        /// <summary>
        ///     Current focused FastColoredTextBox
        /// </summary>
        public FastColoredTextBox CurrentTb
        {
            get { return _currentTb; }
            set
            {
                if (_currentTb == value)
                    return;
                _currentTb = value;
                OnCurrentTbChanged();
            }
        }

        /// <summary>
        ///     Default text style
        ///     This style is using when no one other TextStyle is not defined in Char.style
        /// </summary>
        public TextStyle DefaultStyle { get; set; }

        public virtual bool IsNeedBuildRemovedLineIds
        {
            get { return LineRemoved != null; }
        }

        public virtual void Dispose()
        {
            ;
        }

        public virtual Line this[int i]
        {
            get { return Lines[i]; }
            set { throw new NotImplementedException(); }
        }

        public IEnumerator<Line> GetEnumerator()
        {
            return Lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (Lines as IEnumerator);
        }

        public virtual int IndexOf(Line item)
        {
            return Lines.IndexOf(item);
        }

        public virtual void Insert(int index, Line item)
        {
            InsertLine(index, item);
        }

        public virtual void RemoveAt(int index)
        {
            RemoveLine(index);
        }

        public virtual void Add(Line item)
        {
            InsertLine(Count, item);
        }

        public virtual void Clear()
        {
            RemoveLine(0, Count);
        }

        public virtual bool Contains(Line item)
        {
            return Lines.Contains(item);
        }

        public virtual void CopyTo(Line[] array, int arrayIndex)
        {
            Lines.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Lines count
        /// </summary>
        public virtual int Count
        {
            get { return Lines.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual bool Remove(Line item)
        {
            var i = IndexOf(item);
            if (i >= 0)
            {
                RemoveLine(i);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Occurs when line was inserted/added
        /// </summary>
        public event EventHandler<LineInsertedEventArgs> LineInserted;

        /// <summary>
        ///     Occurs when line was removed
        /// </summary>
        public event EventHandler<LineRemovedEventArgs> LineRemoved;

        /// <summary>
        ///     Occurs when text was changed
        /// </summary>
        public event EventHandler<TextChangedEventArgs> TextChanged;

        /// <summary>
        ///     Occurs when recalc is needed
        /// </summary>
        public event EventHandler<TextChangedEventArgs> RecalcNeeded;

        /// <summary>
        ///     Occurs when recalc wordwrap is needed
        /// </summary>
        public event EventHandler<TextChangedEventArgs> RecalcWordWrap;

        /// <summary>
        ///     Occurs before text changing
        /// </summary>
        public event EventHandler<TextChangingEventArgs> TextChanging;

        /// <summary>
        ///     Occurs after CurrentTB was changed
        /// </summary>
        public event EventHandler CurrentTbChanged;

        public virtual void ClearIsChanged()
        {
            foreach (var line in Lines)
                line.IsChanged = false;
        }

        public virtual Line CreateLine()
        {
            return new Line(GenerateUniqueLineId());
        }

        private void OnCurrentTbChanged()
        {
            if (CurrentTbChanged != null)
                CurrentTbChanged(this, EventArgs.Empty);
        }

        public virtual void InitDefaultStyle()
        {
            DefaultStyle = new TextStyle(null, null, FontStyle.Regular);
        }

        public virtual bool IsLineLoaded(int iLine)
        {
            return Lines[iLine] != null;
        }

        /// <summary>
        ///     Text lines
        /// </summary>
        public virtual IList<string> GetLines()
        {
            return LinesAccessor;
        }

        public virtual int BinarySearch(Line item, IComparer<Line> comparer)
        {
            return Lines.BinarySearch(item, comparer);
        }

        public virtual int GenerateUniqueLineId()
        {
            return _lastLineUniqueId++;
        }

        public virtual void InsertLine(int index, Line line)
        {
            Lines.Insert(index, line);
            OnLineInserted(index);
        }

        public virtual void OnLineInserted(int index)
        {
            OnLineInserted(index, 1);
        }

        public virtual void OnLineInserted(int index, int count)
        {
            if (LineInserted != null)
                LineInserted(this, new LineInsertedEventArgs(index, count));
        }

        public virtual void RemoveLine(int index)
        {
            RemoveLine(index, 1);
        }

        public virtual void RemoveLine(int index, int count)
        {
            var removedLineIds = new List<int>();
            //
            if (count > 0)
                if (IsNeedBuildRemovedLineIds)
                    for (var i = 0; i < count; i++)
                        removedLineIds.Add(this[index + i].UniqueId);
            //
            Lines.RemoveRange(index, count);

            OnLineRemoved(index, count, removedLineIds);
        }

        public virtual void OnLineRemoved(int index, int count, List<int> removedLineIds)
        {
            if (count > 0)
                if (LineRemoved != null)
                    LineRemoved(this, new LineRemovedEventArgs(index, count, removedLineIds));
        }

        public virtual void OnTextChanged(int fromLine, int toLine)
        {
            if (TextChanged != null)
                TextChanged(this, new TextChangedEventArgs(Math.Min(fromLine, toLine), Math.Max(fromLine, toLine)));
        }

        public virtual void NeedRecalc(TextChangedEventArgs args)
        {
            if (RecalcNeeded != null)
                RecalcNeeded(this, args);
        }

        public virtual void OnRecalcWordWrap(TextChangedEventArgs args)
        {
            if (RecalcWordWrap != null)
                RecalcWordWrap(this, args);
        }

        public virtual void OnTextChanging()
        {
            string temp = null;
            OnTextChanging(ref temp);
        }

        public virtual void OnTextChanging(ref string text)
        {
            if (TextChanging != null)
            {
                var args = new TextChangingEventArgs {InsertingText = text};
                TextChanging(this, args);
                text = args.InsertingText;
                if (args.Cancel)
                    text = string.Empty;
            }
            ;
        }

        public virtual int GetLineLength(int i)
        {
            return Lines[i].Count;
        }

        public virtual bool LineHasFoldingStartMarker(int iLine)
        {
            return !string.IsNullOrEmpty(Lines[iLine].FoldingStartMarker);
        }

        public virtual bool LineHasFoldingEndMarker(int iLine)
        {
            return !string.IsNullOrEmpty(Lines[iLine].FoldingEndMarker);
        }

        public virtual void SaveToFile(string fileName, Encoding enc)
        {
            using (var sw = new StreamWriter(fileName, false, enc))
            {
                for (var i = 0; i < Count - 1; i++)
                    sw.WriteLine(Lines[i].Text);

                sw.Write(Lines[Count - 1].Text);
            }
        }

        public class TextChangedEventArgs : EventArgs
        {
            public int IFromLine;
            public int IToLine;

            public TextChangedEventArgs(int fromLine, int toLine)
            {
                IFromLine = fromLine;
                IToLine = toLine;
            }
        }
    }
}