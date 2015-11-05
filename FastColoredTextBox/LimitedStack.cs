using System;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Limited stack
    /// </summary>
    public class LimitedStack<T>
    {
        private T[] _items;
        private int _start;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="maxItemCount">Maximum length of stack</param>
        public LimitedStack(int maxItemCount)
        {
            _items = new T[maxItemCount];
            Count = 0;
            _start = 0;
        }

        /// <summary>
        ///     Max stack length
        /// </summary>
        public int MaxItemCount
        {
            get { return _items.Length; }
        }

        /// <summary>
        ///     Current length of stack
        /// </summary>
        public int Count { get; private set; }

        private int LastIndex
        {
            get { return (_start + Count - 1)%_items.Length; }
        }

        /// <summary>
        ///     Pop item
        /// </summary>
        public T Pop()
        {
            if (Count == 0)
                throw new Exception("Stack is empty");

            var i = LastIndex;
            var item = _items[i];
            _items[i] = default(T);

            Count--;

            return item;
        }

        /// <summary>
        ///     Peek item
        /// </summary>
        public T Peek()
        {
            if (Count == 0)
                return default(T);

            return _items[LastIndex];
        }

        /// <summary>
        ///     Push item
        /// </summary>
        public void Push(T item)
        {
            if (Count == _items.Length)
                _start = (_start + 1)%_items.Length;
            else
                Count++;

            _items[LastIndex] = item;
        }

        /// <summary>
        ///     Clear stack
        /// </summary>
        public void Clear()
        {
            _items = new T[_items.Length];
            Count = 0;
            _start = 0;
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            for (var i = 0; i < Count; i++)
                result[i] = _items[(_start + i)%_items.Length];
            return result;
        }
    }
}