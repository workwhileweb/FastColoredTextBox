﻿using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Exports colored text as HTML
    /// </summary>
    /// <remarks>At this time only TextStyle renderer is supported. Other styles is not exported.</remarks>
    public class ExportToHtml
    {
        private FastColoredTextBox _tb;

        public string LineNumbersCss =
            "<style type=\"text/css\"> .lineNumber{font-family : monospace; font-size : small; font-style : normal; font-weight : normal; color : Teal; background-color : ThreedFace;} </style>";

        public ExportToHtml()
        {
            UseNbsp = true;
            UseOriginalFont = true;
            UseStyleTag = true;
            UseBr = true;
        }

        /// <summary>
        ///     Use nbsp; instead space
        /// </summary>
        public bool UseNbsp { get; set; }

        /// <summary>
        ///     Use nbsp; instead space in beginning of line
        /// </summary>
        public bool UseForwardNbsp { get; set; }

        /// <summary>
        ///     Use original font
        /// </summary>
        public bool UseOriginalFont { get; set; }

        /// <summary>
        ///     Use style tag instead style attribute
        /// </summary>
        public bool UseStyleTag { get; set; }

        /// <summary>
        ///     Use 'br' tag instead of '\n'
        /// </summary>
        public bool UseBr { get; set; }

        /// <summary>
        ///     Includes line numbers
        /// </summary>
        public bool IncludeLineNumbers { get; set; }

        public string GetHtml(FastColoredTextBox tb)
        {
            _tb = tb;
            var sel = new Range(tb);
            sel.SelectAll();
            return GetHtml(sel);
        }

        public string GetHtml(Range r)
        {
            _tb = r.Tb;
            var styles = new Dictionary<StyleIndex, object>();
            var sb = new StringBuilder();
            var tempSb = new StringBuilder();
            var currentStyleId = StyleIndex.None;
            r.Normalize();
            var currentLine = r.Start.ILine;
            styles[currentStyleId] = null;
            //
            if (UseOriginalFont)
                sb.AppendFormat("<font style=\"font-family: {0}, monospace; font-size: {1}pt; line-height: {2}px;\">",
                    r.Tb.Font.Name, r.Tb.Font.SizeInPoints, r.Tb.CharHeight);

            //
            if (IncludeLineNumbers)
                tempSb.AppendFormat("<span class=lineNumber>{0}</span>  ", currentLine + 1);
            //
            var hasNonSpace = false;
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
                        tempSb.Append(UseBr ? "<br>" : "\r\n");
                        if (IncludeLineNumbers)
                            tempSb.AppendFormat("<span class=lineNumber>{0}</span>  ", i + 2);
                    }
                    currentLine = p.ILine;
                    hasNonSpace = false;
                }
                switch (c.C)
                {
                    case ' ':
                        if ((hasNonSpace || !UseForwardNbsp) && !UseNbsp)
                            goto default;

                        tempSb.Append("&nbsp;");
                        break;
                    case '<':
                        tempSb.Append("&lt;");
                        break;
                    case '>':
                        tempSb.Append("&gt;");
                        break;
                    case '&':
                        tempSb.Append("&amp;");
                        break;
                    default:
                        hasNonSpace = true;
                        tempSb.Append(c.C);
                        break;
                }
            }
            Flush(sb, tempSb, currentStyleId);

            if (UseOriginalFont)
                sb.Append("</font>");

            //build styles
            if (UseStyleTag)
            {
                tempSb.Length = 0;
                tempSb.Append("<style type=\"text/css\">");
                foreach (var styleId in styles.Keys)
                    tempSb.AppendFormat(".fctb{0}{{ {1} }}\r\n", GetStyleName(styleId), GetCss(styleId));
                tempSb.Append("</style>");

                sb.Insert(0, tempSb.ToString());
            }

            if (IncludeLineNumbers)
                sb.Insert(0, LineNumbersCss);

            return sb.ToString();
        }

        private string GetCss(StyleIndex styleIndex)
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
            var result = "";

            if (!hasTextStyle)
            {
                //draw by default renderer
                result = _tb.DefaultStyle.GetCss();
            }
            else
            {
                result = textStyle.GetCss();
            }
            //add no TextStyle css
            foreach (var style in styles)
//            if (style != textStyle)
                if (!(style is TextStyle))
                    result += style.GetCss();

            return result;
        }

        public static string GetColorAsString(Color color)
        {
            if (color == Color.Transparent)
                return "";
            return string.Format("#{0:x2}{1:x2}{2:x2}", color.R, color.G, color.B);
        }

        private string GetStyleName(StyleIndex styleIndex)
        {
            return styleIndex.ToString().Replace(" ", "").Replace(",", "");
        }

        private void Flush(StringBuilder sb, StringBuilder tempSb, StyleIndex currentStyle)
        {
            //find textRenderer
            if (tempSb.Length == 0)
                return;
            if (UseStyleTag)
                sb.AppendFormat("<font class=fctb{0}>{1}</font>", GetStyleName(currentStyle), tempSb);
            else
            {
                var css = GetCss(currentStyle);
                if (css != "")
                    sb.AppendFormat("<font style=\"{0}\">", css);
                sb.Append(tempSb);
                if (css != "")
                    sb.Append("</font>");
            }
            tempSb.Length = 0;
        }
    }
}