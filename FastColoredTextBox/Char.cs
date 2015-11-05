namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Char and style
    /// </summary>
    public struct Char
    {
        /// <summary>
        ///     Unicode character
        /// </summary>
        public char C;

        /// <summary>
        ///     Style bit mask
        /// </summary>
        /// <remarks>Bit 1 in position n means that this char will rendering by FastColoredTextBox.Styles[n]</remarks>
        public StyleIndex Style;

        public Char(char c)
        {
            C = c;
            Style = StyleIndex.None;
        }
    }
}