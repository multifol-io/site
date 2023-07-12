using System.Globalization;

public static class FormatUtilities {
    public static string formatMoney(int? amount) 
    {
        return String.Format("${0:#,0.##}", amount);
    }

    public static string formatMoney(double? amount, bool withColor = false, int decimalPlaces = 2) 
    {
        if (decimalPlaces != 2 && decimalPlaces != 0) {
            throw new ArgumentException("only 0 or 2 decimal places is allowed","decimalPlaces");
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
            
            if (decimalPlaces == 2) {
                return prefix + String.Format("${0:#,0.00}", amount) + suffix;
            } else if (decimalPlaces == 0) {
                return prefix + String.Format("${0:#,0}", amount) + suffix;
            } else { return "decimalPlaces value not allowed"; }
        }
        else
        {
            return "";
        }
    }

    public static string formatMoneyThousands(double? amount) 
    {
        if (amount == null) return "";

        if (amount >= 1000000 || amount <= -1000000) {
            return String.Format("${0:#,0.##M}", Math.Round((double)amount / 10000.0)/100.0);
        } else if (amount >= 1000 || amount <= -1000) {
            return String.Format("${0:#,0.##K}", Math.Round((double)amount / 1000.0));
        } else {
            return String.Format("${0:#,0.##}", amount);
        }
    }

    public static string formatMoneyNarrow(double? amount) 
    {
        if (amount == null) return "";

        if (amount >= 100000000) {
            return String.Format("${0:#,0.##M}", Math.Round((double)amount / 10000.0)/100.0);
        } else if (amount >= 1000000 || amount <= -1000000) {
            return String.Format("${0:#,0.##K}", Math.Round((double)amount / 1000.0));
        } else if (amount >= 1000 || amount <= -1000) {
            return String.Format("${0:#,0}", amount);
        } else {
            return String.Format("${0:#,0.##}", amount);
        }
    }

    public static string formatPercent(double? amount)
    {
        return String.Format("{0:#,0.#}%", amount);
    }

    public static string formatPercent3(double? amount)
    {
        return String.Format("{0:#,0.###}%", amount);
    }
    public static string formatPercent3WithColor(double? amount)
    {
        return amount.HasValue ? "<span style=color:"+ (amount < 0 ? "red>" : "green>") + String.Format("{0:#,#.###}%", amount)+"</span>" : "";
    }

    public static string formatDouble(double? amount) 
    {
        return String.Format("{0:#,0}", amount);
    }
    public static string formatDoubleTwoDecimal(double? amount) 
    {
        return String.Format("{0:#,0.00}", amount);
    }
    public static string formatDoubleFourDecimal(double? amount) 
    {
        return String.Format("{0:#,0.0000}", amount);
    }

    public static double? ParseDoubleOrNull(string? value, bool allowCurrency = false) {
        return (value == null ? null : ParseDouble(value, allowCurrency));
    }
    
    public static double ParseDouble(string value, bool allowCurrency = false) {
        double doubleValue;
        var numberStyles = (allowCurrency ? currency : numbers);
        double.TryParse(FormatUtilities.TrimQuotes(value), numberStyles,
                        CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"),
                        out doubleValue);
        return doubleValue;
    }

    public static string TrimQuotes(string input) {
        if (string.IsNullOrEmpty(input)) { return input; }
        int start = 0;
        int end = input.Length;

        if (end > 1 && input[end-1] == '"') {
            end--;
        }

        if (input[0] == '"') {
            start++;
            end--;
        }
        return input.Substring(start, end);
    }
    
    private static NumberStyles numbers = NumberStyles.Float | NumberStyles.AllowThousands;
    private static NumberStyles currency = NumberStyles.AllowCurrencySymbol | numbers;
}