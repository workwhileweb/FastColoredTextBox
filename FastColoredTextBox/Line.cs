using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Line of text
    /// </summary>
    public class Line : IList<Char>
    {
        protected List<Char> Chars;

        internal Line(int uid)
        {
            UniqueId = uid;
            Chars = new List<Char>();
        }

        public string FoldingStartMarker { get; set; }
        public string FoldingEndMarker { get; set; }

        /// <summary>
        ///     Text of line was changed
        /// </summary>
        public bool IsChanged { get; set; }

        /// <summary>
        ///     Time of last visit of caret in this line
        /// </summary>
        /// <remarks>This property can be used for forward/backward navigating</remarks>
        public DateTime LastVisit { get; set; }

        /// <summary>
        ///     Background brush.
        /// </summary>
        public Brush BackgroundBrush { get; set; }

        /// <summary>
        ///     Unique ID
        /// </summary>
        public int UniqueId { get; private set; }

        /// <summary>
        ///     Count of needed start spaces for AutoIndent
        /// </summary>
        public int AutoIndentSpacesNeededCount { get; set; }

        /// <summary>
        ///     Text of the line
        /// </summary>
        public virtual string Text
        {
            get
            {
                var sb = new StringBuilder(Count);
                foreach (var c in this)
                    sb.Append(c.C);
                return sb.ToString();
            }
        }

        /// <summary>
        ///     Count of start spaces
        /// </summary>
        public int StartSpacesCount
        {
            get
            {
                var spacesCount = 0;
                for (var i = 0; i < Count; i++)
                    if (this[i].C == ' ')
                        spacesCount++;
                    else
                        break;
                return spacesCount;
            }
        }

        public int IndexOf(Char item)
        {
            return Chars.IndexOf(item);
        }

        public void Insert(int index, Char item)
        {
            Chars.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Chars.RemoveAt(index);
        }

        public Char this[int index]
        {
            get { return Chars[index]; }
            set { Chars[index] = value; }
        }

        public void Add(Char item)
        {
            Chars.Add(item);
        }

        public void Clear()
        {
            Chars.Clear();
        }

        public bool Contains(Char item)
        {
            return Chars.Contains(item);
        }

        public void CopyTo(Char[] array, int arrayIndex)
        {
            Chars.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Chars count
        /// </summary>
        public int Count
        {
            get { return Chars.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Char item)
        {
            return Chars.Remove(item);
        }

        public IEnumerator<Char> GetEnumerator()
        {
            return Chars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Chars.GetEnumerator();
        }


        /// <summary>
        ///     Clears style of chars, delete folding markers
        /// </summary>
        public void ClearStyle(StyleIndex styleIndex)
        {
            FoldingStartMarker = null;
            FoldingEndMarker = null;
            for (var i = 0; i < Count; i++)
            {
                var c = this[i];
                c.Style &= ~styleIndex;
                this[i] = c;
            }
        }

        /// <summary>
        ///     Clears folding markers
        /// </summary>
        public void ClearFoldingMarkers()
        {
            FoldingStartMarker = null;
            FoldingEndMarker = null;
        }

        public virtual void RemoveRange(int index, int count)
        {
            if (index >= Count)
                return;
            Chars.RemoveRange(index, Math.Min(Count - index, count));
        }

        public virtual void TrimExcess()
        {
            Chars.TrimExcess();
        }

        public virtual void AddRange(IEnumerable<Char> collection)
        {
            Chars.AddRange(collection);
        }
    }

    public struct LineInfo
    {
        private List<int> _cutOffPositions;
        //Y coordinate of line on screen
        internal int StartY;
        internal int BottomPadding;
        //indent of secondary wordwrap strings (in chars)
        internal int WordWrapIndent;

        /// <summary>
        ///     Visible state
        /// </summary>
        public VisibleState VisibleState;

        public LineInfo(int startY)
        {
            _cutOffPositions = null;
            VisibleState = VisibleState.Visible;
            StartY = startY;
            BottomPadding = 0;
            WordWrapIndent = 0;
        }

        /// <summary>
        ///     Positions for wordwrap cutoffs
        /// </summary>
        public List<int> CutOffPositions
        {
            get
            {
                if (_cutOffPositions == null)
                    _cutOffPositions = new List<int>();
                return _cutOffPositions;
            }
        }

        /// <summary>
        ///     Count of wordwrap string count for this line
        /// </summary>
        public int WordWrapStringsCount
        {
            get
            {
                switch (VisibleState)
                {
                    case VisibleState.Visible:
                        if (_cutOffPositions == null)
                            return 1;
                        return _cutOffPositions.Count + 1;
                    case VisibleState.Hidden:
                        return 0;
                    case VisibleState.StartOfHiddenBlock:
                        return 1;
                }

                return 0;
            }
        }

        internal int GetWordWrapStringStartPosition(int iWordWrapLine)
        {
            return iWordWrapLine == 0 ? 0 : CutOffPositions[iWordWrapLine - 1];
        }

        internal int GetWordWrapStringFinishPosition(int iWordWrapLine, Line line)
        {
            if (WordWrapStringsCount <= 0)
                return 0;
            return iWordWrapLine == WordWrapStringsCount - 1 ? line.Count - 1 : CutOffPositions[iWordWrapLine] - 1;
        }

        /// <summary>
        ///     Gets index of wordwrap string for given char position
        /// </summary>
        public int GetWordWrapStringIndex(int iChar)
        {
            if (_cutOffPositions == null || _cutOffPositions.Count == 0) return 0;
            for (var i = 0; i < _cutOffPositions.Count; i++)
                if (_cutOffPositions[i] > /*>=*/ iChar)
                    return i;
            return _cutOffPositions.Count;
        }
    }

    public enum VisibleState : byte
    {
        Visible,
        StartOfHiddenBlock,
        Hidden
    }

    public enum IndentMarker
    {
        None,
        Increased,
        Decreased
    }
}