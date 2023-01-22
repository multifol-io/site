// created using https://json2csharp.com

namespace IRS
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class TaxRateData : DataDocument
    {
        public TaxData? TaxData { get; set; }
    }

    public class TaxBracket
    {
        public string? Rate { get; set; }
        public int StartAmount { get; set; }
    }

    public class TaxData
    {
        public int AnnualExclusionForGifts { get; set; }
        public List<TaxFiler>? TaxFilers { get; set; }
    }

    public class TaxFiler
    {
        public string? TaxFilingStatus { get; set; }
        public int StandardDeduction { get; set; }
        public List<TaxBracket>? TaxBrackets { get; set; }
    }



}