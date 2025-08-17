using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

class Knoten
{
    private readonly List<Kante> _kanten = new();
    private readonly string _bedeutung;
    private string? _url;

    public Knoten(string bedeutung) => _bedeutung = bedeutung;

    public void add_kante(Kante k) => _kanten.Add(k);
    public int anzahl_kanten() => _kanten.Count;
    public List<Kante> gib_kante() => _kanten;
    public string gib_inhalt() => _bedeutung;
    public void set_url(string url) => _url = url;
    public string? gib_url() => _url;
}

class Kante
{
    private readonly string _bedeutung;
    private Knoten? _nextKnoten = null;

    public Kante(string bedeutung) => _bedeutung = bedeutung;

    public void add_knoten(Knoten k) => _nextKnoten = k;
    public Knoten? gib_knoten() => _nextKnoten;
    public string gib_inhalt() => _bedeutung;
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
    private readonly Dictionary<string, Knoten> _nodes = new();

    public Knoten GetOrCreate(string id)
    {
        if (!_nodes.TryGetValue(id, out var n))
        {
            n = new Knoten(id);
            _nodes[id] = n;
        }
        return n;
    }

    public void AddTriple(string subject, string predicate, string obj)
    {
        var s = GetOrCreate(subject);
        var o = GetOrCreate(obj);
        var e = new Kante(predicate);
        e.add_knoten(o);
        s.add_kante(e);
    }

    public void SetNodeUrl(string id, string url)
    {
        var n = GetOrCreate(id);
        n.set_url(url);
    }

    public IEnumerable<(string predicate, string target)> Neighbors(string start, string? predicate = null)
    {
        if (!_nodes.TryGetValue(start, out var s)) yield break;
        foreach (var e in s.gib_kante())
        {
            if (predicate != null && e.gib_inhalt() != predicate) continue;
            var t = e.gib_knoten();
            if (t != null) yield return (e.gib_inhalt(), t.gib_inhalt());
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
                Id = n.gib_inhalt(),
                Url = n.gib_url(),
                Edges = n.gib_kante().Select(e => new EdgeDTO
                {
                    Predicate = e.gib_inhalt(),
                    Target = e.gib_knoten()!.gib_inhalt()
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

    public Knoten? GetNodeForTraversal(string id) =>
        _nodes.TryGetValue(id, out var n) ? n : null;

    public void PrintAscii(string start, int indent = 0)
    {
        if (!_nodes.TryGetValue(start, out var s)) return;
        Console.WriteLine(new string(' ', indent) + "- " + s.gib_inhalt());
        foreach (var e in s.gib_kante())
        {
            var t = e.gib_knoten();
            if (t != null)
            {
                Console.WriteLine(new string(' ', indent + 2) + $"[{e.gib_inhalt()}]");
                PrintAscii(t.gib_inhalt(), indent + 4);
            }
        }
    }

    public string ToDot()
    {
        var lines = new List<string> { "digraph G {" };
        foreach (var n in _nodes.Values)
        {
            foreach (var e in n.gib_kante())
                lines.Add($"  \"{n.gib_inhalt()}\" -> \"{e.gib_knoten()!.gib_inhalt()}\" [label=\"{e.gib_inhalt()}\"];");
        }
        lines.Add("}");
        return string.Join("\n", lines);
    }
}

class Program
{
    static void durchforsten(Knoten? t_knoten)
    {
        if (t_knoten == null) return;
        Console.WriteLine("Knoteninhalt: " + t_knoten.gib_inhalt());
        if (t_knoten.anzahl_kanten() != 0)
        {
            foreach (var iKante in t_knoten.gib_kante())
            {
                Console.WriteLine("Kanteninhalt: " + iKante.gib_inhalt());
                durchforsten(iKante.gib_knoten());
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

        Console.WriteLine("[Schnelle Suche] Direkte Nachbarn von 'Katze':");
        foreach (var (pred, tar) in g.Neighbors("Katze"))
        {
            Console.WriteLine($"  Katze -[{pred}]-> {tar}");
        }

        Console.WriteLine("\n[Schnelle Suche] Nur 'ist'-Beziehungen ab 'Katze':");
        foreach (var (pred, tar) in g.Neighbors("Katze", "ist"))
            Console.WriteLine($"  Katze -[{pred}]-> {tar}");

        Console.WriteLine("\n[Vertiefte Suche] DFS (bis Tiefe 3) ab 'Katze':");
        var deep = g.DeepSearchDFS("Katze", 3);
        foreach (var path in deep)
            Console.WriteLine("  - " + string.Join(" -> ", path.Select(s => $"{s.from}-[{s.predicate}]->{s.to}")));

        var json = g.ToJson();
        File.WriteAllText("graph.json", json);
        Console.WriteLine("\nJSON in Datei geschrieben: graph.json");
        var jsonFromFile = File.ReadAllText("graph.json");
        var loaded = Graph.FromJson(jsonFromFile);
        Console.WriteLine("\nReload Check (Nachbarn von 'Katze'):");
        foreach (var (pred, tar) in loaded.Neighbors("Katze"))
            Console.WriteLine($"  Katze -[{pred}]-> {tar}");

        Console.WriteLine("\nURL-Check (nach Reload):");
        Console.WriteLine("  Katze URL: " + (loaded.GetNodeForTraversal("Katze")?.gib_url() ?? "-"));
        Console.WriteLine("  Haus URL:  " + (loaded.GetNodeForTraversal("Haus")?.gib_url() ?? "-"));
        Console.WriteLine("  Vogel URL: " + (loaded.GetNodeForTraversal("Vogel")?.gib_url() ?? "-"));

        Console.WriteLine("\nRessourcen (URLs) zu Knoten:");
        foreach (var name in new[] { "Katze", "Haus", "Vogel" })
            Console.WriteLine($"  {name}: {loaded.GetNodeForTraversal(name)?.gib_url() ?? "-"}");

        Console.WriteLine("\nASCII-Darstellung des Graphen:");
        foreach (var node in g.DeepSearchDFS("Katze", 3))
        {
            Console.WriteLine("  " + string.Join(" -> ", node.Select(s => $"{s.from} -[{s.predicate}]-> {s.to}")));
        }

        Console.WriteLine("\nDurchforsten (ab 'Katze'):");
        var root = g.GetNodeForTraversal("Katze");
        durchforsten(root);

        Console.WriteLine("\nASCII-Darstellung ab 'Katze':");
        g.PrintAscii("Katze");

        var dot = g.ToDot();
        File.WriteAllText("graph.dot", dot);
        Console.WriteLine("\nGraphviz-Datei geschrieben: graph.dot  (optional rendern mit: dot -Tpng graph.dot -o graph.png)");

        while (true)
        {
            Console.Write("\nStartknoten eingeben (oder leer zum Beenden): ");
            var start = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(start)) break;

            Console.Write("Suchmodus [schnell|tief]: ");
            var mode = (Console.ReadLine() ?? "").Trim().ToLower();

            if (mode == "schnell")
            {
                Console.WriteLine($"\n[Schnelle Suche] Direkte Nachbarn von '{start}':");
                foreach (var (pred, tar) in g.Neighbors(start))
                    Console.WriteLine($"  {start} -[{pred}]-> {tar}");
            }
            else
            {
                Console.WriteLine($"\n[Vertiefte Suche] DFS (bis Tiefe 3) ab '{start}':");
                var deepPaths = g.DeepSearchDFS(start, 3);
                foreach (var path in deepPaths)
                    Console.WriteLine("  - " + string.Join(" -> ", path.Select(s => $"{s.from}-[{s.predicate}]->{s.to}")));
            }
        }

        sw.Stop();
        Console.WriteLine($"\nLaufzeit (Demo): {sw.ElapsedMilliseconds} ms");

        Console.WriteLine("\nFertig. ENTER zum Beenden.");
        Console.ReadLine();
    }
}
