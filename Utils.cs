namespace MultipleGroupRegexReplace
{
    public static class Utils
    {
        public static double CalculatePercentage(double input, double max)
        {
            return input / max * 100;
        }

        public static long Map(long value, long fromLow, long fromHigh, long toLow, long toHigh)
        {
            return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }
    }
}