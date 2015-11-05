using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class AutocompleteSample3 : Form
    {
        private readonly AutocompleteMenu _popupMenu;

        public AutocompleteSample3()
        {
            InitializeComponent();

            //create autocomplete popup menu
            _popupMenu = new AutocompleteMenu(fctb);
            _popupMenu.ForeColor = Color.White;
            _popupMenu.BackColor = Color.Gray;
            _popupMenu.SelectedColor = Color.Purple;
            _popupMenu.SearchPattern = @"[\w\.]";
            _popupMenu.AllowTabKey = true;
            //assign DynamicCollection as items source
            _popupMenu.Items.SetAutocompleteItems(new DynamicCollection(_popupMenu, fctb));
        }
    }

    /// <summary>
    ///     Builds list of methods and properties for current class name was typed in the textbox
    /// </summary>
    internal class DynamicCollection : IEnumerable<AutocompleteItem>
    {
        private readonly AutocompleteMenu _menu;
        private FastColoredTextBox _tb;

        public DynamicCollection(AutocompleteMenu menu, FastColoredTextBox tb)
        {
            _menu = menu;
            _tb = tb;
        }

        public IEnumerator<AutocompleteItem> GetEnumerator()
        {
            //get current fragment of the text
            var text = _menu.Fragment.Text;

            //extract class name (part before dot)
            var parts = text.Split('.');
            if (parts.Length < 2)
                yield break;
            var className = parts[parts.Length - 2];

            //find type for given className
            var type = FindTypeByName(className);

            if (type == null)
                yield break;

            //return static methods of the class
            foreach (var methodName in type.GetMethods().AsEnumerable().Select(mi => mi.Name).Distinct())
                yield return new MethodAutocompleteItem(methodName + "()")
                {
                    ToolTipTitle = methodName,
                    ToolTipText = "Description of method " + methodName + " goes here."
                };

            //return static properties of the class
            foreach (var pi in type.GetProperties())
                yield return new MethodAutocompleteItem(pi.Name)
                {
                    ToolTipTitle = pi.Name,
                    ToolTipText = "Description of property " + pi.Name + " goes here."
                };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private Type FindTypeByName(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type type = null;
            foreach (var a in assemblies)
            {
                foreach (var t in a.GetTypes())
                    if (t.Name == name)
                    {
                        return t;
                    }
            }

            return null;
        }
    }
}