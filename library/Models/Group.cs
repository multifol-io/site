namespace Models;

public class Group<T, U>(T groupInfo, List<U> items) : List<U>(items) where U : class
{
    public T GroupInfo { get; private set; } = groupInfo;

    public static List<Group<T, U>> Create(IEnumerable<T> collection, string itemsPropertyName)
    {
        var groups = new List<Group<T, U>>();
        var propInfo = typeof(T).GetProperty(itemsPropertyName);
        foreach (var outerObject in collection)
        {
            var value = propInfo!.GetValue(outerObject) as List<U>;
            if (value != null)
            {
                var group = new Group<T, U>(outerObject, value);
                groups.Add(group);
            }
        }

        return groups;
    }
}

public class HoldingGroupInfo(string investmentOrderCategory)
{
    public string InvestmentOrderCategory { get; private set; } = investmentOrderCategory;
    public double? Value { get; set; }
}

