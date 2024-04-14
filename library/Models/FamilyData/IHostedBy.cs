using DocumentFormat.OpenXml.Office2010.PowerPoint;

namespace Models
{
    public interface IHostedBy<T> where T : class
    {
        WeakReference<T>? Host { get; set; }
        bool IsDirty { get; set; }
    }
}