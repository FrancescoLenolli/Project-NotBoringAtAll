﻿static class Formatter
{
    public static string FormatValue(double value)
    {
        string formattedValue = "";

        if (value > 10000)
        {
            formattedValue = $"{string.Format("{0:#.##e+0}", value)}m";
        }
        else
        {
            formattedValue = value.ToString();
        }

        return formattedValue;
    }
}
