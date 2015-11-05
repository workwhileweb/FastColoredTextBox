using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class OpenTypeFontSample : Form
    {
        public OpenTypeFontSample()
        {
            InitializeComponent();
        }


        private void cbFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFont.SelectedItem == null)
                return;

            var item = (Enumlogfontex) cbFont.SelectedItem;
            if (cbSize.SelectedItem != null)
            {
                item.elfLogFont.lfHeight = int.Parse(cbSize.SelectedItem.ToString());
                item.elfLogFont.lfWidth = item.elfLogFont.lfHeight/2;
            }
            fctb.DefaultStyle = new OpenTypeFontStyle(fctb, item.elfLogFont);
            fctb.Invalidate();
        }

        #region Build OpenType font list

        private readonly List<Enumlogfontex> _fontList = new List<Enumlogfontex>();

        protected override void OnLoad(EventArgs e)
        {
            //build list of OpenType fonts
            var lf = new Logfont();

            var plogFont = Marshal.AllocHGlobal(Marshal.SizeOf(lf));
            Marshal.StructureToPtr(lf, plogFont, true);

            try
            {
                _fontList.Clear();
                using (var g = CreateGraphics())
                {
                    var p = g.GetHdc();
                    EnumFontFamiliesEx(p, plogFont, Callback, IntPtr.Zero, 0);
                    g.ReleaseHdc(p);
                }
            }
            finally
            {
                Marshal.DestroyStructure(plogFont, typeof (Logfont));
            }

            //sort fonts
            _fontList.Sort((f1, f2) => f1.elfFullName.CompareTo(f2.elfFullName));
            //build combobox
            cbFont.Items.Clear();
            foreach (var item in _fontList)
                cbFont.Items.Add(item);
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern int EnumFontFamiliesEx(IntPtr hdc, [In] IntPtr pLogfont,
            EnumFontExDelegate lpEnumFontFamExProc, IntPtr lParam, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct Newtextmetric
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
            private readonly int ntmFlags;
            private readonly int ntmSizeEM;
            private readonly int ntmCellHeight;
            private readonly int ntmAvgWidth;
        }

        public struct Fontsignature
        {
            [MarshalAs(UnmanagedType.ByValArray)] private int[] _fsUsb;
            [MarshalAs(UnmanagedType.ByValArray)] private int[] _fsCsb;
        }

        public struct Newtextmetricex
        {
            private Newtextmetric _ntmTm;
            private Fontsignature _ntmFontSig;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct Enumlogfontex
        {
            public Logfont elfLogFont;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string elfFullName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string elfStyle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string elfScript;


            public override string ToString()
            {
                return elfFullName;
            }
        }

        private const int RasterFonttype = 1;
        private const int DeviceFonttype = 2;
        private const int TruetypeFonttype = 4;

        private delegate int EnumFontExDelegate(
            ref Enumlogfontex lpelfe, ref Newtextmetricex lpntme, int fontType, int lParam);

        private int _cnt;

        public int Callback(ref Enumlogfontex lpelfe, ref Newtextmetricex lpntme, int fontType, int lParam)
        {
            try
            {
                _cnt++;
                if (fontType != TruetypeFonttype)
                    _fontList.Add(lpelfe);
            }
            catch
            {
            }
            return _cnt;
        }

        #endregion
    }

    /// <summary>
    ///     Text renderer for OpenType fonts (uses GDI rendering)
    /// </summary>
    public class OpenTypeFontStyle : TextStyle
    {
        private readonly Logfont _font;

        public OpenTypeFontStyle(FastColoredTextBox fctb, Logfont font) : base(null, null, FontStyle.Regular)
        {
            _font = font;
            //measure font
            using (var gr = fctb.CreateGraphics())
            {
                var hdc = gr.GetHdc();

                var fontHandle = CreateFontIndirect(font);
                var f = SelectObject(hdc, fontHandle);

                var measureSize = new Size(0, 0);

                try
                {
                    GetTextExtentPoint(hdc, "M", 1, ref measureSize);
                }
                finally
                {
                    DeleteObject(SelectObject(hdc, f));
                    gr.ReleaseHdc(hdc);
                }

                fctb.CharWidth = measureSize.Width;
                fctb.CharHeight = measureSize.Height + fctb.LineInterval;
                fctb.NeedRecalc(true, true);
            }
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([In, MarshalAs(UnmanagedType.LPStruct)] Logfont lplf);

        [DllImport("gdi32.dll")]
        private static extern bool TextOut(IntPtr hdc, int nXStart, int nYStart, string lpString, int cbString);

        [DllImport("gdi32.dll")]
        private static extern bool GetTextExtentPoint(IntPtr hdc, string lpString, int cbString, ref Size lpSize);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr objectHandle);

        [DllImport("gdi32.dll")]
        public static extern int SetBkColor(IntPtr hDc, int crColor);

        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, int crColor);


        public override void Draw(Graphics gr, Point position, Range range)
        {
            //create font
            var hdc = gr.GetHdc();
            var fontHandle = CreateFontIndirect(_font);
            var f = SelectObject(hdc, fontHandle);
            //set foreground and background colors
            SetTextColor(hdc, ColorTranslator.ToWin32(range.Tb.ForeColor));
            SetBkColor(hdc, ColorTranslator.ToWin32(range.Tb.BackColor));


            //draw background
            if (BackgroundBrush != null)
                gr.FillRectangle(BackgroundBrush, position.X, position.Y,
                    (range.End.IChar - range.Start.IChar)*range.Tb.CharWidth, range.Tb.CharHeight);

            //coordinates
            var y = position.Y + range.Tb.LineInterval/2;
            var x = position.X;
            var dx = range.Tb.CharWidth;

            //draw chars
            try
            {
                var s = range.Text;
                foreach (var c in s)
                {
                    TextOut(hdc, x, y, c.ToString(), 1);
                    x += dx;
                }
            }
            finally
            {
                DeleteObject(SelectObject(hdc, f));
                gr.ReleaseHdc(hdc);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class Logfont
    {
        public const int LfFacesize = 32;
        public byte lfCharSet;
        public byte lfClipPrecision;
        public int lfEscapement;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LfFacesize)] public string lfFaceName;

        public int lfHeight;
        public byte lfItalic;
        public int lfOrientation;
        public byte lfOutPrecision;
        public byte lfPitchAndFamily;
        public byte lfQuality;
        public byte lfStrikeOut;
        public byte lfUnderline;
        public int lfWeight;
        public int lfWidth;
    }
}