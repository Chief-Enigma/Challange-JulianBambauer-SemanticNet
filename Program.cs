using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

class Node
{
    private readonly List<Edge> _edges = new();
    private readonly string _label;
    private string? _url;

    public Node(string label) => _label = label;

    public void AddEdge(Edge e) => _edges.Add(e);
    public int EdgeCount() => _edges.Count;
    public List<Edge> GetEdges() => _edges;
    public string GetLabel() => _label;
    public void SetUrl(string url) => _url = url;
    public string? GetUrl() => _url;
}

class Edge
{
    private readonly string _label;
    private Node? _nextNode = null;

    public Edge(string label) => _label = label;

    public void AddNode(Node n) => _nextNode = n;
    public Node? GetNode() => _nextNode;
    public string GetLabel() => _label;
}

class EdgeDTO
{
    public string Predicate { get; set; } = "";
    public string Target { get; set; } = "";
}

class NodeDTO
{
    public string Id { get; set; } = "";
    public string? Url { get; set; }
    public List<EdgeDTO> Edges { get; set; } = new();
}

class GraphDTO
{
    public List<NodeDTO> Nodes { get; set; } = new();
}

class Graph
{
    private readonly Dictionary<string, Node> _nodes = new();

    public Node GetOrCreate(string id)
    {
        if (!_nodes.TryGetValue(id, out var n))
        {
            n = new Node(id);
            _nodes[id] = n;
        }
        return n;
    }

    public void AddTriple(string subject, string predicate, string obj)
    {
        var s = GetOrCreate(subject);
        var o = GetOrCreate(obj);
        var e = new Edge(predicate);
        e.AddNode(o);
        s.AddEdge(e);
    }

    public void SetNodeUrl(string id, string url)
    {
        var n = GetOrCreate(id);
        n.SetUrl(url);
    }

    public IEnumerable<(string predicate, string target)> Neighbors(string start, string? predicate = null)
    {
        if (!_nodes.TryGetValue(start, out var s)) yield break;
        foreach (var e in s.GetEdges())
        {
            if (predicate != null && e.GetLabel() != predicate) continue;
            var t = e.GetNode();
            if (t != null) yield return (e.GetLabel(), t.GetLabel());
        }
    }

    public List<List<(string from, string predicate, string to)>> DeepSearchDFS(string start, int maxDepth = 3, string? predicateFilter = null)
    {
        var results = new List<List<(string from, string predicate, string to)>>();
        var visited = new HashSet<string>();

        void dfs(string current, int depth, List<(string from, string predicate, string to)> path)
        {
            if (depth > maxDepth) return;
            var key = $"{current}|{depth}|{string.Join("/", path.Select(p => p.predicate + "->" + p.to))}";
            if (!visited.Add(key)) return;

            foreach (var (pred, tar) in Neighbors(current))
            {
                if (predicateFilter != null && pred != predicateFilter) continue;
                var step = (from: current, predicate: pred, to: tar);
                var np = new List<(string from, string predicate, string to)>(path) { step };
                results.Add(np);
                dfs(tar, depth + 1, np);
            }
        }

        if (_nodes.ContainsKey(start))
            dfs(start, 1, new());
        return results;
    }

    public string ToJson()
    {
        var dto = new GraphDTO
        {
            Nodes = _nodes.Values.Select(n => new NodeDTO
            {
                Id = n.GetLabel(),
                Url = n.GetUrl(),
                Edges = n.GetEdges().Select(e => new EdgeDTO
                {
                    Predicate = e.GetLabel(),
                    Target = e.GetNode()!.GetLabel()
                }).ToList()
            }).ToList()
        };
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    public static Graph FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<GraphDTO>(json) ?? new GraphDTO();
        var g = new Graph();
        foreach (var n in dto.Nodes) g.GetOrCreate(n.Id);
        foreach (var n in dto.Nodes)
        {
            if (!string.IsNullOrWhiteSpace(n.Url))
            {
                g.SetNodeUrl(n.Id, n.Url!);
            }
        }
        foreach (var n in dto.Nodes)
            foreach (var e in n.Edges)
                g.AddTriple(n.Id, e.Predicate, e.Target);
        return g;
    }

    public Node? GetNodeForTraversal(string id) =>
        _nodes.TryGetValue(id, out var n) ? n : null;

    public void PrintAscii(string start, int indent = 0)
    {
        if (!_nodes.TryGetValue(start, out var s)) return;
        Console.WriteLine(new string(' ', indent) + "- " + s.GetLabel());
        foreach (var e in s.GetEdges())
        {
            var t = e.GetNode();
            if (t != null)
            {
                Console.WriteLine(new string(' ', indent + 2) + $"[{e.GetLabel()}]");
                PrintAscii(t.GetLabel(), indent + 4);
            }
        }
    }

    public string ToDot()
    {
        var lines = new List<string> { "digraph G {" };
        foreach (var n in _nodes.Values)
        {
            foreach (var e in n.GetEdges())
                lines.Add($"  \"{n.GetLabel()}\" -> \"{e.GetNode()!.GetLabel()}\" [label=\"{e.GetLabel()}\"];");
        }
        lines.Add("}");
        return string.Join("\n", lines);
    }
}

class Program
{
    static void Traverse(Node? startNode)
    {
        if (startNode == null) return;
        Console.WriteLine("Node content: " + startNode.GetLabel());
        if (startNode.EdgeCount() != 0)
        {
            foreach (var edge in startNode.GetEdges())
            {
                Console.WriteLine("Edge content: " + edge.GetLabel());
                Traverse(edge.GetNode());
            }
        }
    }

    static void Main()
    {
        var sw = Stopwatch.StartNew();

        var g = new Graph();
        g.AddTriple("Katze", "ist", "Tier");
        g.AddTriple("Katze", "jagt", "Maus");
        g.AddTriple("Maus", "ist", "Lebewesen");
        g.AddTriple("Vogel", "ist", "Tier");
        g.AddTriple("Vogel", "singt", "Lied");
        g.AddTriple("Haus", "ist", "Gebäude");
        g.AddTriple("Katze", "schläft im", "Haus");

        g.SetNodeUrl("Katze", "https://example.com/doku/katze-wiki");
        g.SetNodeUrl("Haus", "https://example.com/bilder/haus.png");
        g.SetNodeUrl("Vogel", "https://example.com/audio/vogelsang.mp3");

        Console.WriteLine("[Quick Search] Direct neighbors of 'Katze':");
        foreach (var (pred, tar) in g.Neighbors("Katze"))
        {
            Console.WriteLine($"  Katze -[{pred}]-> {tar}");
        }

        Console.WriteLine("\n[Quick Search] Only 'is' relations from 'Katze':");
        foreach (var (pred, tar) in g.Neighbors("Katze", "ist"))
            Console.WriteLine($"  Katze -[{pred}]-> {tar}");

        Console.WriteLine("\n[Deep Search] DFS (up to depth 3) from 'Katze':");
        var deep = g.DeepSearchDFS("Katze", 3);
        foreach (var path in deep)
            Console.WriteLine("  - " + string.Join(" -> ", path.Select(s => $"{s.from}-[{s.predicate}]->{s.to}")));

        var json = g.ToJson();
        File.WriteAllText("graph.json", json);
        Console.WriteLine("\nJSON written to file: graph.json");
        var jsonFromFile = File.ReadAllText("graph.json");
        var loaded = Graph.FromJson(jsonFromFile);
        Console.WriteLine("\nReload check (neighbors of 'Katze'):");
        foreach (var (pred, tar) in loaded.Neighbors("Katze"))
            Console.WriteLine($"  Katze -[{pred}]-> {tar}");

        Console.WriteLine("\nURL check (after reload):");
        Console.WriteLine("  Katze URL: " + (loaded.GetNodeForTraversal("Katze")?.GetUrl() ?? "-"));
        Console.WriteLine("  Haus URL:  " + (loaded.GetNodeForTraversal("Haus")?.GetUrl() ?? "-"));
        Console.WriteLine("  Vogel URL: " + (loaded.GetNodeForTraversal("Vogel")?.GetUrl() ?? "-"));

        Console.WriteLine("\nResources (URLs) for nodes:");
        foreach (var name in new[] { "Katze", "Haus", "Vogel" })
            Console.WriteLine($"  {name}: {loaded.GetNodeForTraversal(name)?.GetUrl() ?? "-"}");

        Console.WriteLine("\nASCII representation of graph:");
        foreach (var node in g.DeepSearchDFS("Katze", 3))
        {
            Console.WriteLine("  " + string.Join(" -> ", node.Select(s => $"{s.from} -[{s.predicate}]-> {s.to}")));
        }

        Console.WriteLine("\nTraverse (from 'Katze'):");
        var root = g.GetNodeForTraversal("Katze");
        Traverse(root);

        Console.WriteLine("\nASCII representation from 'Katze':");
        g.PrintAscii("Katze");

        var dot = g.ToDot();
        File.WriteAllText("graph.dot", dot);
        Console.WriteLine("\nGraphviz file written: graph.dot  (optional render with: dot -Tpng graph.dot -o graph.png)");

        while (true)
        {
            Console.Write("\nEnter start node (or empty to exit): ");
            var start = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(start)) break;

            Console.Write("Search mode [quick|deep]: ");
            var mode = (Console.ReadLine() ?? "").Trim().ToLower();

            if (mode == "quick")
            {
                Console.WriteLine($"\n[Quick Search] Direct neighbors of '{start}':");
                foreach (var (pred, tar) in g.Neighbors(start))
                    Console.WriteLine($"  {start} -[{pred}]-> {tar}");
            }
            else
            {
                Console.WriteLine($"\n[Deep Search] DFS (up to depth 3) from '{start}':");
                var deepPaths = g.DeepSearchDFS(start, 3);
                foreach (var path in deepPaths)
                    Console.WriteLine("  - " + string.Join(" -> ", path.Select(s => $"{s.from}-[{s.predicate}]->{s.to}")));
            }
        }

        sw.Stop();
        Console.WriteLine($"\nRuntime (demo): {sw.ElapsedMilliseconds} ms");

        Console.WriteLine("\nDone. Press ENTER to exit.");
        Console.ReadLine();
    }
}
