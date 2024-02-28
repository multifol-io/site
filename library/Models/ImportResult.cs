namespace Models;

public class ImportResult {

    public List<Account> ImportedAccounts { get; set; } = [];
    private bool _ImportUpdatedAccounts;
    public bool ImportUpdatedAccounts {
        get { return _ImportUpdatedAccounts; }
        set {
            _ImportUpdatedAccounts = value;
            foreach (var account in UpdatedAccounts)
            {
                account.Import = _ImportUpdatedAccounts;
            }
        }
    }
    public List<Account> UpdatedAccounts { get; set; } = [];
    private bool _ImportNewAccounts;
    public bool ImportNewAccounts {
        get { return _ImportNewAccounts; }
        set {
            _ImportNewAccounts = value;
            foreach (var account in NewAccounts)
            {
                account.Import = _ImportNewAccounts;
            }
        }
    }
    public List<Account> NewAccounts { get; set; } = [];
    public List<ImportError> Errors { get; set; } = [];
    public int DataFilesImported { get; set; }
}

public class ImportError
{
    public Exception? Exception { get; set; }
}