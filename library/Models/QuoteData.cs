// List<QuoteData> quoteData = JsonSerializer.Deserialize<List<QuoteData>>(myJsonResponse);
using System.Text.Json.Serialization;

public class QuoteData
    {
        [JsonConstructor]
        public QuoteData(
            string code,
            int? timestamp,
            int? gmtoffset,
            double? open,
            double? high,
            double? low,
            double? close,
            int? volume,
            double? previousClose,
            double? change,
            double? changeP
        )
        {
            this.Code = code;
            this.Timestamp = timestamp;
            this.Gmtoffset = gmtoffset;
            this.Open = open;
            this.High = high;
            this.Low = low;
            this.Close = close;
            this.Volume = volume;
            this.PreviousClose = previousClose;
            this.Change = change;
            this.ChangeP = changeP;
        }

        [JsonPropertyName("code")]
        public string Code { get; }

        [JsonPropertyName("timestamp")]
        public int? Timestamp { get; }

        [JsonPropertyName("gmtoffset")]
        public int? Gmtoffset { get; }

        [JsonPropertyName("open")]
        public double? Open { get; }

        [JsonPropertyName("high")]
        public double? High { get; }

        [JsonPropertyName("low")]
        public double? Low { get; }

        [JsonPropertyName("close")]
        public double? Close { get; }

        [JsonPropertyName("volume")]
        public int? Volume { get; }

        [JsonPropertyName("previousClose")]
        public double? PreviousClose { get; }

        [JsonPropertyName("change")]
        public double? Change { get; }

        [JsonPropertyName("change_p")]
        public double? ChangeP { get; }
    }

