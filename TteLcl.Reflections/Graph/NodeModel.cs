using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// Models nodes and their connections in a graph
/// </summary>
public class NodeModel
{
  private readonly Dictionary<string, TargetEdge> _targetEdges;
  private readonly Dictionary<string, SourceEdge> _sourceEdges;

  internal NodeModel(
    AssemblyNode node)
  {
    Node = node;
    _targetEdges = new Dictionary<string, TargetEdge>();
    _sourceEdges = new Dictionary<string, SourceEdge>();
  }

  /// <summary>
  /// The serializable node data that this object expands on
  /// </summary>
  public AssemblyNode Node { get; }

  /// <summary>
  /// The key identifying this node
  /// </summary>
  public string Key => Node.Key;

  /// <summary>
  /// The collection of outgoing edges, indexed by target key
  /// </summary>
  public IReadOnlyDictionary<string, TargetEdge> Targets => _targetEdges;

  /// <summary>
  /// The collection of incoming edges, indexed by source key
  /// </summary>
  public IReadOnlyDictionary<string, SourceEdge> Sources => _sourceEdges;

  internal void ConnectTarget(AssemblyEdge edge, NodeModel target)
  {
    var te = new TargetEdge(edge, target);
    _targetEdges.Add(target.Key, te);
  }

  internal void ConnectSource(AssemblyEdge edge, NodeModel source)
  {
    var se = new SourceEdge(edge, source);
    _sourceEdges.Add(source.Key, se);
  }

  /// <summary>
  /// An outgoing edge from this <see cref="NodeModel"/> to
  /// a target node
  /// </summary>
  public class TargetEdge
  {
    internal TargetEdge(
      AssemblyEdge edge,
      NodeModel target)
    {
      Edge = edge;
      Target = target;
    }

    /// <summary>
    /// The serializable descriptor of this edge
    /// </summary>
    public AssemblyEdge Edge { get; }

    /// <summary>
    /// The target node
    /// </summary>
    public NodeModel Target { get; }
  }

  /// <summary>
  /// An incoming edge from a source node to
  /// this <see cref="NodeModel"/>
  /// </summary>
  public class SourceEdge
  {
    internal SourceEdge(
      AssemblyEdge edge,
      NodeModel source)
    {
      Edge = edge;
      Source = source;
    }

    /// <summary>
    /// The serializable descriptor of this edge
    /// </summary>
    public AssemblyEdge Edge { get; }

    /// <summary>
    /// The source node
    /// </summary>
    public NodeModel Source { get; }
  }

}
