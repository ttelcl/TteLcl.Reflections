using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using TteLcl.Graphs.Analysis;

namespace TteLcl.Graphs;

/// <summary>
/// A generic node in a <see cref="Graph"/>
/// </summary>
public class GraphNode: IHasMetadata, IHasKey
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
  /// Remove any incoming and outgoing edges to or from nodes in the given
  /// set of node keys (to match nodes being removed). This does NOT disconnect
  /// the other side, since it is assumed the other side is no longer part
  /// of the graph at all.
  /// </summary>
  /// <param name="remoteNodeKeys"></param>
  public void RemoveEdges(IEnumerable<string> remoteNodeKeys)
  {
    foreach(var remoteNodeKey in remoteNodeKeys)
    {
      _targets.Remove(remoteNodeKey);
      _sources.Remove(remoteNodeKey);
    }
  }

  /// <summary>
  /// Removes the edge to the target node on both this side and the
  /// other side. Use this to remove edges when the nodes stay in the
  /// graph.
  /// </summary>
  /// <param name="targetKey">
  /// The key of the target node to disconnect
  /// </param>
  /// <returns>
  /// The <see cref="GraphEdge"/> that was removed, or null if not found
  /// </returns>
  public GraphEdge? DisconnectTarget(string targetKey)
  {
    if(_targets.TryGetValue(targetKey, out var edge))
    {
      _targets.Remove(targetKey);
      edge.Target._sources.Remove(Key);
      return edge;
    }
    return null;
  }

  /// <summary>
  /// Disconnect all targets except the ones with keys in <paramref name="targetsToKeep"/>.
  /// Both ends of the target edge are disconnected
  /// </summary>
  /// <param name="targetsToKeep"></param>
  public void DisconnectAllExcept(IEnumerable<string> targetsToKeep)
  {
    var targetsToRemove = KeySet.CreateDifference(_targets.Keys, targetsToKeep);
    foreach(var targetKey in targetsToRemove)
    {
      DisconnectTarget(targetKey);
    }
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
