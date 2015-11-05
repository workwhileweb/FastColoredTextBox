//#define debug

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     This class contains the source text (chars and styles).
    ///     It stores a text lines, the manager of commands, undo/redo stack, styles.
    /// </summary>
    public class FileTextSource : TextSource, IDisposable
    {
        private Encoding _fileEncoding;
        private FileStream _fs;
        private List<int> _sourceFileLinePositions = new List<int>();
        private readonly Timer _timer = new Timer();

        public FileTextSource(FastColoredTextBox currentTb)
            : base(currentTb)
        {
            _timer.Interval = 10000;
            _timer.Tick += timer_Tick;
            _timer.Enabled = true;

            SaveEol = Environment.NewLine;
        }

        /// <summary>
        ///     End Of Line characters used for saving
        /// </summary>
        public string SaveEol { get; set; }

        public override Line this[int i]
        {
            get
            {
                if (Lines[i] != null)
                    return Lines[i];
                LoadLineFromSourceFile(i);

                return Lines[i];
            }
            set { throw new NotImplementedException(); }
        }

        public override void Dispose()
        {
            if (_fs != null)
                _fs.Dispose();

            _timer.Dispose();
        }

        /// <summary>
        ///     Occurs when need to display line in the textbox
        /// </summary>
        public event EventHandler<LineNeededEventArgs> LineNeeded;

        /// <summary>
        ///     Occurs when need to save line in the file
        /// </summary>
        public event EventHandler<LinePushedEventArgs> LinePushed;

        private void timer_Tick(object sender, EventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                UnloadUnusedLines();
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private void UnloadUnusedLines()
        {
            const int margin = 2000;
            var iStartVisibleLine = CurrentTb.VisibleRange.Start.ILine;
            var iFinishVisibleLine = CurrentTb.VisibleRange.End.ILine;

            var count = 0;
            for (var i = 0; i < Count; i++)
                if (Lines[i] != null && !Lines[i].IsChanged && Math.Abs(i - iFinishVisibleLine) > margin)
                {
                    Lines[i] = null;
                    count++;
                }
#if debug
            Console.WriteLine("UnloadUnusedLines: " + count);
            #endif
        }

        public void OpenFile(string fileName, Encoding enc)
        {
            Clear();

            if (_fs != null)
                _fs.Dispose();

            SaveEol = Environment.NewLine;

            //read lines of file
            _fs = new FileStream(fileName, FileMode.Open);
            var length = _fs.Length;
            //read signature
            enc = DefineEncoding(enc, _fs);
            var shift = DefineShift(enc);
            //first line
            _sourceFileLinePositions.Add((int) _fs.Position);
            Lines.Add(null);
            //other lines
            _sourceFileLinePositions.Capacity = (int) (length/7 + 1000);
            var prev = 0;
            while (_fs.Position < length)
            {
                var b = _fs.ReadByte();

                if (b == 10) // \n
                {
                    _sourceFileLinePositions.Add((int) (_fs.Position) + shift);
                    Lines.Add(null);
                }
                else if (prev == 13) // \r (Mac format)
                {
                    _sourceFileLinePositions.Add((int) (_fs.Position - 1) + shift);
                    Lines.Add(null);
                    SaveEol = "\r";
                }

                prev = b;
            }

            if (prev == 13)
            {
                _sourceFileLinePositions.Add((int) (_fs.Position) + shift);
                Lines.Add(null);
            }

            if (length > 2000000)
                GC.Collect();

            var temp = new Line[100];

            var c = Lines.Count;
            Lines.AddRange(temp);
            Lines.TrimExcess();
            Lines.RemoveRange(c, temp.Length);


            var temp2 = new int[100];
            c = Lines.Count;
            _sourceFileLinePositions.AddRange(temp2);
            _sourceFileLinePositions.TrimExcess();
            _sourceFileLinePositions.RemoveRange(c, temp.Length);


            _fileEncoding = enc;

            OnLineInserted(0, Count);
            //load first lines for calc width of the text
            var linesCount = Math.Min(Lines.Count, CurrentTb.ClientRectangle.Height/CurrentTb.CharHeight);
            for (var i = 0; i < linesCount; i++)
                LoadLineFromSourceFile(i);
            //
            NeedRecalc(new TextChangedEventArgs(0, linesCount - 1));
            if (CurrentTb.WordWrap)
                OnRecalcWordWrap(new TextChangedEventArgs(0, linesCount - 1));
        }

        private int DefineShift(Encoding enc)
        {
            if (enc.IsSingleByte)
                return 0;

            if (enc.HeaderName == "unicodeFFFE")
                return 0; //UTF16 BE

            if (enc.HeaderName == "utf-16")
                return 1; //UTF16 LE

            if (enc.HeaderName == "utf-32BE")
                return 0; //UTF32 BE

            if (enc.HeaderName == "utf-32")
                return 3; //UTF32 LE

            return 0;
        }

        private static Encoding DefineEncoding(Encoding enc, FileStream fs)
        {
            var bytesPerSignature = 0;
            var signature = new byte[4];
            var c = fs.Read(signature, 0, 4);
            if (signature[0] == 0xFF && signature[1] == 0xFE && signature[2] == 0x00 && signature[3] == 0x00 && c >= 4)
            {
                enc = Encoding.UTF32; //UTF32 LE
                bytesPerSignature = 4;
            }
            else if (signature[0] == 0x00 && signature[1] == 0x00 && signature[2] == 0xFE && signature[3] == 0xFF)
            {
                enc = new UTF32Encoding(true, true); //UTF32 BE
                bytesPerSignature = 4;
            }
            else if (signature[0] == 0xEF && signature[1] == 0xBB && signature[2] == 0xBF)
            {
                enc = Encoding.UTF8; //UTF8
                bytesPerSignature = 3;
            }
            else if (signature[0] == 0xFE && signature[1] == 0xFF)
            {
                enc = Encoding.BigEndianUnicode; //UTF16 BE
                bytesPerSignature = 2;
            }
            else if (signature[0] == 0xFF && signature[1] == 0xFE)
            {
                enc = Encoding.Unicode; //UTF16 LE
                bytesPerSignature = 2;
            }

            fs.Seek(bytesPerSignature, SeekOrigin.Begin);

            return enc;
        }

        public void CloseFile()
        {
            if (_fs != null)
                try
                {
                    _fs.Dispose();
                }
                catch
                {
                    ;
                }
            _fs = null;
        }

        public override void SaveToFile(string fileName, Encoding enc)
        {
            //
            var newLinePos = new List<int>(Count);
            //create temp file
            var dir = Path.GetDirectoryName(fileName);
            var tempFileName = Path.Combine(dir, Path.GetFileNameWithoutExtension(fileName) + ".tmp");

            var sr = new StreamReader(_fs, _fileEncoding);
            using (var tempFs = new FileStream(tempFileName, FileMode.Create))
            using (var sw = new StreamWriter(tempFs, enc))
            {
                sw.Flush();

                for (var i = 0; i < Count; i++)
                {
                    newLinePos.Add((int) tempFs.Length);

                    var sourceLine = ReadLine(sr, i); //read line from source file
                    string line;

                    var lineIsChanged = Lines[i] != null && Lines[i].IsChanged;

                    if (lineIsChanged)
                        line = Lines[i].Text;
                    else
                        line = sourceLine;

                    //call event handler
                    if (LinePushed != null)
                    {
                        var args = new LinePushedEventArgs(sourceLine, i, lineIsChanged ? line : null);
                        LinePushed(this, args);

                        if (args.SavedText != null)
                            line = args.SavedText;
                    }

                    //save line to file
                    sw.Write(line);

                    if (i < Count - 1)
                        sw.Write(SaveEol);

                    sw.Flush();
                }
            }

            //clear lines buffer
            for (var i = 0; i < Count; i++)
                Lines[i] = null;
            //deattach from source file
            sr.Dispose();
            _fs.Dispose();
            //delete target file
            if (File.Exists(fileName))
                File.Delete(fileName);
            //rename temp file
            File.Move(tempFileName, fileName);

            //binding to new file
            _sourceFileLinePositions = newLinePos;
            _fs = new FileStream(fileName, FileMode.Open);
            _fileEncoding = enc;
        }

        private string ReadLine(StreamReader sr, int i)
        {
            string line;
            var filePos = _sourceFileLinePositions[i];
            if (filePos < 0)
                return "";
            _fs.Seek(filePos, SeekOrigin.Begin);
            sr.DiscardBufferedData();
            line = sr.ReadLine();
            return line;
        }

        public override void ClearIsChanged()
        {
            foreach (var line in Lines)
                if (line != null)
                    line.IsChanged = false;
        }

        private void LoadLineFromSourceFile(int i)
        {
            var line = CreateLine();
            _fs.Seek(_sourceFileLinePositions[i], SeekOrigin.Begin);
            var sr = new StreamReader(_fs, _fileEncoding);

            var s = sr.ReadLine();
            if (s == null)
                s = "";

            //call event handler
            if (LineNeeded != null)
            {
                var args = new LineNeededEventArgs(s, i);
                LineNeeded(this, args);
                s = args.DisplayedLineText;
                if (s == null)
                    return;
            }

            foreach (var c in s)
                line.Add(new Char(c));
            Lines[i] = line;

            if (CurrentTb.WordWrap)
                OnRecalcWordWrap(new TextChangedEventArgs(i, i));
        }

        public override void InsertLine(int index, Line line)
        {
            _sourceFileLinePositions.Insert(index, -1);
            base.InsertLine(index, line);
        }

        public override void RemoveLine(int index, int count)
        {
            _sourceFileLinePositions.RemoveRange(index, count);
            base.RemoveLine(index, count);
        }

        public override void Clear()
        {
            base.Clear();
        }

        public override int GetLineLength(int i)
        {
            if (Lines[i] == null)
                return 0;
            return Lines[i].Count;
        }

        public override bool LineHasFoldingStartMarker(int iLine)
        {
            if (Lines[iLine] == null)
                return false;
            return !string.IsNullOrEmpty(Lines[iLine].FoldingStartMarker);
        }

        public override bool LineHasFoldingEndMarker(int iLine)
        {
            if (Lines[iLine] == null)
                return false;
            return !string.IsNullOrEmpty(Lines[iLine].FoldingEndMarker);
        }

        internal void UnloadLine(int iLine)
        {
            if (Lines[iLine] != null && !Lines[iLine].IsChanged)
                Lines[iLine] = null;
        }
    }

    public class LineNeededEventArgs : EventArgs
    {
        public LineNeededEventArgs(string sourceLineText, int displayedLineIndex)
        {
            SourceLineText = sourceLineText;
            DisplayedLineIndex = displayedLineIndex;
            DisplayedLineText = sourceLineText;
        }

        public string SourceLineText { get; private set; }
        public int DisplayedLineIndex { get; private set; }

        /// <summary>
        ///     This text will be displayed in textbox
        /// </summary>
        public string DisplayedLineText { get; set; }
    }

    public class LinePushedEventArgs : EventArgs
    {
        public LinePushedEventArgs(string sourceLineText, int displayedLineIndex, string displayedLineText)
        {
            SourceLineText = sourceLineText;
            DisplayedLineIndex = displayedLineIndex;
            DisplayedLineText = displayedLineText;
            SavedText = displayedLineText;
        }

        public string SourceLineText { get; private set; }
        public int DisplayedLineIndex { get; private set; }

        /// <summary>
        ///     This property contains only changed text.
        ///     If text of line is not changed, this property contains null.
        /// </summary>
        public string DisplayedLineText { get; private set; }

        /// <summary>
        ///     This text will be saved in the file
        /// </summary>
        public string SavedText { get; set; }
    }
}