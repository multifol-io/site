using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Group<T,U> : List<U> where U : class
{
    public T GroupInfo { get; private set; }

    public Group(T groupInfo, List<U> items) : base(items)
    {
        GroupInfo = groupInfo;
    }
}
