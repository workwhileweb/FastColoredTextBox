using System;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    public class CommandManager
    {
        private readonly int _maxHistoryLength = 200;

        private int _autoUndoCommands;
        private readonly LimitedStack<UndoableCommand> _history;
        private readonly Stack<UndoableCommand> _redoStack = new Stack<UndoableCommand>();

        protected int DisabledCommands;

        public CommandManager(TextSource ts)
        {
            _history = new LimitedStack<UndoableCommand>(_maxHistoryLength);
            TextSource = ts;
            UndoRedoStackIsEnabled = true;
        }

        public TextSource TextSource { get; }
        public bool UndoRedoStackIsEnabled { get; set; }

        public bool UndoEnabled
        {
            get { return _history.Count > 0; }
        }

        public bool RedoEnabled
        {
            get { return _redoStack.Count > 0; }
        }

        public virtual void ExecuteCommand(Command cmd)
        {
            if (DisabledCommands > 0)
                return;

            //multirange ?
            if (cmd.Ts.CurrentTb.Selection.ColumnSelectionMode)
                if (cmd is UndoableCommand)
                    //make wrapper
                    cmd = new MultiRangeCommand((UndoableCommand) cmd);


            if (cmd is UndoableCommand)
            {
                //if range is ColumnRange, then create wrapper
                (cmd as UndoableCommand).AutoUndo = _autoUndoCommands > 0;
                _history.Push(cmd as UndoableCommand);
            }

            try
            {
                cmd.Execute();
            }
            catch (ArgumentOutOfRangeException)
            {
                //OnTextChanging cancels enter of the text
                if (cmd is UndoableCommand)
                    _history.Pop();
            }
            //
            if (!UndoRedoStackIsEnabled)
                ClearHistory();
            //
            _redoStack.Clear();
            //
            TextSource.CurrentTb.OnUndoRedoStateChanged();
        }

        public void Undo()
        {
            if (_history.Count > 0)
            {
                var cmd = _history.Pop();
                //
                BeginDisableCommands(); //prevent text changing into handlers
                try
                {
                    cmd.Undo();
                }
                finally
                {
                    EndDisableCommands();
                }
                //
                _redoStack.Push(cmd);
            }

            //undo next autoUndo command
            if (_history.Count > 0)
            {
                if (_history.Peek().AutoUndo)
                    Undo();
            }

            TextSource.CurrentTb.OnUndoRedoStateChanged();
        }

        private void EndDisableCommands()
        {
            DisabledCommands--;
        }

        private void BeginDisableCommands()
        {
            DisabledCommands++;
        }

        public void EndAutoUndoCommands()
        {
            _autoUndoCommands--;
            if (_autoUndoCommands == 0)
                if (_history.Count > 0)
                    _history.Peek().AutoUndo = false;
        }

        public void BeginAutoUndoCommands()
        {
            _autoUndoCommands++;
        }

        internal void ClearHistory()
        {
            _history.Clear();
            _redoStack.Clear();
            TextSource.CurrentTb.OnUndoRedoStateChanged();
        }

        internal void Redo()
        {
            if (_redoStack.Count == 0)
                return;
            UndoableCommand cmd;
            BeginDisableCommands(); //prevent text changing into handlers
            try
            {
                cmd = _redoStack.Pop();
                if (TextSource.CurrentTb.Selection.ColumnSelectionMode)
                    TextSource.CurrentTb.Selection.ColumnSelectionMode = false;
                TextSource.CurrentTb.Selection.Start = cmd.Sel.Start;
                TextSource.CurrentTb.Selection.End = cmd.Sel.End;
                cmd.Execute();
                _history.Push(cmd);
            }
            finally
            {
                EndDisableCommands();
            }

            //redo command after autoUndoable command
            if (cmd.AutoUndo)
                Redo();

            TextSource.CurrentTb.OnUndoRedoStateChanged();
        }
    }

    public abstract class Command
    {
        public TextSource Ts;
        public abstract void Execute();
    }

    internal class RangeInfo
    {
        public RangeInfo(Range r)
        {
            Start = r.Start;
            End = r.End;
        }

        public Place Start { get; set; }
        public Place End { get; set; }

        internal int FromX
        {
            get
            {
                if (End.ILine < Start.ILine) return End.IChar;
                if (End.ILine > Start.ILine) return Start.IChar;
                return Math.Min(End.IChar, Start.IChar);
            }
        }
    }

    public abstract class UndoableCommand : Command
    {
        internal bool AutoUndo;
        internal RangeInfo LastSel;
        internal RangeInfo Sel;

        public UndoableCommand(TextSource ts)
        {
            Ts = ts;
            Sel = new RangeInfo(ts.CurrentTb.Selection);
        }

        public virtual void Undo()
        {
            OnTextChanged(true);
        }

        public override void Execute()
        {
            LastSel = new RangeInfo(Ts.CurrentTb.Selection);
            OnTextChanged(false);
        }

        protected virtual void OnTextChanged(bool invert)
        {
            var b = Sel.Start.ILine < LastSel.Start.ILine;
            if (invert)
            {
                if (b)
                    Ts.OnTextChanged(Sel.Start.ILine, Sel.Start.ILine);
                else
                    Ts.OnTextChanged(Sel.Start.ILine, LastSel.Start.ILine);
            }
            else
            {
                if (b)
                    Ts.OnTextChanged(Sel.Start.ILine, LastSel.Start.ILine);
                else
                    Ts.OnTextChanged(LastSel.Start.ILine, LastSel.Start.ILine);
            }
        }

        public abstract UndoableCommand Clone();
    }
}