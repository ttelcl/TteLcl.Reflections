using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace TteLcl.Graphs;

/// <summary>
/// A generic node in a <see cref="Graph"/>
/// </summary>
public class GraphNode: IHasMetadata
{
  private readonly Dictionary<string, GraphEdge> _sources;
  private readonly Dictionary<string, GraphEdge> _targets;

  /// <summary>
  /// Create a new <see cref="GraphNode"/>.
  /// </summary>
  /// <param name="key">
  /// The key for this node
  /// </param>
  /// <param name="metadata">
  /// The metadata that will be copied into the new node (optional)
  /// </param>
  internal GraphNode(
    string key,
    Metadata? metadata = null)
  {
    Key = key;
    _sources = new Dictionary<string, GraphEdge>(StringComparer.OrdinalIgnoreCase);
    _targets = new Dictionary<string, GraphEdge>(StringComparer.OrdinalIgnoreCase);
    Metadata = new Metadata();
    if(metadata != null)
    {
      Metadata.Import(metadata);
    }
  }

  /// <summary>
  /// The key identifying this node
  /// </summary>
  public string Key { get; }

  /// <inheritdoc/>
  public Metadata Metadata { get; }

  /// <summary>
  /// The incoming edges (where this node is <see cref="GraphEdge.Target"/>),
  /// indexed by the target key
  /// </summary>
  public IReadOnlyDictionary<string, GraphEdge> Sources => _sources;

  /// <summary>
  /// The outgoing edges (where this node is <see cref="GraphEdge.Source"/>),
  /// indexed by the source key
  /// </summary>
  public IReadOnlyDictionary<string, GraphEdge> Targets => _targets;

  /// <summary>
  /// Connect this node to the <paramref name="target"/> node, creating
  /// a new <see cref="GraphEdge"/>.
  /// </summary>
  /// <param name="target">
  /// The target node to connect to
  /// </param>
  /// <param name="metadata">
  /// The optional metadata to copy into the new edge
  /// </param>
  /// <returns>
  /// The newly created edge
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if this node is already connected to <paramref name="target"/>.
  /// </exception>
  public GraphEdge Connect(GraphNode target, Metadata? metadata = null)
  {
    if(_targets.ContainsKey(target.Key))
    {
      throw new InvalidOperationException(
        $"Duplicate connection from '{Key}' to '{target.Key}'");
    }
    var edge = new GraphEdge(this, target, metadata);
    _targets[target.Key] = edge;
    target._sources[Key] = edge;
    return edge;
  }

  /// <summary>
  /// Return a JSON description of this node and its outgoing edges
  /// </summary>
  public JObject Serialize()
  {
    var o = new JObject();
    o["key"] = Key;
    Metadata.AddToObject(o);
    var targets = new JObject();
    foreach(var targetEdge in _targets.Values.OrderBy(e => e.Target.Key))
    {
      var edge = new JObject();
      targetEdge.Metadata.AddToObject(edge);
      targets[targetEdge.Target.Key] = edge;
    }
    o["targets"] = targets;
    return o;
  }
}
