using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    public class SyntaxDescriptor : IDisposable
    {
        public readonly List<FoldingDesc> Foldings = new List<FoldingDesc>();
        public readonly List<RuleDesc> Rules = new List<RuleDesc>();
        public readonly List<Style> Styles = new List<Style>();
        public BracketsHighlightStrategy BracketsHighlightStrategy = BracketsHighlightStrategy.Strategy2;
        public char LeftBracket = '(';
        public char LeftBracket2 = '{';
        public char RightBracket = ')';
        public char RightBracket2 = '}';

        public void Dispose()
        {
            foreach (var style in Styles)
                style.Dispose();
        }
    }

    public class RuleDesc
    {
        private Regex _regex;
        public RegexOptions Options = RegexOptions.None;
        public string Pattern;
        public Style Style;

        public Regex Regex
        {
            get
            {
                if (_regex == null)
                {
                    _regex = new Regex(Pattern, SyntaxHighlighter.RegexCompiledOption | Options);
                }
                return _regex;
            }
        }
    }

    public class FoldingDesc
    {
        public string FinishMarkerRegex;
        public RegexOptions Options = RegexOptions.None;
        public string StartMarkerRegex;
    }
}