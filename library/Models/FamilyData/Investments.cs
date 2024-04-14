using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Models;

public class HostedCollection<T, U> : ObservableCollection<U> where U : IHostedBy<T>, INotifyPropertyChanged where T : class
{
    public HostedCollection(T host)
    {
        Host = new WeakReference<T>(host);
    }

    public HostedCollection()
    {
        // have to have paramaterless ctor for JsonSerialization
    }

    public WeakReference<T>? Host { get; set; }

    protected override void InsertItem(int index, U item)
    {
        item.Host = Host;
        base.InsertItem(index, item);
        IsDirty = true;
        AreStatsDirty = true;
        item.PropertyChanged += Investment_PropertyChanged;
    }

    public void ListenForValueChange(U item)
    {
        item.PropertyChanged += Investment_PropertyChanged;
    }

    private async void Investment_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        IsDirty = true;

        switch (e.PropertyName)
        {
            case "Value":
                AreStatsDirty = true;
                var investment = sender as Investment;
                if (investment.Host != null)
                {
                    investment.Host.TryGetTarget(out Account host);
                    var account = host as Account;
                    account.FamilyData.TryGetTarget(out FamilyData familyData);
                    await familyData.UpdateStatsAsync();
                }

                break;
        }
    }


    protected override void RemoveItem(int index)
    {
        var item = this[index];
        base.RemoveItem(index);
        item.PropertyChanged -= Investment_PropertyChanged;
        item.Host = null;
        IsDirty = true;
        AreStatsDirty = true;
    }

    private bool isDirty;

    [JsonIgnore]
    public bool IsDirty
    {
        get { return isDirty; }
        set
        {
            isDirty = value;
        }
    }

    private bool areStatsDirty;
    [JsonIgnore]
    public bool AreStatsDirty
    {
        get
        {
            return areStatsDirty;
        }
        set
        {
            areStatsDirty = value;
        }
    }

    public void AddRange(IEnumerable<U> collection)
    {
        foreach (var item in collection)
        {
            this.Add(item);
        }
    }
}
