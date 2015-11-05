using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Exports colored text as RTF
    /// </summary>
    /// <remarks>At this time only TextStyle renderer is supported. Other styles is not exported.</remarks>
    public class ExportToRtf
    {
        private readonly Dictionary<Color, int> _colorTable = new Dictionary<Color, int>();

        private FastColoredTextBox _tb;

        public ExportToRtf()
        {
            UseOriginalFont = true;
        }

        /// <summary>
        ///     Includes line numbers
        /// </summary>
        public bool IncludeLineNumbers { get; set; }

        /// <summary>
        ///     Use original font
        /// </summary>
        public bool UseOriginalFont { get; set; }

        public string GetRtf(FastColoredTextBox tb)
        {
            _tb = tb;
            var sel = new Range(tb);
            sel.SelectAll();
            return GetRtf(sel);
        }

        public string GetRtf(Range r)
        {
            _tb = r.Tb;
            var styles = new Dictionary<StyleIndex, object>();
            var sb = new StringBuilder();
            var tempSb = new StringBuilder();
            var currentStyleId = StyleIndex.None;
            r.Normalize();
            var currentLine = r.Start.ILine;
            styles[currentStyleId] = null;
            _colorTable.Clear();
            //
            var lineNumberColor = GetColorTableNumber(r.Tb.LineNumberColor);

            if (IncludeLineNumbers)
                tempSb.AppendFormat(@"{{\cf{1} {0}}}\tab", currentLine + 1, lineNumberColor);
            //
            foreach (var p in r)
            {
                var c = r.Tb[p.ILine][p.IChar];
                if (c.Style != currentStyleId)
                {
                    Flush(sb, tempSb, currentStyleId);
                    currentStyleId = c.Style;
                    styles[currentStyleId] = null;
                }

                if (p.ILine != currentLine)
                {
                    for (var i = currentLine; i < p.ILine; i++)
                    {
                        tempSb.AppendLine(@"\line");
                        if (IncludeLineNumbers)
                            tempSb.AppendFormat(@"{{\cf{1} {0}}}\tab", i + 2, lineNumberColor);
                    }
                    currentLine = p.ILine;
                }
                switch (c.C)
                {
                    case '\\':
                        tempSb.Append(@"\\");
                        break;
                    case '{':
                        tempSb.Append(@"\{");
                        break;
                    case '}':
                        tempSb.Append(@"\}");
                        break;
                    default:
                        var ch = c.C;
                        var code = (int) ch;
                        if (code < 128)
                            tempSb.Append(c.C);
                        else
                            tempSb.AppendFormat(@"{{\u{0}}}", code);
                        break;
                }
            }
            Flush(sb, tempSb, currentStyleId);

            //build color table
            var list = new SortedList<int, Color>();
            foreach (var pair in _colorTable)
                list.Add(pair.Value, pair.Key);

            tempSb.Length = 0;
            tempSb.AppendFormat(@"{{\colortbl;");

            foreach (var pair in list)
                tempSb.Append(GetColorAsString(pair.Value) + ";");
            tempSb.AppendLine("}");

            //
            if (UseOriginalFont)
            {
                sb.Insert(0, string.Format(@"{{\fonttbl{{\f0\fmodern {0};}}}}{{\fs{1} ",
                    _tb.Font.Name, (int) (2*_tb.Font.SizeInPoints), _tb.CharHeight));
                sb.AppendLine(@"}");
            }

            sb.Insert(0, tempSb.ToString());

            sb.Insert(0, @"{\rtf1\ud\deff0");
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private RtfStyleDescriptor GetRtfDescriptor(StyleIndex styleIndex)
        {
            var styles = new List<Style>();
            //find text renderer
            TextStyle textStyle = null;
            var mask = 1;
            var hasTextStyle = false;
            for (var i = 0; i < _tb.Styles.Length; i++)
            {
                if (_tb.Styles[i] != null && ((int) styleIndex & mask) != 0)
                    if (_tb.Styles[i].IsExportable)
                    {
                        var style = _tb.Styles[i];
                        styles.Add(style);

                        var isTextStyle = style is TextStyle;
                        if (isTextStyle)
                            if (!hasTextStyle || _tb.AllowSeveralTextStyleDrawing)
                            {
                                hasTextStyle = true;
                                textStyle = style as TextStyle;
                            }
                    }
                mask = mask << 1;
            }
            //add TextStyle css
            RtfStyleDescriptor result = null;

            if (!hasTextStyle)
            {
                //draw by default renderer
                result = _tb.DefaultStyle.GetRtf();
            }
            else
            {
                result = textStyle.GetRtf();
            }

            return result;
        }

        public static string GetColorAsString(Color color)
        {
            if (color == Color.Transparent)
                return "";
            return string.Format(@"\red{0}\green{1}\blue{2}", color.R, color.G, color.B);
        }

        private void Flush(StringBuilder sb, StringBuilder tempSb, StyleIndex currentStyle)
        {
            //find textRenderer
            if (tempSb.Length == 0)
                return;

            var desc = GetRtfDescriptor(currentStyle);
            var cf = GetColorTableNumber(desc.ForeColor);
            var cb = GetColorTableNumber(desc.BackColor);
            var tags = new StringBuilder();
            if (cf >= 0)
                tags.AppendFormat(@"\cf{0}", cf);
            if (cb >= 0)
                tags.AppendFormat(@"\highlight{0}", cb);
            if (!string.IsNullOrEmpty(desc.AdditionalTags))
                tags.Append(desc.AdditionalTags.Trim());

            if (tags.Length > 0)
                sb.AppendFormat(@"{{{0} {1}}}", tags, tempSb);
            else
                sb.Append(tempSb);
            tempSb.Length = 0;
        }

        private int GetColorTableNumber(Color color)
        {
            if (color.A == 0)
                return -1;

            if (!_colorTable.ContainsKey(color))
                _colorTable[color] = _colorTable.Count + 1;

            return _colorTable[color];
        }
    }

    public class RtfStyleDescriptor
    {
        public Color ForeColor { get; set; }
        public Color BackColor { get; set; }
        public string AdditionalTags { get; set; }
    }
}