using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Diapason of text chars
    /// </summary>
    public class Range : IEnumerable<Place>
    {
        public readonly FastColoredTextBox Tb;
        private List<Place> _cachedCharIndexToPlace;

        private string _cachedText;
        private int _cachedTextVersion = -1;

        private Place _end;
        private int _preferedPos = -1;
        private Place _start;
        private int _updating;

        /// <summary>
        ///     Constructor
        /// </summary>
        public Range(FastColoredTextBox tb)
        {
            Tb = tb;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public Range(FastColoredTextBox tb, int iStartChar, int iStartLine, int iEndChar, int iEndLine)
            : this(tb)
        {
            _start = new Place(iStartChar, iStartLine);
            _end = new Place(iEndChar, iEndLine);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public Range(FastColoredTextBox tb, Place start, Place end)
            : this(tb)
        {
            _start = start;
            _end = end;
        }

        /// <summary>
        ///     Constructor. Creates range of the line
        /// </summary>
        public Range(FastColoredTextBox tb, int iLine)
            : this(tb)
        {
            _start = new Place(0, iLine);
            _end = new Place(tb[iLine].Count, iLine);
        }

        /// <summary>
        ///     Return true if no selected text
        /// </summary>
        public virtual bool IsEmpty
        {
            get
            {
                if (ColumnSelectionMode)
                    return Start.IChar == End.IChar;
                return Start == End;
            }
        }

        /// <summary>
        ///     Column selection mode
        /// </summary>
        public bool ColumnSelectionMode { get; set; }

        /// <summary>
        ///     Start line and char position
        /// </summary>
        public Place Start
        {
            get { return _start; }
            set
            {
                _end = _start = value;
                _preferedPos = -1;
                OnSelectionChanged();
            }
        }

        /// <summary>
        ///     Finish line and char position
        /// </summary>
        public Place End
        {
            get { return _end; }
            set
            {
                _end = value;
                OnSelectionChanged();
            }
        }

        /// <summary>
        ///     Text of range
        /// </summary>
        /// <remarks>
        ///     This property has not 'set' accessor because undo/redo stack works only with
        ///     FastColoredTextBox.Selection range. So, if you want to set text, you need to use FastColoredTextBox.Selection
        ///     and FastColoredTextBox.InsertText() mehtod.
        /// </remarks>
        public virtual string Text
        {
            get
            {
                if (ColumnSelectionMode)
                    return TextColumnSelectionMode;

                var fromLine = Math.Min(_end.ILine, _start.ILine);
                var toLine = Math.Max(_end.ILine, _start.ILine);
                var fromChar = FromX;
                var toChar = ToX;
                if (fromLine < 0) return null;
                //
                var sb = new StringBuilder();
                for (var y = fromLine; y <= toLine; y++)
                {
                    var fromX = y == fromLine ? fromChar : 0;
                    var toX = y == toLine ? Math.Min(Tb[y].Count - 1, toChar - 1) : Tb[y].Count - 1;
                    for (var x = fromX; x <= toX; x++)
                        sb.Append(Tb[y][x].C);
                    if (y != toLine && fromLine != toLine)
                        sb.AppendLine();
                }
                return sb.ToString();
            }
        }

        /// <summary>
        ///     Returns first char after Start place
        /// </summary>
        public char CharAfterStart
        {
            get
            {
                if (Start.IChar >= Tb[Start.ILine].Count)
                    return '\n';
                return Tb[Start.ILine][Start.IChar].C;
            }
        }

        /// <summary>
        ///     Returns first char before Start place
        /// </summary>
        public char CharBeforeStart
        {
            get
            {
                if (Start.IChar > Tb[Start.ILine].Count)
                    return '\n';
                if (Start.IChar <= 0)
                    return '\n';
                return Tb[Start.ILine][Start.IChar - 1].C;
            }
        }

        /// <summary>
        ///     Return minimum of end.X and start.X
        /// </summary>
        internal int FromX
        {
            get
            {
                if (_end.ILine < _start.ILine) return _end.IChar;
                if (_end.ILine > _start.ILine) return _start.IChar;
                return Math.Min(_end.IChar, _start.IChar);
            }
        }

        /// <summary>
        ///     Return maximum of end.X and start.X
        /// </summary>
        internal int ToX
        {
            get
            {
                if (_end.ILine < _start.ILine) return _start.IChar;
                if (_end.ILine > _start.ILine) return _end.IChar;
                return Math.Max(_end.IChar, _start.IChar);
            }
        }

        public int FromLine
        {
            get { return Math.Min(Start.ILine, End.ILine); }
        }

        public int ToLine
        {
            get { return Math.Max(Start.ILine, End.ILine); }
        }

        /// <summary>
        ///     Chars of range (exclude \n)
        /// </summary>
        public IEnumerable<Char> Chars
        {
            get
            {
                if (ColumnSelectionMode)
                {
                    foreach (var p in GetEnumerator_ColumnSelectionMode())
                        yield return Tb[p];
                    yield break;
                }

                var fromLine = Math.Min(_end.ILine, _start.ILine);
                var toLine = Math.Max(_end.ILine, _start.ILine);
                var fromChar = FromX;
                var toChar = ToX;
                if (fromLine < 0) yield break;
                //
                for (var y = fromLine; y <= toLine; y++)
                {
                    var fromX = y == fromLine ? fromChar : 0;
                    var toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
                    var line = Tb[y];
                    for (var x = fromX; x <= toX; x++)
                        yield return line[x];
                }
            }
        }

        public RangeRect Bounds
        {
            get
            {
                var minX = Math.Min(Start.IChar, End.IChar);
                var minY = Math.Min(Start.ILine, End.ILine);
                var maxX = Math.Max(Start.IChar, End.IChar);
                var maxY = Math.Max(Start.ILine, End.ILine);
                return new RangeRect(minY, minX, maxY, maxX);
            }
        }

        /// <summary>
        ///     Range is readonly?
        ///     This property return True if any char of the range contains ReadOnlyStyle.
        ///     Set this property to True/False to mark chars of the range as Readonly/Writable.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                if (Tb.ReadOnly) return true;

                ReadOnlyStyle readonlyStyle = null;
                foreach (var style in Tb.Styles)
                    if (style is ReadOnlyStyle)
                    {
                        readonlyStyle = (ReadOnlyStyle) style;
                        break;
                    }

                if (readonlyStyle != null)
                {
                    var si = ToStyleIndex(Tb.GetStyleIndex(readonlyStyle));

                    if (IsEmpty)
                    {
                        //check previous and next chars
                        var line = Tb[_start.ILine];
                        if (ColumnSelectionMode)
                        {
                            foreach (var sr in GetSubRanges(false))
                            {
                                line = Tb[sr._start.ILine];
                                if (sr._start.IChar < line.Count && sr._start.IChar > 0)
                                {
                                    var left = line[sr._start.IChar - 1];
                                    var right = line[sr._start.IChar];
                                    if ((left.Style & si) != 0 &&
                                        (right.Style & si) != 0) return true; //we are between readonly chars
                                }
                            }
                        }
                        else if (_start.IChar < line.Count && _start.IChar > 0)
                        {
                            var left = line[_start.IChar - 1];
                            var right = line[_start.IChar];
                            if ((left.Style & si) != 0 &&
                                (right.Style & si) != 0) return true; //we are between readonly chars
                        }
                    }
                    else
                        foreach (var c in Chars)
                            if ((c.Style & si) != 0) //found char with ReadonlyStyle
                                return true;
                }

                return false;
            }

            set
            {
                //find exists ReadOnlyStyle of style buffer
                ReadOnlyStyle readonlyStyle = null;
                foreach (var style in Tb.Styles)
                    if (style is ReadOnlyStyle)
                    {
                        readonlyStyle = (ReadOnlyStyle) style;
                        break;
                    }

                //create ReadOnlyStyle
                if (readonlyStyle == null)
                    readonlyStyle = new ReadOnlyStyle();

                //set/clear style
                if (value)
                    SetStyle(readonlyStyle);
                else
                    ClearStyle(readonlyStyle);
            }
        }

        IEnumerator<Place> IEnumerable<Place>.GetEnumerator()
        {
            if (ColumnSelectionMode)
            {
                foreach (var p in GetEnumerator_ColumnSelectionMode())
                    yield return p;
                yield break;
            }

            var fromLine = Math.Min(_end.ILine, _start.ILine);
            var toLine = Math.Max(_end.ILine, _start.ILine);
            var fromChar = FromX;
            var toChar = ToX;
            if (fromLine < 0) yield break;
            //
            for (var y = fromLine; y <= toLine; y++)
            {
                var fromX = y == fromLine ? fromChar : 0;
                var toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
                for (var x = fromX; x <= toX; x++)
                    yield return new Place(x, y);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<Place>).GetEnumerator();
        }

        public bool Contains(Place place)
        {
            if (place.ILine < Math.Min(_start.ILine, _end.ILine)) return false;
            if (place.ILine > Math.Max(_start.ILine, _end.ILine)) return false;

            var s = _start;
            var e = _end;
            //normalize start and end
            if (s.ILine > e.ILine || (s.ILine == e.ILine && s.IChar > e.IChar))
            {
                var temp = s;
                s = e;
                e = temp;
            }

            if (ColumnSelectionMode)
            {
                if (place.IChar < s.IChar || place.IChar > e.IChar) return false;
            }
            else
            {
                if (place.ILine == s.ILine && place.IChar < s.IChar) return false;
                if (place.ILine == e.ILine && place.IChar > e.IChar) return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns intersection with other range,
        ///     empty range returned otherwise
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public virtual Range GetIntersectionWith(Range range)
        {
            if (ColumnSelectionMode)
                return GetIntersectionWith_ColumnSelectionMode(range);

            var r1 = Clone();
            var r2 = range.Clone();
            r1.Normalize();
            r2.Normalize();
            var newStart = r1.Start > r2.Start ? r1.Start : r2.Start;
            var newEnd = r1.End < r2.End ? r1.End : r2.End;
            if (newEnd < newStart)
                return new Range(Tb, _start, _start);
            return Tb.GetRange(newStart, newEnd);
        }

        /// <summary>
        ///     Returns union with other range.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Range GetUnionWith(Range range)
        {
            var r1 = Clone();
            var r2 = range.Clone();
            r1.Normalize();
            r2.Normalize();
            var newStart = r1.Start < r2.Start ? r1.Start : r2.Start;
            var newEnd = r1.End > r2.End ? r1.End : r2.End;

            return Tb.GetRange(newStart, newEnd);
        }

        /// <summary>
        ///     Select all chars of control
        /// </summary>
        public void SelectAll()
        {
            ColumnSelectionMode = false;

            Start = new Place(0, 0);
            if (Tb.LinesCount == 0)
                Start = new Place(0, 0);
            else
            {
                _end = new Place(0, 0);
                _start = new Place(Tb[Tb.LinesCount - 1].Count, Tb.LinesCount - 1);
            }
            if (this == Tb.Selection)
                Tb.Invalidate();
        }

        internal void GetText(out string text, out List<Place> charIndexToPlace)
        {
            //try get cached text
            if (Tb.TextVersion == _cachedTextVersion)
            {
                text = _cachedText;
                charIndexToPlace = _cachedCharIndexToPlace;
                return;
            }
            //
            var fromLine = Math.Min(_end.ILine, _start.ILine);
            var toLine = Math.Max(_end.ILine, _start.ILine);
            var fromChar = FromX;
            var toChar = ToX;

            var sb = new StringBuilder((toLine - fromLine)*50);
            charIndexToPlace = new List<Place>(sb.Capacity);
            if (fromLine >= 0)
            {
                for (var y = fromLine; y <= toLine; y++)
                {
                    var fromX = y == fromLine ? fromChar : 0;
                    var toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
                    for (var x = fromX; x <= toX; x++)
                    {
                        sb.Append(Tb[y][x].C);
                        charIndexToPlace.Add(new Place(x, y));
                    }
                    if (y != toLine && fromLine != toLine)
                        foreach (var c in Environment.NewLine)
                        {
                            sb.Append(c);
                            charIndexToPlace.Add(new Place(Tb[y].Count /*???*/, y));
                        }
                }
            }
            text = sb.ToString();
            charIndexToPlace.Add(End > Start ? End : Start);
            //caching
            _cachedText = text;
            _cachedCharIndexToPlace = charIndexToPlace;
            _cachedTextVersion = Tb.TextVersion;
        }

        /// <summary>
        ///     Returns required char's number before start of the Range
        /// </summary>
        public string GetCharsBeforeStart(int charsCount)
        {
            var pos = Tb.PlaceToPosition(Start) - charsCount;
            if (pos < 0) pos = 0;

            return new Range(Tb, Tb.PositionToPlace(pos), Start).Text;
        }

        /// <summary>
        ///     Returns required char's number after start of the Range
        /// </summary>
        public string GetCharsAfterStart(int charsCount)
        {
            return GetCharsBeforeStart(-charsCount);
        }

        /// <summary>
        ///     Clone range
        /// </summary>
        /// <returns></returns>
        public Range Clone()
        {
            return (Range) MemberwiseClone();
        }

        /// <summary>
        ///     Move range right
        /// </summary>
        /// <remarks>This method jump over folded blocks</remarks>
        public bool GoRight()
        {
            var prevStart = _start;
            GoRight(false);
            return prevStart != _start;
        }

        /// <summary>
        ///     Move range left
        /// </summary>
        /// <remarks>This method can to go inside folded blocks</remarks>
        public virtual bool GoRightThroughFolded()
        {
            if (ColumnSelectionMode)
                return GoRightThroughFolded_ColumnSelectionMode();

            if (_start.ILine >= Tb.LinesCount - 1 && _start.IChar >= Tb[Tb.LinesCount - 1].Count)
                return false;

            if (_start.IChar < Tb[_start.ILine].Count)
                _start.Offset(1, 0);
            else
                _start = new Place(0, _start.ILine + 1);

            _preferedPos = -1;
            _end = _start;
            OnSelectionChanged();
            return true;
        }

        /// <summary>
        ///     Move range left
        /// </summary>
        /// <remarks>This method jump over folded blocks</remarks>
        public bool GoLeft()
        {
            ColumnSelectionMode = false;

            var prevStart = _start;
            GoLeft(false);
            return prevStart != _start;
        }

        /// <summary>
        ///     Move range left
        /// </summary>
        /// <remarks>This method can to go inside folded blocks</remarks>
        public bool GoLeftThroughFolded()
        {
            ColumnSelectionMode = false;

            if (_start.IChar == 0 && _start.ILine == 0)
                return false;

            if (_start.IChar > 0)
                _start.Offset(-1, 0);
            else
                _start = new Place(Tb[_start.ILine - 1].Count, _start.ILine - 1);

            _preferedPos = -1;
            _end = _start;
            OnSelectionChanged();
            return true;
        }

        public void GoLeft(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
                if (_start > _end)
                {
                    Start = End;
                    return;
                }

            if (_start.IChar != 0 || _start.ILine != 0)
            {
                if (_start.IChar > 0 && Tb.LineInfos[_start.ILine].VisibleState == VisibleState.Visible)
                    _start.Offset(-1, 0);
                else
                {
                    var i = Tb.FindPrevVisibleLine(_start.ILine);
                    if (i == _start.ILine) return;
                    _start = new Place(Tb[i].Count, i);
                }
            }

            if (!shift)
                _end = _start;

            OnSelectionChanged();

            _preferedPos = -1;
        }

        public void GoRight(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
                if (_start < _end)
                {
                    Start = End;
                    return;
                }

            if (_start.ILine < Tb.LinesCount - 1 || _start.IChar < Tb[Tb.LinesCount - 1].Count)
            {
                if (_start.IChar < Tb[_start.ILine].Count &&
                    Tb.LineInfos[_start.ILine].VisibleState == VisibleState.Visible)
                    _start.Offset(1, 0);
                else
                {
                    var i = Tb.FindNextVisibleLine(_start.ILine);
                    if (i == _start.ILine) return;
                    _start = new Place(0, i);
                }
            }

            if (!shift)
                _end = _start;

            OnSelectionChanged();

            _preferedPos = -1;
        }

        internal void GoUp(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
                if (_start.ILine > _end.ILine)
                {
                    Start = End;
                    return;
                }

            if (_preferedPos < 0)
                _preferedPos = _start.IChar -
                               Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(
                                   Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar));

            var iWw = Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar);
            if (iWw == 0)
            {
                if (_start.ILine <= 0) return;
                var i = Tb.FindPrevVisibleLine(_start.ILine);
                if (i == _start.ILine) return;
                _start.ILine = i;
                iWw = Tb.LineInfos[_start.ILine].WordWrapStringsCount;
            }

            if (iWw > 0)
            {
                var finish = Tb.LineInfos[_start.ILine].GetWordWrapStringFinishPosition(iWw - 1, Tb[_start.ILine]);
                _start.IChar = Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(iWw - 1) + _preferedPos;
                if (_start.IChar > finish + 1)
                    _start.IChar = finish + 1;
            }

            if (!shift)
                _end = _start;

            OnSelectionChanged();
        }

        internal void GoPageUp(bool shift)
        {
            ColumnSelectionMode = false;

            if (_preferedPos < 0)
                _preferedPos = _start.IChar -
                               Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(
                                   Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar));

            var pageHeight = Tb.ClientRectangle.Height/Tb.CharHeight - 1;

            for (var i = 0; i < pageHeight; i++)
            {
                var iWw = Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar);
                if (iWw == 0)
                {
                    if (_start.ILine <= 0) break;
                    //pass hidden
                    var newLine = Tb.FindPrevVisibleLine(_start.ILine);
                    if (newLine == _start.ILine) break;
                    _start.ILine = newLine;
                    iWw = Tb.LineInfos[_start.ILine].WordWrapStringsCount;
                }

                if (iWw > 0)
                {
                    var finish = Tb.LineInfos[_start.ILine].GetWordWrapStringFinishPosition(iWw - 1, Tb[_start.ILine]);
                    _start.IChar = Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(iWw - 1) + _preferedPos;
                    if (_start.IChar > finish + 1)
                        _start.IChar = finish + 1;
                }
            }

            if (!shift)
                _end = _start;

            OnSelectionChanged();
        }

        internal void GoDown(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
                if (_start.ILine < _end.ILine)
                {
                    Start = End;
                    return;
                }

            if (_preferedPos < 0)
                _preferedPos = _start.IChar -
                               Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(
                                   Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar));

            var iWw = Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar);
            if (iWw >= Tb.LineInfos[_start.ILine].WordWrapStringsCount - 1)
            {
                if (_start.ILine >= Tb.LinesCount - 1) return;
                //pass hidden
                var i = Tb.FindNextVisibleLine(_start.ILine);
                if (i == _start.ILine) return;
                _start.ILine = i;
                iWw = -1;
            }

            if (iWw < Tb.LineInfos[_start.ILine].WordWrapStringsCount - 1)
            {
                var finish = Tb.LineInfos[_start.ILine].GetWordWrapStringFinishPosition(iWw + 1, Tb[_start.ILine]);
                _start.IChar = Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(iWw + 1) + _preferedPos;
                if (_start.IChar > finish + 1)
                    _start.IChar = finish + 1;
            }

            if (!shift)
                _end = _start;

            OnSelectionChanged();
        }

        internal void GoPageDown(bool shift)
        {
            ColumnSelectionMode = false;

            if (_preferedPos < 0)
                _preferedPos = _start.IChar -
                               Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(
                                   Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar));

            var pageHeight = Tb.ClientRectangle.Height/Tb.CharHeight - 1;

            for (var i = 0; i < pageHeight; i++)
            {
                var iWw = Tb.LineInfos[_start.ILine].GetWordWrapStringIndex(_start.IChar);
                if (iWw >= Tb.LineInfos[_start.ILine].WordWrapStringsCount - 1)
                {
                    if (_start.ILine >= Tb.LinesCount - 1) break;
                    //pass hidden
                    var newLine = Tb.FindNextVisibleLine(_start.ILine);
                    if (newLine == _start.ILine) break;
                    _start.ILine = newLine;
                    iWw = -1;
                }

                if (iWw < Tb.LineInfos[_start.ILine].WordWrapStringsCount - 1)
                {
                    var finish = Tb.LineInfos[_start.ILine].GetWordWrapStringFinishPosition(iWw + 1, Tb[_start.ILine]);
                    _start.IChar = Tb.LineInfos[_start.ILine].GetWordWrapStringStartPosition(iWw + 1) + _preferedPos;
                    if (_start.IChar > finish + 1)
                        _start.IChar = finish + 1;
                }
            }

            if (!shift)
                _end = _start;

            OnSelectionChanged();
        }

        internal void GoHome(bool shift)
        {
            ColumnSelectionMode = false;

            if (_start.ILine < 0)
                return;

            if (Tb.LineInfos[_start.ILine].VisibleState != VisibleState.Visible)
                return;

            _start = new Place(0, _start.ILine);

            if (!shift)
                _end = _start;

            OnSelectionChanged();

            _preferedPos = -1;
        }

        internal void GoEnd(bool shift)
        {
            ColumnSelectionMode = false;

            if (_start.ILine < 0)
                return;
            if (Tb.LineInfos[_start.ILine].VisibleState != VisibleState.Visible)
                return;

            _start = new Place(Tb[_start.ILine].Count, _start.ILine);

            if (!shift)
                _end = _start;

            OnSelectionChanged();

            _preferedPos = -1;
        }

        /// <summary>
        ///     Set style for range
        /// </summary>
        public void SetStyle(Style style)
        {
            //search code for style
            var code = Tb.GetOrSetStyleLayerIndex(style);
            //set code to chars
            SetStyle(ToStyleIndex(code));
            //
            Tb.Invalidate();
        }

        /// <summary>
        ///     Set style for given regex pattern
        /// </summary>
        public void SetStyle(Style style, string regexPattern)
        {
            //search code for style
            var layer = ToStyleIndex(Tb.GetOrSetStyleLayerIndex(style));
            SetStyle(layer, regexPattern, RegexOptions.None);
        }

        /// <summary>
        ///     Set style for given regex
        /// </summary>
        public void SetStyle(Style style, Regex regex)
        {
            //search code for style
            var layer = ToStyleIndex(Tb.GetOrSetStyleLayerIndex(style));
            SetStyle(layer, regex);
        }


        /// <summary>
        ///     Set style for given regex pattern
        /// </summary>
        public void SetStyle(Style style, string regexPattern, RegexOptions options)
        {
            //search code for style
            var layer = ToStyleIndex(Tb.GetOrSetStyleLayerIndex(style));
            SetStyle(layer, regexPattern, options);
        }

        /// <summary>
        ///     Set style for given regex pattern
        /// </summary>
        public void SetStyle(StyleIndex styleLayer, string regexPattern, RegexOptions options)
        {
            if (Math.Abs(Start.ILine - End.ILine) > 1000)
                options |= SyntaxHighlighter.RegexCompiledOption;
            //
            foreach (var range in GetRanges(regexPattern, options))
                range.SetStyle(styleLayer);
            //
            Tb.Invalidate();
        }

        /// <summary>
        ///     Set style for given regex pattern
        /// </summary>
        public void SetStyle(StyleIndex styleLayer, Regex regex)
        {
            foreach (var range in GetRanges(regex))
                range.SetStyle(styleLayer);
            //
            Tb.Invalidate();
        }

        /// <summary>
        ///     Appends style to chars of range
        /// </summary>
        public void SetStyle(StyleIndex styleIndex)
        {
            //set code to chars
            var fromLine = Math.Min(End.ILine, Start.ILine);
            var toLine = Math.Max(End.ILine, Start.ILine);
            var fromChar = FromX;
            var toChar = ToX;
            if (fromLine < 0) return;
            //
            for (var y = fromLine; y <= toLine; y++)
            {
                var fromX = y == fromLine ? fromChar : 0;
                var toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
                for (var x = fromX; x <= toX; x++)
                {
                    var c = Tb[y][x];
                    c.Style |= styleIndex;
                    Tb[y][x] = c;
                }
            }
        }

        /// <summary>
        ///     Sets folding markers
        /// </summary>
        /// <param name="startFoldingPattern">Pattern for start folding line</param>
        /// <param name="finishFoldingPattern">Pattern for finish folding line</param>
        public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern)
        {
            SetFoldingMarkers(startFoldingPattern, finishFoldingPattern, SyntaxHighlighter.RegexCompiledOption);
        }

        /// <summary>
        ///     Sets folding markers
        /// </summary>
        /// <param name="startFoldingPattern">Pattern for start folding line</param>
        /// <param name="finishFoldingPattern">Pattern for finish folding line</param>
        public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern, RegexOptions options)
        {
            if (startFoldingPattern == finishFoldingPattern)
            {
                SetFoldingMarkers(startFoldingPattern, options);
                return;
            }

            foreach (var range in GetRanges(startFoldingPattern, options))
                Tb[range.Start.ILine].FoldingStartMarker = startFoldingPattern;

            foreach (var range in GetRanges(finishFoldingPattern, options))
                Tb[range.Start.ILine].FoldingEndMarker = startFoldingPattern;
            //
            Tb.Invalidate();
        }

        /// <summary>
        ///     Sets folding markers
        /// </summary>
        /// <param name="startEndFoldingPattern">Pattern for start and end folding line</param>
        public void SetFoldingMarkers(string foldingPattern, RegexOptions options)
        {
            foreach (var range in GetRanges(foldingPattern, options))
            {
                if (range.Start.ILine > 0)
                    Tb[range.Start.ILine - 1].FoldingEndMarker = foldingPattern;
                Tb[range.Start.ILine].FoldingStartMarker = foldingPattern;
            }

            Tb.Invalidate();
        }

        /// <summary>
        ///     Finds ranges for given regex pattern
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRanges(string regexPattern)
        {
            return GetRanges(regexPattern, RegexOptions.None);
        }

        /// <summary>
        ///     Finds ranges for given regex pattern
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRanges(string regexPattern, RegexOptions options)
        {
            //get text
            string text;
            List<Place> charIndexToPlace;
            GetText(out text, out charIndexToPlace);
            //create regex
            var regex = new Regex(regexPattern, options);
            //
            foreach (Match m in regex.Matches(text))
            {
                var r = new Range(Tb);
                //try get 'range' group, otherwise use group 0
                var group = m.Groups["range"];
                if (!group.Success)
                    group = m.Groups[0];
                //
                r.Start = charIndexToPlace[group.Index];
                r.End = charIndexToPlace[group.Index + group.Length];
                yield return r;
            }
        }

        /// <summary>
        ///     Finds ranges for given regex pattern.
        ///     Search is separately in each line.
        ///     This method requires less memory than GetRanges().
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRangesByLines(string regexPattern, RegexOptions options)
        {
            var regex = new Regex(regexPattern, options);
            foreach (var r in GetRangesByLines(regex))
                yield return r;
        }

        /// <summary>
        ///     Finds ranges for given regex.
        ///     Search is separately in each line.
        ///     This method requires less memory than GetRanges().
        /// </summary>
        /// <param name="regex">Regex</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRangesByLines(Regex regex)
        {
            Normalize();

            var fts = Tb.TextSource as FileTextSource; //<----!!!! ugly

            //enumaerate lines
            for (var iLine = Start.ILine; iLine <= End.ILine; iLine++)
            {
                //
                var isLineLoaded = fts != null ? fts.IsLineLoaded(iLine) : true;
                //
                var r = new Range(Tb, new Place(0, iLine), new Place(Tb[iLine].Count, iLine));
                if (iLine == Start.ILine || iLine == End.ILine)
                    r = r.GetIntersectionWith(this);

                foreach (var foundRange in r.GetRanges(regex))
                    yield return foundRange;

                if (!isLineLoaded)
                    fts.UnloadLine(iLine);
            }
        }

        /// <summary>
        ///     Finds ranges for given regex pattern.
        ///     Search is separately in each line (order of lines is reversed).
        ///     This method requires less memory than GetRanges().
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRangesByLinesReversed(string regexPattern, RegexOptions options)
        {
            Normalize();
            //create regex
            var regex = new Regex(regexPattern, options);
            //
            var fts = Tb.TextSource as FileTextSource; //<----!!!! ugly

            //enumaerate lines
            for (var iLine = End.ILine; iLine >= Start.ILine; iLine--)
            {
                //
                var isLineLoaded = fts != null ? fts.IsLineLoaded(iLine) : true;
                //
                var r = new Range(Tb, new Place(0, iLine), new Place(Tb[iLine].Count, iLine));
                if (iLine == Start.ILine || iLine == End.ILine)
                    r = r.GetIntersectionWith(this);

                var list = new List<Range>();

                foreach (var foundRange in r.GetRanges(regex))
                    list.Add(foundRange);

                for (var i = list.Count - 1; i >= 0; i--)
                    yield return list[i];

                if (!isLineLoaded)
                    fts.UnloadLine(iLine);
            }
        }

        /// <summary>
        ///     Finds ranges for given regex
        /// </summary>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRanges(Regex regex)
        {
            //get text
            string text;
            List<Place> charIndexToPlace;
            GetText(out text, out charIndexToPlace);
            //
            foreach (Match m in regex.Matches(text))
            {
                var r = new Range(Tb);
                //try get 'range' group, otherwise use group 0
                var group = m.Groups["range"];
                if (!group.Success)
                    group = m.Groups[0];
                //
                r.Start = charIndexToPlace[group.Index];
                r.End = charIndexToPlace[group.Index + group.Length];
                yield return r;
            }
        }

        /// <summary>
        ///     Clear styles of range
        /// </summary>
        public void ClearStyle(params Style[] styles)
        {
            try
            {
                ClearStyle(Tb.GetStyleIndexMask(styles));
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        ///     Clear styles of range
        /// </summary>
        public void ClearStyle(StyleIndex styleIndex)
        {
            //set code to chars
            var fromLine = Math.Min(End.ILine, Start.ILine);
            var toLine = Math.Max(End.ILine, Start.ILine);
            var fromChar = FromX;
            var toChar = ToX;
            if (fromLine < 0) return;
            //
            for (var y = fromLine; y <= toLine; y++)
            {
                var fromX = y == fromLine ? fromChar : 0;
                var toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
                for (var x = fromX; x <= toX; x++)
                {
                    var c = Tb[y][x];
                    c.Style &= ~styleIndex;
                    Tb[y][x] = c;
                }
            }
            //
            Tb.Invalidate();
        }

        /// <summary>
        ///     Clear folding markers of all lines of range
        /// </summary>
        public void ClearFoldingMarkers()
        {
            //set code to chars
            var fromLine = Math.Min(End.ILine, Start.ILine);
            var toLine = Math.Max(End.ILine, Start.ILine);
            if (fromLine < 0) return;
            //
            for (var y = fromLine; y <= toLine; y++)
                Tb[y].ClearFoldingMarkers();
            //
            Tb.Invalidate();
        }

        private void OnSelectionChanged()
        {
            //clear cache
            _cachedTextVersion = -1;
            _cachedText = null;
            _cachedCharIndexToPlace = null;
            //
            if (Tb.Selection == this)
                if (_updating == 0)
                    Tb.OnSelectionChanged();
        }

        /// <summary>
        ///     Starts selection position updating
        /// </summary>
        public void BeginUpdate()
        {
            _updating++;
        }

        /// <summary>
        ///     Ends selection position updating
        /// </summary>
        public void EndUpdate()
        {
            _updating--;
            if (_updating == 0)
                OnSelectionChanged();
        }

        public override string ToString()
        {
            return "Start: " + Start + " End: " + End;
        }

        /// <summary>
        ///     Exchanges Start and End if End appears before Start
        /// </summary>
        public void Normalize()
        {
            if (Start > End)
                Inverse();
        }

        /// <summary>
        ///     Exchanges Start and End
        /// </summary>
        public void Inverse()
        {
            var temp = _start;
            _start = _end;
            _end = temp;
        }

        /// <summary>
        ///     Expands range from first char of Start line to last char of End line
        /// </summary>
        public void Expand()
        {
            Normalize();
            _start = new Place(0, _start.ILine);
            _end = new Place(Tb.GetLineLength(_end.ILine), _end.ILine);
        }

        /// <summary>
        ///     Get fragment of text around Start place. Returns maximal matched to pattern fragment.
        /// </summary>
        /// <param name="allowedSymbolsPattern">Allowed chars pattern for fragment</param>
        /// <returns>Range of found fragment</returns>
        public Range GetFragment(string allowedSymbolsPattern)
        {
            return GetFragment(allowedSymbolsPattern, RegexOptions.None);
        }

        /// <summary>
        ///     Get fragment of text around Start place. Returns maximal matched to given Style.
        /// </summary>
        /// <param name="style">Allowed style for fragment</param>
        /// <returns>Range of found fragment</returns>
        public Range GetFragment(Style style, bool allowLineBreaks)
        {
            var mask = Tb.GetStyleIndexMask(new[] {style});
            //
            var r = new Range(Tb);
            r.Start = Start;
            //go left, check style
            while (r.GoLeftThroughFolded())
            {
                if (!allowLineBreaks && r.CharAfterStart == '\n')
                    break;
                if (r.Start.IChar < Tb.GetLineLength(r.Start.ILine))
                    if ((Tb[r.Start].Style & mask) == 0)
                    {
                        r.GoRightThroughFolded();
                        break;
                    }
            }
            var startFragment = r.Start;

            r.Start = Start;
            //go right, check style
            do
            {
                if (!allowLineBreaks && r.CharAfterStart == '\n')
                    break;
                if (r.Start.IChar < Tb.GetLineLength(r.Start.ILine))
                    if ((Tb[r.Start].Style & mask) == 0)
                        break;
            } while (r.GoRightThroughFolded());
            var endFragment = r.Start;

            return new Range(Tb, startFragment, endFragment);
        }

        /// <summary>
        ///     Get fragment of text around Start place. Returns maximal mathed to pattern fragment.
        /// </summary>
        /// <param name="allowedSymbolsPattern">Allowed chars pattern for fragment</param>
        /// <returns>Range of found fragment</returns>
        public Range GetFragment(string allowedSymbolsPattern, RegexOptions options)
        {
            var r = new Range(Tb);
            r.Start = Start;
            var regex = new Regex(allowedSymbolsPattern, options);
            //go left, check symbols
            while (r.GoLeftThroughFolded())
            {
                if (!regex.IsMatch(r.CharAfterStart.ToString()))
                {
                    r.GoRightThroughFolded();
                    break;
                }
            }
            var startFragment = r.Start;

            r.Start = Start;
            //go right, check symbols
            do
            {
                if (!regex.IsMatch(r.CharAfterStart.ToString()))
                    break;
            } while (r.GoRightThroughFolded());
            var endFragment = r.Start;

            return new Range(Tb, startFragment, endFragment);
        }

        private bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private bool IsSpaceChar(char c)
        {
            return c == ' ' || c == '\t';
        }

        public void GoWordLeft(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift && _start > _end)
            {
                Start = End;
                return;
            }

            var range = Clone(); //to OnSelectionChanged disable
            var wasSpace = false;
            while (IsSpaceChar(range.CharBeforeStart))
            {
                wasSpace = true;
                range.GoLeft(shift);
            }
            var wasIdentifier = false;
            while (IsIdentifierChar(range.CharBeforeStart))
            {
                wasIdentifier = true;
                range.GoLeft(shift);
            }
            if (!wasIdentifier && (!wasSpace || range.CharBeforeStart != '\n'))
                range.GoLeft(shift);
            Start = range.Start;
            End = range.End;

            if (Tb.LineInfos[Start.ILine].VisibleState != VisibleState.Visible)
                GoRight(shift);
        }

        public void GoWordRight(bool shift, bool goToStartOfNextWord = false)
        {
            ColumnSelectionMode = false;

            if (!shift && _start < _end)
            {
                Start = End;
                return;
            }

            var range = Clone(); //to OnSelectionChanged disable

            var wasNewLine = false;


            if (range.CharAfterStart == '\n')
            {
                range.GoRight(shift);
                wasNewLine = true;
            }

            var wasSpace = false;
            while (IsSpaceChar(range.CharAfterStart))
            {
                wasSpace = true;
                range.GoRight(shift);
            }

            if (!((wasSpace || wasNewLine) && goToStartOfNextWord))
            {
                var wasIdentifier = false;
                while (IsIdentifierChar(range.CharAfterStart))
                {
                    wasIdentifier = true;
                    range.GoRight(shift);
                }

                if (!wasIdentifier)
                    range.GoRight(shift);

                if (goToStartOfNextWord && !wasSpace)
                    while (IsSpaceChar(range.CharAfterStart))
                        range.GoRight(shift);
            }

            Start = range.Start;
            End = range.End;

            if (Tb.LineInfos[Start.ILine].VisibleState != VisibleState.Visible)
                GoLeft(shift);
        }

        internal void GoFirst(bool shift)
        {
            ColumnSelectionMode = false;

            _start = new Place(0, 0);
            if (Tb.LineInfos[Start.ILine].VisibleState != VisibleState.Visible)
                Tb.ExpandBlock(Start.ILine);

            if (!shift)
                _end = _start;

            OnSelectionChanged();
        }

        internal void GoLast(bool shift)
        {
            ColumnSelectionMode = false;

            _start = new Place(Tb[Tb.LinesCount - 1].Count, Tb.LinesCount - 1);
            if (Tb.LineInfos[Start.ILine].VisibleState != VisibleState.Visible)
                Tb.ExpandBlock(Start.ILine);

            if (!shift)
                _end = _start;

            OnSelectionChanged();
        }

        public static StyleIndex ToStyleIndex(int i)
        {
            return (StyleIndex) (1 << i);
        }

        public IEnumerable<Range> GetSubRanges(bool includeEmpty)
        {
            if (!ColumnSelectionMode)
            {
                yield return this;
                yield break;
            }

            var rect = Bounds;
            for (var y = rect.IStartLine; y <= rect.IEndLine; y++)
            {
                if (rect.IStartChar > Tb[y].Count && !includeEmpty)
                    continue;

                var r = new Range(Tb, rect.IStartChar, y, Math.Min(rect.IEndChar, Tb[y].Count), y);
                yield return r;
            }
        }

        /// <summary>
        ///     Is char before range readonly
        /// </summary>
        /// <returns></returns>
        public bool IsReadOnlyLeftChar()
        {
            if (Tb.ReadOnly) return true;

            var r = Clone();

            r.Normalize();
            if (r._start.IChar == 0) return false;
            if (ColumnSelectionMode)
                r.GoLeft_ColumnSelectionMode();
            else
                r.GoLeft(true);

            return r.ReadOnly;
        }

        /// <summary>
        ///     Is char after range readonly
        /// </summary>
        /// <returns></returns>
        public bool IsReadOnlyRightChar()
        {
            if (Tb.ReadOnly) return true;

            var r = Clone();

            r.Normalize();
            if (r._end.IChar >= Tb[_end.ILine].Count) return false;
            if (ColumnSelectionMode)
                r.GoRight_ColumnSelectionMode();
            else
                r.GoRight(true);

            return r.ReadOnly;
        }

        public IEnumerable<Place> GetPlacesCyclic(Place startPlace, bool backward = false)
        {
            if (backward)
            {
                var r = new Range(Tb, startPlace, startPlace);
                while (r.GoLeft() && r._start >= Start)
                {
                    if (r.Start.IChar < Tb[r.Start.ILine].Count)
                        yield return r.Start;
                }

                r = new Range(Tb, End, End);
                while (r.GoLeft() && r._start >= startPlace)
                {
                    if (r.Start.IChar < Tb[r.Start.ILine].Count)
                        yield return r.Start;
                }
            }
            else
            {
                var r = new Range(Tb, startPlace, startPlace);
                if (startPlace < End)
                    do
                    {
                        if (r.Start.IChar < Tb[r.Start.ILine].Count)
                            yield return r.Start;
                    } while (r.GoRight());

                r = new Range(Tb, Start, Start);
                if (r.Start < startPlace)
                    do
                    {
                        if (r.Start.IChar < Tb[r.Start.ILine].Count)
                            yield return r.Start;
                    } while (r.GoRight() && r.Start < startPlace);
            }
        }

        #region ColumnSelectionMode

        private Range GetIntersectionWith_ColumnSelectionMode(Range range)
        {
            if (range.Start.ILine != range.End.ILine)
                return new Range(Tb, Start, Start);
            var rect = Bounds;
            if (range.Start.ILine < rect.IStartLine || range.Start.ILine > rect.IEndLine)
                return new Range(Tb, Start, Start);

            return
                new Range(Tb, rect.IStartChar, range.Start.ILine, rect.IEndChar, range.Start.ILine).GetIntersectionWith(
                    range);
        }

        private bool GoRightThroughFolded_ColumnSelectionMode()
        {
            var boundes = Bounds;
            var endOfLines = true;
            for (var iLine = boundes.IStartLine; iLine <= boundes.IEndLine; iLine++)
                if (boundes.IEndChar < Tb[iLine].Count)
                {
                    endOfLines = false;
                    break;
                }

            if (endOfLines)
                return false;

            var start = Start;
            var end = End;
            start.Offset(1, 0);
            end.Offset(1, 0);
            BeginUpdate();
            Start = start;
            End = end;
            EndUpdate();

            return true;
        }

        private IEnumerable<Place> GetEnumerator_ColumnSelectionMode()
        {
            var bounds = Bounds;
            if (bounds.IStartLine < 0) yield break;
            //
            for (var y = bounds.IStartLine; y <= bounds.IEndLine; y++)
            {
                for (var x = bounds.IStartChar; x < bounds.IEndChar; x++)
                {
                    if (x < Tb[y].Count)
                        yield return new Place(x, y);
                }
            }
        }

        private string TextColumnSelectionMode
        {
            get
            {
                var sb = new StringBuilder();
                var bounds = Bounds;
                if (bounds.IStartLine < 0) return "";
                //
                for (var y = bounds.IStartLine; y <= bounds.IEndLine; y++)
                {
                    for (var x = bounds.IStartChar; x < bounds.IEndChar; x++)
                    {
                        if (x < Tb[y].Count)
                            sb.Append(Tb[y][x].C);
                    }
                    if (bounds.IEndLine != bounds.IStartLine && y != bounds.IEndLine)
                        sb.AppendLine();
                }

                return sb.ToString();
            }
        }

        internal void GoDown_ColumnSelectionMode()
        {
            var iLine = Tb.FindNextVisibleLine(End.ILine);
            End = new Place(End.IChar, iLine);
        }

        internal void GoUp_ColumnSelectionMode()
        {
            var iLine = Tb.FindPrevVisibleLine(End.ILine);
            End = new Place(End.IChar, iLine);
        }

        internal void GoRight_ColumnSelectionMode()
        {
            End = new Place(End.IChar + 1, End.ILine);
        }

        internal void GoLeft_ColumnSelectionMode()
        {
            if (End.IChar > 0)
                End = new Place(End.IChar - 1, End.ILine);
        }

        #endregion
    }

    public struct RangeRect
    {
        public RangeRect(int startLine, int startChar, int endLine, int endChar)
        {
            IStartLine = startLine;
            IStartChar = startChar;
            IEndLine = endLine;
            IEndChar = endChar;
        }

        public int IStartLine;
        public int IStartChar;
        public int IEndLine;
        public int IEndChar;
    }
}