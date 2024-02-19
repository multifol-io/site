using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Web {
public class MyErrorBoundary : ErrorBoundary
{
    public new Exception? CurrentException => base.CurrentException;
}
}