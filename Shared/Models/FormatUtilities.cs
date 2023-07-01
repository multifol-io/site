public static class FormatUtilities {



    public static string formatMoney(int? amount) 
    {
        return String.Format("${0:#,0.##}", amount);
    }

    public static string formatMoney(double? amount) {
        return formatMoney(amount, false);
    }

    public static string formatMoney(double? amount, bool withColor) 
    {
        if (amount.HasValue)
        {
            string? prefix = null;
            string? suffix = null;
            if (withColor)
            {
                prefix = "<span style=color:" + (amount < 0.0 ? "red" : "green") + ">";
                suffix = "</span>";
            }

            return prefix + String.Format("${0:#,0.00}", amount) + suffix;
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

    public static string formatDoubleTwoDecimal(double? amount) 
    {
        return String.Format("{0:#,0.00}", amount);
    }
    public static string formatDoubleFourDecimal(double? amount) 
    {
        return String.Format("{0:#,0.0000}", amount);
    }
}