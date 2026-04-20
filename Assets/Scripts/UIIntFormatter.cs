using System.Globalization;

public static class UIIntFormatter
{
    static readonly NumberFormatInfo CommaGroupedNumberFormat = CreateNumberFormat();

    static NumberFormatInfo CreateNumberFormat()
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.NumberGroupSeparator = ",";
        format.NumberDecimalDigits = 0;
        format.NegativeSign = "-";
        return format;
    }

    public static string Format(int value)
    {
        return value.ToString("N0", CommaGroupedNumberFormat);
    }

    public static string Format(long value)
    {
        return value.ToString("N0", CommaGroupedNumberFormat);
    }
}
