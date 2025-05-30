﻿namespace Horizon.Core;

public static class ExtensionMethods
{
    public static string ToFileSize(this long l)
    {
        try
        {
            return string.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
        catch (Exception e)
        {
            return "0 KB";
        }
    }
}

public class FileSizeFormatProvider : IFormatProvider, ICustomFormatter
{
    public object GetFormat(Type formatType)
    {
        if (formatType == typeof(ICustomFormatter)) return this;
        return null;
    }

    private const string fileSizeFormat = "fs";
    private const Decimal OneKiloByte = 1024M;
    private const Decimal OneMegaByte = OneKiloByte * 1024M;
    private const Decimal OneGigaByte = OneMegaByte * 1024M;

    public string Format(string format, object arg, IFormatProvider formatProvider)
    {
        try
        {
            if (format == null || !format.StartsWith(fileSizeFormat))
            {
                return defaultFormat(format, arg, formatProvider);
            }
        }
        catch (System.Exception e)
        {

        }
        if (arg is string)
        {
            return defaultFormat(format, arg, formatProvider);
        }

        Decimal size;

        try
        {
            size = Convert.ToDecimal(arg);
        }
        catch (System.Exception e)
        {
            return defaultFormat(format, arg, formatProvider);
        }

        string suffix;
        if (size > OneGigaByte)
        {
            size /= OneGigaByte;
            suffix = " GB";
        }
        else if (size > OneMegaByte)
        {
            size /= OneMegaByte;
            suffix = " MB";
        }
        else if (size > OneKiloByte)
        {
            size /= OneKiloByte;
            suffix = " KB";
        }
        else
        {
            suffix = " B";
        }

        string precision = format.Substring(2);
        if (String.IsNullOrEmpty(precision)) precision = "2";
        return String.Format("{0:N" + precision + "}{1}", size, suffix);

    }

    private static string defaultFormat(string format, object arg, IFormatProvider formatProvider)
    {
        IFormattable formattableArg = arg as IFormattable;
        if (formattableArg != null)
        {
            return formattableArg.ToString(format, formatProvider);
        }
        return arg.ToString();
    }

}