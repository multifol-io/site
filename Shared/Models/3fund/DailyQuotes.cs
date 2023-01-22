public class DailyQuotes
{
    public DailyQuotes(int index, string date, string close)
    {
        Index = index;
        Date = date;
        Close = close;
    }
    public int Index { get; set; }
    public string Date { get; set; } 
    public string Close { get; set; } 

    public static async Task<List<DailyQuotes>> GetDailyQuotesAsync(HttpClient httpClient, string url)
    {
        var csvContent = await httpClient.GetStringAsync(url);
        List<DailyQuotes> dailyQuotesList = new ();
        var skippedFirstLine = false;
        var index = 0;
        foreach(var line in csvContent.Split('\n'))
        {
            if (!skippedFirstLine) {
                skippedFirstLine = true;
                continue;
            }

            if (!string.IsNullOrEmpty(line))
            {
                var chunks = line.Split(',');
                DailyQuotes dailyQuote = new (index++,chunks[0],chunks[1]);
                dailyQuotesList.Add(dailyQuote);
            }
        }

        return dailyQuotesList;
    }
}
