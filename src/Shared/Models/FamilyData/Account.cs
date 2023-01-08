public class Account 
{
    public string Identifier { get; set; }
    public string AccountType { get; set; }
    public string? Custodian { get; set; }

    private List<Investment>? _Investments;
    public List<Investment> Investments {
        get {
            if (_Investments == null)
            {
                _Investments = new List<Investment>();
            }

            return _Investments;
        }
    }
}