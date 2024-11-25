public class DependencyNode
{
    public Type? EntityType { get; set; }
    public HashSet<Type> Dependencies { get; set; } = new HashSet<Type>();
    public bool IsProcessed { get; set; }
}

public class DependencyGraph
{
    private Dictionary<Type, DependencyNode> _nodes = new Dictionary<Type, DependencyNode>();

    public void AddNode(Type entityType)
    {
        if (!_nodes.ContainsKey(entityType))
        {
            _nodes[entityType] = new DependencyNode { EntityType = entityType };
        }
    }

    public void AddDependency(Type from, Type to)
    {
        AddNode(from);
        AddNode(to);
        _nodes[from].Dependencies.Add(to);
    }

    public List<Type> GetSortedEntities()
    {
        var sorted = new List<Type>();
        var visited = new HashSet<Type>();
        var processing = new HashSet<Type>();

        foreach (var node in _nodes.Keys)
        {
            if (!visited.Contains(node))
            {
                TopologicalSort(node, visited, processing, sorted);
            }
        }

        return sorted;
    }

    private void TopologicalSort(Type current, HashSet<Type> visited, HashSet<Type> processing, List<Type> sorted)
    {
        if (processing.Contains(current))
        {
            throw new InvalidOperationException($"Circular dependency detected involving type {current.Name}");
        }

        if (visited.Contains(current))
        {
            return;
        }

        processing.Add(current);

        foreach (var dependency in _nodes[current].Dependencies)
        {
            TopologicalSort(dependency, visited, processing, sorted);
        }

        processing.Remove(current);
        visited.Add(current);
        sorted.Add(current);
    }
}
