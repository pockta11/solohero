using System;
using System.Globalization;

namespace SoloHero.Core.Security
{
    /// <summary>
    /// D-184: a double held masked in memory, so a memory editor searching for the number on screen (gold, gems) finds
    /// nothing it can change. Every assignment draws a new mask, so an "unknown value, changed / unchanged" search does
    /// not converge either. A plain decoy copy sits next to it as a honeypot: if something rewrites the decoy the real
    /// value is kept and <see cref="TamperGuard"/> is told. Behaves like a double everywhere (implicit conversions both
    /// ways); saves write it as a plain number (the serializer's converter), so the save format does not change.
    /// default(SecureDouble) is 0.
    /// </summary>
    public struct SecureDouble : IEquatable<SecureDouble>, IEquatable<double>, IEquatable<int>, IEquatable<long>, IComparable<SecureDouble>, IComparable, IFormattable
    {
        private long _mask;
        private long _hidden;
        private double _decoy;

        public SecureDouble(double value)
        {
            _mask = TamperGuard.NextKey();
            _hidden = BitConverter.DoubleToInt64Bits(value) ^ _mask;
            _decoy = value;
        }

        public double Value
        {
            get
            {
                long bits = _hidden ^ _mask;
                if (bits != BitConverter.DoubleToInt64Bits(_decoy)) TamperGuard.Report("memory");
                return BitConverter.Int64BitsToDouble(bits);
            }
        }

        public static implicit operator double(SecureDouble v) => v.Value;

        public static implicit operator SecureDouble(double v) => new SecureDouble(v);

        public static SecureDouble operator ++(SecureDouble v) => new SecureDouble(v.Value + 1d);

        public static SecureDouble operator --(SecureDouble v) => new SecureDouble(v.Value - 1d);

        public bool Equals(SecureDouble other) => Value.Equals(other.Value);

        // NUnit compares through IEquatable<T> of the expected value's type (Assert.AreEqual(5, save.gem)).
        public bool Equals(double other) => Value.Equals(other);

        public bool Equals(int other) => Value.Equals((double)other);

        public bool Equals(long other) => Value.Equals((double)other);

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case SecureDouble s: return Equals(s);
                case double d: return Value.Equals(d);
                case int i: return Equals(i);
                case long l: return Equals(l);
                default: return false;
            }
        }

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(SecureDouble other) => Value.CompareTo(other.Value);

        public int CompareTo(object obj) => obj is SecureDouble s ? CompareTo(s) : Value.CompareTo(obj);

        public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);

        public string ToString(string format) => Value.ToString(format, CultureInfo.CurrentCulture);

        public string ToString(IFormatProvider provider) => Value.ToString(provider);

        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
    }

    /// <summary>D-184: an int held masked in memory (tickets, stones, pity counters). See <see cref="SecureDouble"/>.</summary>
    public struct SecureInt : IEquatable<SecureInt>, IEquatable<int>, IEquatable<long>, IEquatable<double>, IComparable<SecureInt>, IComparable, IFormattable
    {
        private int _mask;
        private int _hidden;
        private int _decoy;

        public SecureInt(int value)
        {
            _mask = unchecked((int)TamperGuard.NextKey()) | 1;
            _hidden = value ^ _mask;
            _decoy = value;
        }

        public int Value
        {
            get
            {
                int value = _hidden ^ _mask;
                if (value != _decoy) TamperGuard.Report("memory");
                return value;
            }
        }

        public static implicit operator int(SecureInt v) => v.Value;

        public static implicit operator SecureInt(int v) => new SecureInt(v);

        public static SecureInt operator ++(SecureInt v) => new SecureInt(unchecked(v.Value + 1));

        public static SecureInt operator --(SecureInt v) => new SecureInt(unchecked(v.Value - 1));

        public bool Equals(SecureInt other) => Value == other.Value;

        public bool Equals(int other) => Value == other;

        public bool Equals(long other) => Value == other;

        public bool Equals(double other) => Value == other;

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case SecureInt s: return Equals(s);
                case int i: return Value == i;
                case long l: return Value == l;
                case double d: return Value == d;
                default: return false;
            }
        }

        public override int GetHashCode() => Value;

        public int CompareTo(SecureInt other) => Value.CompareTo(other.Value);

        public int CompareTo(object obj) => obj is SecureInt s ? CompareTo(s) : Value.CompareTo(obj);

        public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);

        public string ToString(string format) => Value.ToString(format, CultureInfo.CurrentCulture);

        public string ToString(IFormatProvider provider) => Value.ToString(provider);

        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
    }

    /// <summary>D-184: a long held masked in memory (the VIP end time). See <see cref="SecureDouble"/>.</summary>
    public struct SecureLong : IEquatable<SecureLong>, IEquatable<long>, IEquatable<int>, IEquatable<double>, IComparable<SecureLong>, IComparable, IFormattable
    {
        private long _mask;
        private long _hidden;
        private long _decoy;

        public SecureLong(long value)
        {
            _mask = TamperGuard.NextKey();
            _hidden = value ^ _mask;
            _decoy = value;
        }

        public long Value
        {
            get
            {
                long value = _hidden ^ _mask;
                if (value != _decoy) TamperGuard.Report("memory");
                return value;
            }
        }

        public static implicit operator long(SecureLong v) => v.Value;

        public static implicit operator SecureLong(long v) => new SecureLong(v);

        public static SecureLong operator ++(SecureLong v) => new SecureLong(unchecked(v.Value + 1L));

        public static SecureLong operator --(SecureLong v) => new SecureLong(unchecked(v.Value - 1L));

        public bool Equals(SecureLong other) => Value == other.Value;

        public bool Equals(long other) => Value == other;

        public bool Equals(int other) => Value == other;

        public bool Equals(double other) => Value == other;

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case SecureLong s: return Equals(s);
                case long l: return Value == l;
                case int i: return Value == i;
                case double d: return Value == d;
                default: return false;
            }
        }

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(SecureLong other) => Value.CompareTo(other.Value);

        public int CompareTo(object obj) => obj is SecureLong s ? CompareTo(s) : Value.CompareTo(obj);

        public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);

        public string ToString(string format) => Value.ToString(format, CultureInfo.CurrentCulture);

        public string ToString(IFormatProvider provider) => Value.ToString(provider);

        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
    }
}
