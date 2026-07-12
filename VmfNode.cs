namespace Vmf2Map;

internal sealed class VmfNode(string name)
{
    public string Name { get; } = name;
    public List<KeyValuePair<string, string>> Properties { get; } = [];
    public List<VmfNode> Children { get; } = [];

    public string? Get(string key)
    {
        foreach (var property in Properties)
        {
            if (property.Key == key)
            {
                return property.Value;
            }
        }

        return null;
    }

    public IEnumerable<VmfNode> ChildrenNamed(string name)
    {
        foreach (var child in Children)
        {
            if (child.Name == name)
            {
                yield return child;
            }
        }
    }

    public bool HasChild(string name)
    {
        foreach (var child in Children)
        {
            if (child.Name == name)
            {
                return true;
            }
        }

        return false;
    }
}
