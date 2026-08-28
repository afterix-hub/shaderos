using System;
using System.Collections.Generic;

namespace ToolsStudio.ShaderOS.Dependency
{
    internal enum DependencyNodeKind { Material = 0, Shader = 1, Texture = 2 }

    internal readonly struct DependencyNode : IEquatable<DependencyNode>
    {
        public string            Guid      { get; }
        public string            Name      { get; }
        public DependencyNodeKind Kind     { get; }
        public string            AssetPath { get; }
        public bool              IsOrphan  { get; }

        public DependencyNode(string guid, string name, DependencyNodeKind kind, string assetPath, bool isOrphan = false)
        {
            Guid      = guid      ?? throw new ArgumentNullException(nameof(guid));
            Name      = name      ?? throw new ArgumentNullException(nameof(name));
            Kind      = kind;
            AssetPath = assetPath ?? string.Empty;
            IsOrphan  = isOrphan;
        }

        public bool Equals(DependencyNode other)  => Guid == other.Guid;
        public override bool Equals(object obj)   => obj is DependencyNode n && Equals(n);
        public override int GetHashCode()          => Guid.GetHashCode();
        public override string ToString()          => $"{Kind}:{Name}";
    }

    // Directed graph: Material→Shader, Material→Texture. Immutable after Build().
    internal sealed class DependencyGraph
    {
        private readonly Dictionary<string, DependencyNode>  _nodes;
        private readonly Dictionary<string, List<string>>    _outEdges;
        private readonly Dictionary<string, List<string>>    _inEdges;

        private DependencyGraph(
            Dictionary<string, DependencyNode> nodes,
            Dictionary<string, List<string>>   outEdges,
            Dictionary<string, List<string>>   inEdges)
        {
            _nodes    = nodes;
            _outEdges = outEdges;
            _inEdges  = inEdges;
        }

        public int NodeCount => _nodes.Count;
        public int EdgeCount { get { int c = 0; foreach (var v in _outEdges.Values) c += v.Count; return c; } }

        public bool            ContainsNode(string guid) => _nodes.ContainsKey(guid);
        public DependencyNode  GetNode(string guid)      => _nodes[guid];
        public IEnumerable<DependencyNode> AllNodes      => _nodes.Values;

        public IReadOnlyList<string> GetDependencies(string guid)
            => _outEdges.TryGetValue(guid, out var list) ? list : Array.AsReadOnly(Array.Empty<string>());

        public IReadOnlyList<string> GetDependents(string guid)
            => _inEdges.TryGetValue(guid, out var list) ? list : Array.AsReadOnly(Array.Empty<string>());

        public IEnumerable<DependencyNode> OrphanNodes
        {
            get
            {
                foreach (var node in _nodes.Values)
                    if (!_inEdges.ContainsKey(node.Guid) || _inEdges[node.Guid].Count == 0)
                        yield return node;
            }
        }

        public static DependencyGraph Empty() => new DependencyGraph(
            new Dictionary<string, DependencyNode>(),
            new Dictionary<string, List<string>>(),
            new Dictionary<string, List<string>>());

        internal sealed class Builder
        {
            private readonly Dictionary<string, DependencyNode> _nodes    = new Dictionary<string, DependencyNode>();
            private readonly Dictionary<string, List<string>>   _outEdges = new Dictionary<string, List<string>>();
            private readonly Dictionary<string, List<string>>   _inEdges  = new Dictionary<string, List<string>>();
            private bool _built;

            public Builder AddNode(DependencyNode node)
            {
                if (_built) throw new InvalidOperationException("Builder already consumed.");
                _nodes[node.Guid] = node;
                return this;
            }

            public Builder AddEdge(string fromGuid, string toGuid)
            {
                if (_built) throw new InvalidOperationException("Builder already consumed.");
                if (!_outEdges.TryGetValue(fromGuid, out var outList))
                    _outEdges[fromGuid] = outList = new List<string>(4);
                if (!outList.Contains(toGuid)) outList.Add(toGuid);

                if (!_inEdges.TryGetValue(toGuid, out var inList))
                    _inEdges[toGuid] = inList = new List<string>(4);
                if (!inList.Contains(fromGuid)) inList.Add(fromGuid);

                return this;
            }

            public DependencyGraph Build()
            {
                _built = true;
                // Copy dicts so the graph is truly immutable and the Builder cannot be reused.
                return new DependencyGraph(
                    new Dictionary<string, DependencyNode>(_nodes),
                    new Dictionary<string, List<string>>(_outEdges),
                    new Dictionary<string, List<string>>(_inEdges));
            }
        }
    }
}
