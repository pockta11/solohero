namespace SoloHero.Core.Analytics
{
    /// <summary>One event parameter. Firebase accepts long, double or string values.</summary>
    public readonly struct AnalyticsParam
    {
        public enum ValueKind
        {
            Long,
            Double,
            Text
        }

        public readonly string Key;
        public readonly ValueKind Kind;
        public readonly long LongValue;
        public readonly double DoubleValue;
        public readonly string TextValue;

        private AnalyticsParam(string key, ValueKind kind, long l, double d, string s)
        {
            Key = key;
            Kind = kind;
            LongValue = l;
            DoubleValue = d;
            TextValue = s;
        }

        public static AnalyticsParam Of(string key, long value) => new AnalyticsParam(key, ValueKind.Long, value, 0d, null);

        public static AnalyticsParam Of(string key, double value) => new AnalyticsParam(key, ValueKind.Double, 0L, value, null);

        public static AnalyticsParam Of(string key, bool value) => new AnalyticsParam(key, ValueKind.Long, value ? 1L : 0L, 0d, null);

        public static AnalyticsParam Of(string key, string value) => new AnalyticsParam(key, ValueKind.Text, 0L, 0d, value ?? "");

        public override string ToString()
        {
            switch (Kind)
            {
                case ValueKind.Long: return Key + "=" + LongValue;
                case ValueKind.Double: return Key + "=" + DoubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                default: return Key + "=" + TextValue;
            }
        }
    }
}
