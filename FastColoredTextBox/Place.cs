using System;

namespace FastColoredTextBoxNS
{
    /// <summary>
    ///     Line index and char index
    /// </summary>
    public struct Place : IEquatable<Place>
    {
        public int IChar;
        public int ILine;

        public Place(int @char, int line)
        {
            IChar = @char;
            ILine = line;
        }

        public void Offset(int dx, int dy)
        {
            IChar += dx;
            ILine += dy;
        }

        public bool Equals(Place other)
        {
            return IChar == other.IChar && ILine == other.ILine;
        }

        public override bool Equals(object obj)
        {
            return (obj is Place) && Equals((Place) obj);
        }

        public override int GetHashCode()
        {
            return IChar.GetHashCode() ^ ILine.GetHashCode();
        }

        public static bool operator !=(Place p1, Place p2)
        {
            return !p1.Equals(p2);
        }

        public static bool operator ==(Place p1, Place p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator <(Place p1, Place p2)
        {
            if (p1.ILine < p2.ILine) return true;
            if (p1.ILine > p2.ILine) return false;
            if (p1.IChar < p2.IChar) return true;
            return false;
        }

        public static bool operator <=(Place p1, Place p2)
        {
            if (p1.Equals(p2)) return true;
            if (p1.ILine < p2.ILine) return true;
            if (p1.ILine > p2.ILine) return false;
            if (p1.IChar < p2.IChar) return true;
            return false;
        }

        public static bool operator >(Place p1, Place p2)
        {
            if (p1.ILine > p2.ILine) return true;
            if (p1.ILine < p2.ILine) return false;
            if (p1.IChar > p2.IChar) return true;
            return false;
        }

        public static bool operator >=(Place p1, Place p2)
        {
            if (p1.Equals(p2)) return true;
            if (p1.ILine > p2.ILine) return true;
            if (p1.ILine < p2.ILine) return false;
            if (p1.IChar > p2.IChar) return true;
            return false;
        }

        public static Place operator +(Place p1, Place p2)
        {
            return new Place(p1.IChar + p2.IChar, p1.ILine + p2.ILine);
        }

        public static Place Empty
        {
            get { return new Place(); }
        }

        public override string ToString()
        {
            return "(" + IChar + "," + ILine + ")";
        }
    }
}