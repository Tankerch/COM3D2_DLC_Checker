public class DLCList
{
    public int Version { get; set; }

    public string AppMinVersion { get; set; }
    public List<Item> Items { get; set; }
}

public class Item
{
    public string Name { get; set; }
    public List<string> Files { get; set; }
}


