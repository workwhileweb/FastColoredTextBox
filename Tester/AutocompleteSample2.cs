using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class AutocompleteSample2 : Form
    {
        private readonly string[] _declarationSnippets =
        {
            "public class ^\n{\n}", "private class ^\n{\n}", "internal class ^\n{\n}",
            "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
            "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}",
            "protected void ^()\n{\n;\n}",
            "public ^{ get; set; }", "private ^{ get; set; }", "internal ^{ get; set; }", "protected ^{ get; set; }"
        };

        private readonly string[] _keywords =
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return",
            "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while", "add", "alias", "ascending", "descending", "dynamic", "from", "get", "global", "group",
            "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield"
        };

        private readonly string[] _methods = {"Equals()", "GetHashCode()", "GetType()", "ToString()"};
        private readonly AutocompleteMenu _popupMenu;

        private readonly string[] _snippets =
        {
            "if(^)\n{\n;\n}", "if(^)\n{\n;\n}\nelse\n{\n;\n}", "for(^;;)\n{\n;\n}",
            "while(^)\n{\n;\n}", "do${\n^;\n}while();", "switch(^)\n{\ncase : break;\n}"
        };

        public AutocompleteSample2()
        {
            InitializeComponent();

            //create autocomplete popup menu
            _popupMenu = new AutocompleteMenu(fctb);
            _popupMenu.Items.ImageList = imageList1;
            _popupMenu.SearchPattern = @"[\w\.:=!<>]";
            _popupMenu.AllowTabKey = true;
            //
            BuildAutocompleteMenu();
        }

        private void BuildAutocompleteMenu()
        {
            var items = new List<AutocompleteItem>();

            foreach (var item in _snippets)
                items.Add(new SnippetAutocompleteItem(item) {ImageIndex = 1});
            foreach (var item in _declarationSnippets)
                items.Add(new DeclarationSnippet(item) {ImageIndex = 0});
            foreach (var item in _methods)
                items.Add(new MethodAutocompleteItem(item) {ImageIndex = 2});
            foreach (var item in _keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            //set as autocomplete source
            _popupMenu.Items.SetAutocompleteItems(items);
        }

        /// <summary>
        ///     This item appears when any part of snippet text is typed
        /// </summary>
        private class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        ///     Divides numbers and words: "123AND456" -> "123 AND 456"
        ///     Or "i=2" -> "i = 2"
        /// </summary>
        private class InsertSpaceSnippet : AutocompleteItem
        {
            private readonly string _pattern;

            public InsertSpaceSnippet(string pattern) : base("")
            {
                _pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override string ToolTipTitle
            {
                get { return Text; }
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, _pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, _pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }
        }

        /// <summary>
        ///     Inerts line break after '}'
        /// </summary>
        private class InsertEnterSnippet : AutocompleteItem
        {
            private Place _enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override string ToolTipTitle
            {
                get { return "Insert line break after '}'"; }
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.IChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        _enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                var r = Parent.Fragment;
                var end = r.End;
                r.Start = _enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.Tb.AutoIndent)
                    Parent.Fragment.Tb.DoAutoIndent();
            }
        }
    }
}