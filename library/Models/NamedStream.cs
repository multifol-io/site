namespace Models;
public class NamedStream
{
    public NamedStream(string name, Stream stream)
    {
        Name = name;
        Stream = stream;
    }
    public string Name { get; private set; }
    public Stream Stream { get; private set; }
}
