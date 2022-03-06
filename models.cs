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

public enum GameCode
{
    COM3D2,
    CM3D2
}


// ========== Exceptions ========== 
// JSON handling 
abstract class JSONException : Exception
{

}

class JSONNotFoundException : JSONException
{

}

class JSONBadFormatException : JSONException
{

}

class JSONInvalidVersionException : JSONException
{

}

