using System.Collections.Generic;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class AutocompleteSample4 : Form
    {
        private static readonly string[] Sources =
        {
            "com",
            "com.company",
            "com.company.Class1",
            "com.company.Class1.Method1",
            "com.company.Class1.Method2",
            "com.company.Class2",
            "com.company.Class3",
            "com.example",
            "com.example.ClassX",
            "com.example.ClassX.Method1",
            "com.example.ClassY",
            "com.example.ClassY.Method1"
        };

        private readonly AutocompleteMenu _popupMenu;

        public AutocompleteSample4()
        {
            InitializeComponent();

            //create autocomplete popup menu
            _popupMenu = new AutocompleteMenu(fctb);
            _popupMenu.SearchPattern = @"[\w\.]";

            //
            var items = new List<AutocompleteItem>();
            foreach (var item in Sources)
                items.Add(new MethodAutocompleteItem2(item));

            _popupMenu.Items.SetAutocompleteItems(items);
        }
    }

    /// <summary>
    ///     This autocomplete item appears after dot
    /// </summary>
    public class MethodAutocompleteItem2 : MethodAutocompleteItem
    {
        private readonly string _firstPart;
        private readonly string _lastPart;

        public MethodAutocompleteItem2(string text)
            : base(text)
        {
            var i = text.LastIndexOf('.');
            if (i < 0)
                _firstPart = text;
            else
            {
                _firstPart = text.Substring(0, i);
                _lastPart = text.Substring(i + 1);
            }
        }

        public override CompareResult Compare(string fragmentText)
        {
            var i = fragmentText.LastIndexOf('.');

            if (i < 0)
            {
                if (_firstPart.StartsWith(fragmentText) && string.IsNullOrEmpty(_lastPart))
                    return CompareResult.VisibleAndSelected;
                //if (firstPart.ToLower().Contains(fragmentText.ToLower()))
                //  return CompareResult.Visible;
            }
            else
            {
                var fragmentFirstPart = fragmentText.Substring(0, i);
                var fragmentLastPart = fragmentText.Substring(i + 1);


                if (_firstPart != fragmentFirstPart)
                    return CompareResult.Hidden;

                if (_lastPart != null && _lastPart.StartsWith(fragmentLastPart))
                    return CompareResult.VisibleAndSelected;

                if (_lastPart != null && _lastPart.ToLower().Contains(fragmentLastPart.ToLower()))
                    return CompareResult.Visible;
            }

            return CompareResult.Hidden;
        }

        public override string GetTextForReplace()
        {
            if (_lastPart == null)
                return _firstPart;

            return _firstPart + "." + _lastPart;
        }

        public override string ToString()
        {
            if (_lastPart == null)
                return _firstPart;

            return _lastPart;
        }
    }
}