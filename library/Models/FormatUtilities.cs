using System.Globalization;

namespace Models;

public static class FormatUtilities
{
    public static string FormatMoney(int? amount)
    {
        return String.Format("${0:#,0.##}", amount);
    }

    public static string FormatMonth(DateOnly? date)
    {
        return date?.ToString("MM/yyyy") ?? "";
    }

    public static string FormatMonthPlus2DigitYear(DateOnly? date)
    {
        return date?.ToString("MM/yy") ?? "";
    }

    public static string FormatMoney(double? amount, bool withColor = false, int decimalPlaces = 2)
    {
        if (decimalPlaces != 2 && decimalPlaces != 0)
        {
            throw new ArgumentException("only 0 or 2 decimal places is allowed", nameof(decimalPlaces));
        }

        if (amount.HasValue)
        {
            string? prefix = null;
            string? suffix = null;
            if (withColor)
            {
                prefix = "<span style=color:" + (amount < 0.0 ? "red" : "green") + ">";
                suffix = "</span>";
            }

            if (decimalPlaces == 2)
            {
                return prefix + String.Format("${0:#,0.00}", amount) + suffix;
            }
            else if (decimalPlaces == 0)
            {
                return prefix + String.Format("${0:#,0}", amount) + suffix;
            }
            else { return "decimalPlaces value not allowed"; }
        }
        else
        {
            return "";
        }
    }

    public static string FormatMoneyThousands(double? amount)
    {
        if (amount == null) return "";

        if (amount >= 1000000 || amount <= -1000000)
        {
            return String.Format("${0:#,0.##M}", Math.Round((double)amount / 10000.0) / 100.0);
        }
        else if (amount >= 1000 || amount <= -1000)
        {
            return String.Format("${0:#,0.##K}", Math.Round((double)amount / 1000.0));
        }
        else
        {
            return String.Format("${0:#,0.##}", amount);
        }
    }

    public static string FormatMoneyNarrow(double? amount)
    {
        if (amount == null) return "";

        if (amount >= 100000000)
        {
            return String.Format("${0:#,0.##M}", Math.Round((double)amount / 10000.0) / 100.0);
        }
        else if (amount >= 1000000 || amount <= -1000000)
        {
            return String.Format("${0:#,0.##K}", Math.Round((double)amount / 1000.0));
        }
        else if (amount >= 1000 || amount <= -1000)
        {
            return String.Format("${0:#,0}", amount);
        }
        else
        {
            return String.Format("${0:#,0.##}", amount);
        }
    }

    public static string FormatPercent(double? amount)
    {
        return String.Format("{0:#,0.#}%", amount);
    }

    public static string FormatPercent3(double? amount)
    {
        return String.Format("{0:#,0.###}%", amount);
    }
    public static string FormatPercent3WithColor(double? amount)
    {
        return amount.HasValue ? "<span style=color:" + (amount < 0 ? "red>" : "green>") + String.Format("{0:#,0.###}%", amount) + "</span>" : "";
    }

    public static string FormatDouble(double? amount)
    {
        return String.Format("{0:#,0}", amount);
    }
    public static string FormatDoubleTwoDecimal(double? amount)
    {
        return String.Format("{0:#,0.00}", amount);
    }
    public static string FormatDoubleFourDecimal(double? amount)
    {
        return String.Format("{0:#,0.0000}", amount);
    }

    public static double? ParseDoubleOrNull(string? value, bool allowCurrency = false)
    {
        return (value == null ? null : ParseDouble(value, allowCurrency));
    }

    public static double ParseDouble(string value, bool allowCurrency = false)
    {
        var numberStyles = (allowCurrency ? currency : numbers);
        double.TryParse(FormatUtilities.TrimQuotes(value), numberStyles,
                        CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                        out double doubleValue);
        return doubleValue;
    }

    public static string TrimQuotes(string input)
    {
        if (string.IsNullOrEmpty(input)) { return input; }
        int start = 0;
        int end = input.Length;

        if (end > 1 && input[end - 1] == '"')
        {
            end--;
        }

        if (input[0] == '"')
        {
            start++;
            end--;
        }
        return input.Substring(start, end);
    }

    public static string Bold(string text, bool showMarkup)
    {
        if (showMarkup)
        {
            return "[b]<b>" + text + "</b>[/b]";
        }
        else
        {
            return "<b>" + text + "</b>";
        }
    }

    public static string BoldUnderline(string text, bool showMarkup)
    {
        if (showMarkup)
        {
            return "[b][u]<b><u>" + text + "</u></b>[/u][/b]";
        }
        else
        {
            return "<b><u>" + text + "</u></b>";
        }
    }


    public enum Mode
    {
        normal = 0,
        href,
        text,
    }

    public static string Markupize(string? wikiText)
    {
        if (wikiText == null) return "";
        string returnVal = "<span>";
        char lastChar = 'x';
        var mode = Mode.normal;
        string href = "";
        string text = "";
        foreach (var c in wikiText)
        {
            switch (c)
            {
                case '[':
                    if (lastChar == '[')
                    {
                        mode = Mode.href;
                    }
                    break;
                case ']':
                    if (lastChar == ']')
                    {
                        if (text == "") { text = href; }
                        if (href.StartsWith("https://bogle.tools/"))
                        {
                            returnVal += "<a href='" + href[19..] + "'>" + text + "</a>";
                        }
                        else if (href.StartsWith("https://multifol.io/"))
                        {
                            returnVal += "<a href='" + href[19..] + "'>" + text + "</a>";
                        }
                        else if (href.StartsWith("https://"))
                        {
                            returnVal += "<a target=_blank href='" + href + "'>" + text + "</a>";
                        }
                        else
                        {
                            returnVal += "<a target=_blank href='https://www.bogleheads.org/wiki/" + href + "'>" + text + "</a>";
                        }
                        href = "";
                        text = "";
                        mode = Mode.normal;
                    }
                    break;
                case '|':
                    if (mode == Mode.href)
                    {
                        mode = Mode.text;
                    }
                    break;
            }

            switch (mode)
            {
                case Mode.href:
                    switch (c)
                    {
                        case '[':
                        case ']':
                            break;
                        default:
                            href += c;
                            break;
                    }
                    break;
                case Mode.text:
                    switch (c)
                    {
                        case '[':
                        case ']':
                        case '|':
                            break;
                        default:
                            text += c;
                            break;
                    }
                    break;
                default:
                    switch (c)
                    {
                        case '[':
                        case ']':
                            break;
                        case '\n':
                            returnVal += "<br/>";
                            break;
                        default:
                            returnVal += c;
                            break;
                    }
                    break;
            }

            lastChar = c;
        }
        returnVal += "</span>";

        return returnVal;
    }

    private static readonly NumberStyles numbers = NumberStyles.Float | NumberStyles.AllowThousands;
    private static readonly NumberStyles currency = NumberStyles.AllowCurrencySymbol | numbers;
}