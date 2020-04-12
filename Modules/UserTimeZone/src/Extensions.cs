namespace UserTimeZone
{
    using System;
    using System.Text;

    public static class Extensions
    {
        public static string ToShortString(this TimeZoneInfo timeZone)
        {
            var sb = new StringBuilder();
            if (timeZone.BaseUtcOffset.Hours >= 0)
            {
                sb.Append("+");
            }
            else
            {
                sb.Append("-");
            }

            sb.Append($"{Math.Abs(timeZone.BaseUtcOffset.Hours).ToString().PadLeft(2, '0')}:{Math.Abs(timeZone.BaseUtcOffset.Minutes).ToString().PadLeft(2, '0')}");

            return sb.ToString();
        }
    }
}
