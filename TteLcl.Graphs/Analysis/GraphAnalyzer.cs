/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// A workspace for graph analysis. Most operates on sets of graph node keys
/// </summary>
public class GraphAnalyzer
{
  private readonly KeySet _nodes;
  private readonly KeyMap<KeySet> _sourceEdges;
  private readonly KeyMap<KeySet> _targetEdges;
  private KeySetMap? _reachMap = null;
  private KeySetMap? _domainMap = null;

  /// <summary>
  /// Create a new GraphAnalyzer, taking a snapshot of nodes and edges in a graph
  /// </summary>
  public GraphAnalyzer(
    Graph g)
  {
    _nodes = new KeySet(g.Nodes.Keys);
    _sourceEdges = new KeyMap<KeySet>();
    _targetEdges = new KeyMap<KeySet>();
    SourceEdges = new KeySetMap(_sourceEdges);
    TargetEdges = new KeySetMap(_targetEdges);
    foreach(var node in g.Nodes.Values)
    {
      var sourceSet = new KeySet(node.Sources.Keys);
      var targetSet = new KeySet(node.Targets.Keys);
      _sourceEdges[node.Key] = sourceSet;
      _targetEdges[node.Key] = targetSet;
    }
  }

  /// <summary>
  /// A read only view on all nodes in this analyzer
  /// </summary>
  public IReadOnlySet<string> Nodes => _nodes;

  /// <summary>
  /// A read-only view on the incoming ("source") edges of all nodes, mapping each
  /// node key to the node keys of nodes with an edge leading to the former node.
  /// </summary>
  public KeySetMap SourceEdges { get; }

  /// <summary>
  /// A read-only view on the outgoing ("target") edges of all nodes, mapping each
  /// node key to the node keys of nodes with an edge coming from the former node.
  /// </summary>
  public KeySetMap TargetEdges { get; }

  ///// <summary>
  ///// Returns the node keys of nodes that have an edge to the node with the given
  ///// <paramref name="targetKey"/> as key.
  ///// </summary>
  ///// <param name="targetKey"></param>
  ///// <returns></returns>
  //public IReadOnlyCollection<string> SourceKeys(string targetKey) => SourceEdges[targetKey];

  ///// <summary>
  ///// Returns the node keys of nodes that have an edge from the node with the given
  ///// <paramref name="sourceKey"/> as key.
  ///// </summary>
  ///// <param name="sourceKey"></param>
  ///// <returns></returns>
  //public IReadOnlyCollection<string> TargetKeys(string sourceKey) => TargetEdges[sourceKey];

  /// <summary>
  /// Get the map that maps each node to its 'reach' (the set of nodes reachable from that node,
  /// excluding the node itself). This is calculated on first call, then cached.
  /// </summary>
  public KeySetMap GetReachMap()
  {
    if(_reachMap == null)
    {
      var reachMap = CalculatePowerMap(TargetEdges);
      _reachMap = new KeySetMap(reachMap);
    }
    return _reachMap;
  }

  /// <summary>
  /// Get the map that maps each node to its 'reach' (the set of nodes reachable from that node,
  /// excluding the node itself). This is calculated on first call, then cached.
  /// </summary>
  public KeySetMap GetDomainMap()
  {
    if(_domainMap == null)
    {
      var domainMap = CalculatePowerMap(SourceEdges);
      _domainMap = new KeySetMap(domainMap);
    }
    return _domainMap;
  }

  /// <summary>
  /// Calculate a map of the power set for all nodes using the given
  /// <paramref name="edges"/> function. For example, if <paramref name="edges"/> is
  /// <see cref="TargetEdges"/> this would return a
  /// map containing the 'reach' of each node (the set nodes reachable from that node), while 
  /// <see cref="SourceEdges"/> would result in the 'domain'
  /// for each node.
  /// </summary>
  /// <param name="edges"></param>
  /// <returns></returns>
  public KeyMap<KeySet> CalculatePowerMap(
    KeySetMap edges)
  {
    var pm = new KeyMap<KeySet>();
    var guard = new KeySet();
    foreach(var seed in _nodes)
    {
      // calculate missing powermap entries starting from seed
      FillPowerSet(seed, edges, pm, guard);
    }
    return pm;
  }

  /// <summary>
  /// Recursively calculate the powerset for one seed node and store it
  /// in the powerMap (filling missing connected nodes on the fly)
  /// </summary>
  /// <param name="seed">
  /// The node to start from
  /// </param>
  /// <param name="edges"></param>
  /// <param name="powerMap">
  /// The cache of completed nodes
  /// </param>
  /// <param name="circularGuard">
  /// Tracks nodes whose calculation is in progress. Calling this function
  /// for a seed that is in this set indicates a circular dependency in
  /// <paramref name="edges"/>, causing an abort.
  /// </param>
  /// <returns></returns>
  private KeySet FillPowerSet(
    string seed,
    KeySetMap edges,
    KeyMap<KeySet> powerMap,
    KeySet circularGuard)
  {
    if(!powerMap.TryGetValue(seed, out var powerSet))
    {
      if(circularGuard.Contains(seed))
      {
        throw new InvalidOperationException(
          $"Found a circular dependency while processing '{seed}'");
      }
      circularGuard.Add(seed);
      powerSet = new KeySet();
      foreach(var next in edges[seed])
      {
        powerSet.Add(next);
        // recurse
        var nextSet = FillPowerSet(next, edges, powerMap, circularGuard);
        powerSet.UnionWith(nextSet);
      }
      powerMap[seed] = powerSet;
      circularGuard.Remove(seed);
    }
    return powerSet;
  }


}
