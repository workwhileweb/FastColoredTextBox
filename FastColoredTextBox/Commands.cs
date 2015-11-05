using System;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Insert single char
    /// </summary>
    /// <remarks>This operation includes also insertion of new line and removing char by backspace</remarks>
    public class InsertCharCommand : UndoableCommand
    {
        private char _deletedChar = '\x0';
        public char C;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="c">Inserting char</param>
        public InsertCharCommand(TextSource ts, char c) : base(ts)
        {
            C = c;
        }

        /// <summary>
        ///     Undo operation
        /// </summary>
        public override void Undo()
        {
            Ts.OnTextChanging();
            switch (C)
            {
                case '\n':
                    MergeLines(Sel.Start.ILine, Ts);
                    break;
                case '\r':
                    break;
                case '\b':
                    Ts.CurrentTb.Selection.Start = LastSel.Start;
                    var cc = '\x0';
                    if (_deletedChar != '\x0')
                    {
                        Ts.CurrentTb.ExpandBlock(Ts.CurrentTb.Selection.Start.ILine);
                        InsertChar(_deletedChar, ref cc, Ts);
                    }
                    break;
                case '\t':
                    Ts.CurrentTb.ExpandBlock(Sel.Start.ILine);
                    for (var i = Sel.FromX; i < LastSel.FromX; i++)
                        Ts[Sel.Start.ILine].RemoveAt(Sel.Start.IChar);
                    Ts.CurrentTb.Selection.Start = Sel.Start;
                    break;
                default:
                    Ts.CurrentTb.ExpandBlock(Sel.Start.ILine);
                    Ts[Sel.Start.ILine].RemoveAt(Sel.Start.IChar);
                    Ts.CurrentTb.Selection.Start = Sel.Start;
                    break;
            }

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(Sel.Start.ILine, Sel.Start.ILine));

            base.Undo();
        }

        /// <summary>
        ///     Execute operation
        /// </summary>
        public override void Execute()
        {
            Ts.CurrentTb.ExpandBlock(Ts.CurrentTb.Selection.Start.ILine);
            var s = C.ToString();
            Ts.OnTextChanging(ref s);
            if (s.Length == 1)
                C = s[0];

            if (string.IsNullOrEmpty(s))
                throw new ArgumentOutOfRangeException();


            if (Ts.Count == 0)
                InsertLine(Ts);
            InsertChar(C, ref _deletedChar, Ts);

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(Ts.CurrentTb.Selection.Start.ILine,
                Ts.CurrentTb.Selection.Start.ILine));
            base.Execute();
        }

        internal static void InsertChar(char c, ref char deletedChar, TextSource ts)
        {
            var tb = ts.CurrentTb;

            switch (c)
            {
                case '\n':
                    if (!ts.CurrentTb.AllowInsertRemoveLines)
                        throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                    if (ts.Count == 0)
                        InsertLine(ts);
                    InsertLine(ts);
                    break;
                case '\r':
                    break;
                case '\b': //backspace
                    if (tb.Selection.Start.IChar == 0 && tb.Selection.Start.ILine == 0)
                        return;
                    if (tb.Selection.Start.IChar == 0)
                    {
                        if (!ts.CurrentTb.AllowInsertRemoveLines)
                            throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                        if (tb.LineInfos[tb.Selection.Start.ILine - 1].VisibleState != VisibleState.Visible)
                            tb.ExpandBlock(tb.Selection.Start.ILine - 1);
                        deletedChar = '\n';
                        MergeLines(tb.Selection.Start.ILine - 1, ts);
                    }
                    else
                    {
                        deletedChar = ts[tb.Selection.Start.ILine][tb.Selection.Start.IChar - 1].C;
                        ts[tb.Selection.Start.ILine].RemoveAt(tb.Selection.Start.IChar - 1);
                        tb.Selection.Start = new Place(tb.Selection.Start.IChar - 1, tb.Selection.Start.ILine);
                    }
                    break;
                case '\t':
                    var spaceCountNextTabStop = tb.TabLength - (tb.Selection.Start.IChar%tb.TabLength);
                    if (spaceCountNextTabStop == 0)
                        spaceCountNextTabStop = tb.TabLength;

                    for (var i = 0; i < spaceCountNextTabStop; i++)
                        ts[tb.Selection.Start.ILine].Insert(tb.Selection.Start.IChar, new Char(' '));

                    tb.Selection.Start = new Place(tb.Selection.Start.IChar + spaceCountNextTabStop,
                        tb.Selection.Start.ILine);
                    break;
                default:
                    ts[tb.Selection.Start.ILine].Insert(tb.Selection.Start.IChar, new Char(c));
                    tb.Selection.Start = new Place(tb.Selection.Start.IChar + 1, tb.Selection.Start.ILine);
                    break;
            }
        }

        internal static void InsertLine(TextSource ts)
        {
            var tb = ts.CurrentTb;

            if (!tb.Multiline && tb.LinesCount > 0)
                return;

            if (ts.Count == 0)
                ts.InsertLine(0, ts.CreateLine());
            else
                BreakLines(tb.Selection.Start.ILine, tb.Selection.Start.IChar, ts);

            tb.Selection.Start = new Place(0, tb.Selection.Start.ILine + 1);
            ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        ///     Merge lines i and i+1
        /// </summary>
        internal static void MergeLines(int i, TextSource ts)
        {
            var tb = ts.CurrentTb;

            if (i + 1 >= ts.Count)
                return;
            tb.ExpandBlock(i);
            tb.ExpandBlock(i + 1);
            var pos = ts[i].Count;
            //
            /*
            if(ts[i].Count == 0)
                ts.RemoveLine(i);
            else*/
            if (ts[i + 1].Count == 0)
                ts.RemoveLine(i + 1);
            else
            {
                ts[i].AddRange(ts[i + 1]);
                ts.RemoveLine(i + 1);
            }
            tb.Selection.Start = new Place(pos, i);
            ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        internal static void BreakLines(int iLine, int pos, TextSource ts)
        {
            var newLine = ts.CreateLine();
            for (var i = pos; i < ts[iLine].Count; i++)
                newLine.Add(ts[iLine][i]);
            ts[iLine].RemoveRange(pos, ts[iLine].Count - pos);
            //
            ts.InsertLine(iLine + 1, newLine);
        }

        public override UndoableCommand Clone()
        {
            return new InsertCharCommand(Ts, C);
        }
    }

    /// <summary>
    ///     Insert text
    /// </summary>
    public class InsertTextCommand : UndoableCommand
    {
        public string InsertedText;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="insertedText">Text for inserting</param>
        public InsertTextCommand(TextSource ts, string insertedText) : base(ts)
        {
            InsertedText = insertedText;
        }

        /// <summary>
        ///     Undo operation
        /// </summary>
        public override void Undo()
        {
            Ts.CurrentTb.Selection.Start = Sel.Start;
            Ts.CurrentTb.Selection.End = LastSel.Start;
            Ts.OnTextChanging();
            ClearSelectedCommand.ClearSelected(Ts);
            base.Undo();
        }

        /// <summary>
        ///     Execute operation
        /// </summary>
        public override void Execute()
        {
            Ts.OnTextChanging(ref InsertedText);
            InsertText(InsertedText, Ts);
            base.Execute();
        }

        internal static void InsertText(string insertedText, TextSource ts)
        {
            var tb = ts.CurrentTb;
            try
            {
                tb.Selection.BeginUpdate();
                var cc = '\x0';

                if (ts.Count == 0)
                {
                    InsertCharCommand.InsertLine(ts);
                    tb.Selection.Start = Place.Empty;
                }
                tb.ExpandBlock(tb.Selection.Start.ILine);
                var len = insertedText.Length;
                for (var i = 0; i < len; i++)
                {
                    var c = insertedText[i];
                    if (c == '\r' && (i >= len - 1 || insertedText[i + 1] != '\n'))
                        InsertCharCommand.InsertChar('\n', ref cc, ts);
                    else
                        InsertCharCommand.InsertChar(c, ref cc, ts);
                }
                ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
            }
            finally
            {
                tb.Selection.EndUpdate();
            }
        }

        public override UndoableCommand Clone()
        {
            return new InsertTextCommand(Ts, InsertedText);
        }
    }

    /// <summary>
    ///     Insert text into given ranges
    /// </summary>
    public class ReplaceTextCommand : UndoableCommand
    {
        private string _insertedText;
        private readonly List<string> _prevText = new List<string>();
        private readonly List<Range> _ranges;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="ranges">List of ranges for replace</param>
        /// <param name="insertedText">Text for inserting</param>
        public ReplaceTextCommand(TextSource ts, List<Range> ranges, string insertedText)
            : base(ts)
        {
            //sort ranges by place
            ranges.Sort((r1, r2) =>
            {
                if (r1.Start.ILine == r2.Start.ILine)
                    return r1.Start.IChar.CompareTo(r2.Start.IChar);
                return r1.Start.ILine.CompareTo(r2.Start.ILine);
            });
            //
            _ranges = ranges;
            _insertedText = insertedText;
            LastSel = Sel = new RangeInfo(ts.CurrentTb.Selection);
        }

        /// <summary>
        ///     Undo operation
        /// </summary>
        public override void Undo()
        {
            var tb = Ts.CurrentTb;

            Ts.OnTextChanging();
            tb.BeginUpdate();

            tb.Selection.BeginUpdate();
            for (var i = 0; i < _ranges.Count; i++)
            {
                tb.Selection.Start = _ranges[i].Start;
                for (var j = 0; j < _insertedText.Length; j++)
                    tb.Selection.GoRight(true);
                ClearSelected(Ts);
                InsertTextCommand.InsertText(_prevText[_prevText.Count - i - 1], Ts);
            }
            tb.Selection.EndUpdate();
            tb.EndUpdate();

            if (_ranges.Count > 0)
                Ts.OnTextChanged(_ranges[0].Start.ILine, _ranges[_ranges.Count - 1].End.ILine);

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        ///     Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTb;
            _prevText.Clear();

            Ts.OnTextChanging(ref _insertedText);

            tb.Selection.BeginUpdate();
            tb.BeginUpdate();
            for (var i = _ranges.Count - 1; i >= 0; i--)
            {
                tb.Selection.Start = _ranges[i].Start;
                tb.Selection.End = _ranges[i].End;
                _prevText.Add(tb.Selection.Text);
                ClearSelected(Ts);
                if (_insertedText != "")
                    InsertTextCommand.InsertText(_insertedText, Ts);
            }
            if (_ranges.Count > 0)
                Ts.OnTextChanged(_ranges[0].Start.ILine, _ranges[_ranges.Count - 1].End.ILine);
            tb.EndUpdate();
            tb.Selection.EndUpdate();
            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

            LastSel = new RangeInfo(tb.Selection);
        }

        public override UndoableCommand Clone()
        {
            return new ReplaceTextCommand(Ts, new List<Range>(_ranges), _insertedText);
        }

        internal static void ClearSelected(TextSource ts)
        {
            var tb = ts.CurrentTb;

            tb.Selection.Normalize();

            var start = tb.Selection.Start;
            var end = tb.Selection.End;
            var fromLine = Math.Min(end.ILine, start.ILine);
            var toLine = Math.Max(end.ILine, start.ILine);
            var fromChar = tb.Selection.FromX;
            var toChar = tb.Selection.ToX;
            if (fromLine < 0) return;
            //
            if (fromLine == toLine)
                ts[fromLine].RemoveRange(fromChar, toChar - fromChar);
            else
            {
                ts[fromLine].RemoveRange(fromChar, ts[fromLine].Count - fromChar);
                ts[toLine].RemoveRange(0, toChar);
                ts.RemoveLine(fromLine + 1, toLine - fromLine - 1);
                InsertCharCommand.MergeLines(fromLine, ts);
            }
        }
    }

    /// <summary>
    ///     Clear selected text
    /// </summary>
    public class ClearSelectedCommand : UndoableCommand
    {
        private string _deletedText;

        /// <summary>
        ///     Construstor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        public ClearSelectedCommand(TextSource ts) : base(ts)
        {
        }

        /// <summary>
        ///     Undo operation
        /// </summary>
        public override void Undo()
        {
            Ts.CurrentTb.Selection.Start = new Place(Sel.FromX, Math.Min(Sel.Start.ILine, Sel.End.ILine));
            Ts.OnTextChanging();
            InsertTextCommand.InsertText(_deletedText, Ts);
            Ts.OnTextChanged(Sel.Start.ILine, Sel.End.ILine);
            Ts.CurrentTb.Selection.Start = Sel.Start;
            Ts.CurrentTb.Selection.End = Sel.End;
        }

        /// <summary>
        ///     Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTb;

            string temp = null;
            Ts.OnTextChanging(ref temp);
            if (temp == "")
                throw new ArgumentOutOfRangeException();

            _deletedText = tb.Selection.Text;
            ClearSelected(Ts);
            LastSel = new RangeInfo(tb.Selection);
            Ts.OnTextChanged(LastSel.Start.ILine, LastSel.Start.ILine);
        }

        internal static void ClearSelected(TextSource ts)
        {
            var tb = ts.CurrentTb;

            var start = tb.Selection.Start;
            var end = tb.Selection.End;
            var fromLine = Math.Min(end.ILine, start.ILine);
            var toLine = Math.Max(end.ILine, start.ILine);
            var fromChar = tb.Selection.FromX;
            var toChar = tb.Selection.ToX;
            if (fromLine < 0) return;
            //
            if (fromLine == toLine)
                ts[fromLine].RemoveRange(fromChar, toChar - fromChar);
            else
            {
                ts[fromLine].RemoveRange(fromChar, ts[fromLine].Count - fromChar);
                ts[toLine].RemoveRange(0, toChar);
                ts.RemoveLine(fromLine + 1, toLine - fromLine - 1);
                InsertCharCommand.MergeLines(fromLine, ts);
            }
            //
            tb.Selection.Start = new Place(fromChar, fromLine);
            //
            ts.NeedRecalc(new TextSource.TextChangedEventArgs(fromLine, toLine));
        }

        public override UndoableCommand Clone()
        {
            return new ClearSelectedCommand(Ts);
        }
    }

    /// <summary>
    ///     Replaces text
    /// </summary>
    public class ReplaceMultipleTextCommand : UndoableCommand
    {
        private readonly List<string> _prevText = new List<string>();
        private readonly List<ReplaceRange> _ranges;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="ts">Underlaying textsource</param>
        /// <param name="ranges">List of ranges for replace</param>
        public ReplaceMultipleTextCommand(TextSource ts, List<ReplaceRange> ranges)
            : base(ts)
        {
            //sort ranges by place
            ranges.Sort((r1, r2) =>
            {
                if (r1.ReplacedRange.Start.ILine == r2.ReplacedRange.Start.ILine)
                    return r1.ReplacedRange.Start.IChar.CompareTo(r2.ReplacedRange.Start.IChar);
                return r1.ReplacedRange.Start.ILine.CompareTo(r2.ReplacedRange.Start.ILine);
            });
            //
            _ranges = ranges;
            LastSel = Sel = new RangeInfo(ts.CurrentTb.Selection);
        }

        /// <summary>
        ///     Undo operation
        /// </summary>
        public override void Undo()
        {
            var tb = Ts.CurrentTb;

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            for (var i = 0; i < _ranges.Count; i++)
            {
                tb.Selection.Start = _ranges[i].ReplacedRange.Start;
                for (var j = 0; j < _ranges[i].ReplaceText.Length; j++)
                    tb.Selection.GoRight(true);
                ClearSelectedCommand.ClearSelected(Ts);
                var prevTextIndex = _ranges.Count - 1 - i;
                InsertTextCommand.InsertText(_prevText[prevTextIndex], Ts);
                Ts.OnTextChanged(_ranges[i].ReplacedRange.Start.ILine, _ranges[i].ReplacedRange.Start.ILine);
            }
            tb.Selection.EndUpdate();

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        ///     Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTb;
            _prevText.Clear();

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            for (var i = _ranges.Count - 1; i >= 0; i--)
            {
                tb.Selection.Start = _ranges[i].ReplacedRange.Start;
                tb.Selection.End = _ranges[i].ReplacedRange.End;
                _prevText.Add(tb.Selection.Text);
                ClearSelectedCommand.ClearSelected(Ts);
                InsertTextCommand.InsertText(_ranges[i].ReplaceText, Ts);
                Ts.OnTextChanged(_ranges[i].ReplacedRange.Start.ILine, _ranges[i].ReplacedRange.End.ILine);
            }
            tb.Selection.EndUpdate();
            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

            LastSel = new RangeInfo(tb.Selection);
        }

        public override UndoableCommand Clone()
        {
            return new ReplaceMultipleTextCommand(Ts, new List<ReplaceRange>(_ranges));
        }

        public class ReplaceRange
        {
            public Range ReplacedRange { get; set; }
            public string ReplaceText { get; set; }
        }
    }

    /// <summary>
    ///     Removes lines
    /// </summary>
    public class RemoveLinesCommand : UndoableCommand
    {
        private readonly List<int> _iLines;
        private readonly List<string> _prevText = new List<string>();

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="ranges">List of ranges for replace</param>
        /// <param name="insertedText">Text for inserting</param>
        public RemoveLinesCommand(TextSource ts, List<int> iLines)
            : base(ts)
        {
            //sort iLines
            iLines.Sort();
            //
            _iLines = iLines;
            LastSel = Sel = new RangeInfo(ts.CurrentTb.Selection);
        }

        /// <summary>
        ///     Undo operation
        /// </summary>
        public override void Undo()
        {
            var tb = Ts.CurrentTb;

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            //tb.BeginUpdate();
            for (var i = 0; i < _iLines.Count; i++)
            {
                var iLine = _iLines[i];

                if (iLine < Ts.Count)
                    tb.Selection.Start = new Place(0, iLine);
                else
                    tb.Selection.Start = new Place(Ts[Ts.Count - 1].Count, Ts.Count - 1);

                InsertCharCommand.InsertLine(Ts);
                tb.Selection.Start = new Place(0, iLine);
                var text = _prevText[_prevText.Count - i - 1];
                InsertTextCommand.InsertText(text, Ts);
                Ts[iLine].IsChanged = true;
                if (iLine < Ts.Count - 1)
                    Ts[iLine + 1].IsChanged = true;
                else
                    Ts[iLine - 1].IsChanged = true;
                if (text.Trim() != string.Empty)
                    Ts.OnTextChanged(iLine, iLine);
            }
            //tb.EndUpdate();
            tb.Selection.EndUpdate();

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        ///     Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTb;
            _prevText.Clear();

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            for (var i = _iLines.Count - 1; i >= 0; i--)
            {
                var iLine = _iLines[i];

                _prevText.Add(Ts[iLine].Text); //backward
                Ts.RemoveLine(iLine);
                //ts.OnTextChanged(ranges[i].Start.iLine, ranges[i].End.iLine);
            }
            tb.Selection.Start = new Place(0, 0);
            tb.Selection.EndUpdate();
            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

            LastSel = new RangeInfo(tb.Selection);
        }

        public override UndoableCommand Clone()
        {
            return new RemoveLinesCommand(Ts, new List<int>(_iLines));
        }
    }

    /// <summary>
    ///     Wrapper for multirange commands
    /// </summary>
    public class MultiRangeCommand : UndoableCommand
    {
        private readonly UndoableCommand _cmd;
        private readonly List<UndoableCommand> _commandsByRanges = new List<UndoableCommand>();
        private readonly Range _range;

        public MultiRangeCommand(UndoableCommand command) : base(command.Ts)
        {
            _cmd = command;
            _range = Ts.CurrentTb.Selection.Clone();
        }

        public override void Execute()
        {
            _commandsByRanges.Clear();
            var prevSelection = _range.Clone();
            var iChar = -1;
            var iStartLine = prevSelection.Start.ILine;
            var iEndLine = prevSelection.End.ILine;
            Ts.CurrentTb.Selection.ColumnSelectionMode = false;
            Ts.CurrentTb.Selection.BeginUpdate();
            Ts.CurrentTb.BeginUpdate();
            Ts.CurrentTb.AllowInsertRemoveLines = false;
            try
            {
                if (_cmd is InsertTextCommand)
                    ExecuteInsertTextCommand(ref iChar, (_cmd as InsertTextCommand).InsertedText);
                else if (_cmd is InsertCharCommand && (_cmd as InsertCharCommand).C != '\x0' &&
                         (_cmd as InsertCharCommand).C != '\b') //if not DEL or BACKSPACE
                    ExecuteInsertTextCommand(ref iChar, (_cmd as InsertCharCommand).C.ToString());
                else
                    ExecuteCommand(ref iChar);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            finally
            {
                Ts.CurrentTb.AllowInsertRemoveLines = true;
                Ts.CurrentTb.EndUpdate();

                Ts.CurrentTb.Selection = _range;
                if (iChar >= 0)
                {
                    Ts.CurrentTb.Selection.Start = new Place(iChar, iStartLine);
                    Ts.CurrentTb.Selection.End = new Place(iChar, iEndLine);
                }
                Ts.CurrentTb.Selection.ColumnSelectionMode = true;
                Ts.CurrentTb.Selection.EndUpdate();
            }
        }

        private void ExecuteInsertTextCommand(ref int iChar, string text)
        {
            var lines = text.Split('\n');
            var iLine = 0;
            foreach (var r in _range.GetSubRanges(true))
            {
                var line = Ts.CurrentTb[r.Start.ILine];
                var lineIsEmpty = r.End < r.Start && line.StartSpacesCount == line.Count;
                if (!lineIsEmpty)
                {
                    var insertedText = lines[iLine%lines.Length];
                    if (r.End < r.Start && insertedText != "")
                    {
                        //add forwarding spaces
                        insertedText = new string(' ', r.Start.IChar - r.End.IChar) + insertedText;
                        r.Start = r.End;
                    }
                    Ts.CurrentTb.Selection = r;
                    var c = new InsertTextCommand(Ts, insertedText);
                    c.Execute();
                    if (Ts.CurrentTb.Selection.End.IChar > iChar)
                        iChar = Ts.CurrentTb.Selection.End.IChar;
                    _commandsByRanges.Add(c);
                }
                iLine++;
            }
        }

        private void ExecuteCommand(ref int iChar)
        {
            foreach (var r in _range.GetSubRanges(false))
            {
                Ts.CurrentTb.Selection = r;
                var c = _cmd.Clone();
                c.Execute();
                if (Ts.CurrentTb.Selection.End.IChar > iChar)
                    iChar = Ts.CurrentTb.Selection.End.IChar;
                _commandsByRanges.Add(c);
            }
        }

        public override void Undo()
        {
            Ts.CurrentTb.BeginUpdate();
            Ts.CurrentTb.Selection.BeginUpdate();
            try
            {
                for (var i = _commandsByRanges.Count - 1; i >= 0; i--)
                    _commandsByRanges[i].Undo();
            }
            finally
            {
                Ts.CurrentTb.Selection.EndUpdate();
                Ts.CurrentTb.EndUpdate();
            }
            Ts.CurrentTb.Selection = _range.Clone();
            Ts.CurrentTb.OnTextChanged(_range);
            Ts.CurrentTb.OnSelectionChanged();
            Ts.CurrentTb.Selection.ColumnSelectionMode = true;
        }

        public override UndoableCommand Clone()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Remembers current selection and restore it after Undo
    /// </summary>
    public class SelectCommand : UndoableCommand
    {
        public SelectCommand(TextSource ts) : base(ts)
        {
        }

        public override void Execute()
        {
            //remember selection
            LastSel = new RangeInfo(Ts.CurrentTb.Selection);
        }

        protected override void OnTextChanged(bool invert)
        {
        }

        public override void Undo()
        {
            //restore selection
            Ts.CurrentTb.Selection = new Range(Ts.CurrentTb, LastSel.Start, LastSel.End);
        }

        public override UndoableCommand Clone()
        {
            var result = new SelectCommand(Ts);
            if (LastSel != null)
                result.LastSel = new RangeInfo(new Range(Ts.CurrentTb, LastSel.Start, LastSel.End));
            return result;
        }
    }
}