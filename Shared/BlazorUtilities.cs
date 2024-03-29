using Microsoft.AspNetCore.Components.Forms;

namespace Models
{
    public static class BlazorUtilities
    {
        public static IEnumerable<NamedStream>? GetNamedStreamsFromBrowserFiles(IEnumerable<IBrowserFile> browserFiles)
        {
            List<NamedStream> namedStreams = [];
            foreach (var file in browserFiles)
            {
                var namedStream = new NamedStream(file.Name, stream: file.OpenReadStream());
                namedStreams.Add(namedStream);
            }

            if (namedStreams.Count > 0)
            {
                return namedStreams;
            }
            else
            {
                return null;
            }
        }
    }
}
