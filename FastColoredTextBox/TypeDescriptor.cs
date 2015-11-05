using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// These classes are required for correct data binding to Text property of FastColoredTextbox
    internal class FctbDescriptionProvider : TypeDescriptionProvider
    {
        public FctbDescriptionProvider(Type type)
            : base(GetDefaultTypeProvider(type))
        {
        }

        private static TypeDescriptionProvider GetDefaultTypeProvider(Type type)
        {
            return TypeDescriptor.GetProvider(type);
        }


        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            var defaultDescriptor = base.GetTypeDescriptor(objectType, instance);
            return new FctbTypeDescriptor(defaultDescriptor, instance);
        }
    }

    internal class FctbTypeDescriptor : CustomTypeDescriptor
    {
        private readonly object _instance;
        private ICustomTypeDescriptor _parent;

        public FctbTypeDescriptor(ICustomTypeDescriptor parent, object instance)
            : base(parent)
        {
            _parent = parent;
            _instance = instance;
        }

        public override string GetComponentName()
        {
            var ctrl = (_instance as Control);
            return ctrl == null ? null : ctrl.Name;
        }

        public override EventDescriptorCollection GetEvents()
        {
            var coll = base.GetEvents();
            var list = new EventDescriptor[coll.Count];

            for (var i = 0; i < coll.Count; i++)
                if (coll[i].Name == "TextChanged") //instead of TextChanged slip BindingTextChanged for binding
                    list[i] = new FooTextChangedDescriptor(coll[i]);
                else
                    list[i] = coll[i];

            return new EventDescriptorCollection(list);
        }
    }

    internal class FooTextChangedDescriptor : EventDescriptor
    {
        public FooTextChangedDescriptor(MemberDescriptor desc)
            : base(desc)
        {
        }

        public override Type ComponentType
        {
            get { return typeof (FastColoredTextBox); }
        }

        public override Type EventType
        {
            get { return typeof (EventHandler); }
        }

        public override bool IsMulticast
        {
            get { return true; }
        }

        public override void AddEventHandler(object component, Delegate value)
        {
            (component as FastColoredTextBox).BindingTextChanged += value as EventHandler;
        }

        public override void RemoveEventHandler(object component, Delegate value)
        {
            (component as FastColoredTextBox).BindingTextChanged -= value as EventHandler;
        }
    }
}