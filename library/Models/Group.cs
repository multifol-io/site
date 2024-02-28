using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Group<T, U> : List<U> where U : class
{
    public T GroupInfo { get; private set; }

    public Group(T groupInfo, List<U> items) : base(items)
    {
        GroupInfo = groupInfo;
    }

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

public class HoldingGroupInfo
{
    public HoldingGroupInfo(string investmentOrderCategory)
    {
        InvestmentOrderCategory = investmentOrderCategory;
    }
    public string InvestmentOrderCategory { get; private set; }
    public double? Value { get; set; }
}

