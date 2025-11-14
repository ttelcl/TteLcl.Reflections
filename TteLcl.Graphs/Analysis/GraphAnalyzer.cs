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
  private readonly KeySet _seeds;
  private readonly KeySet _sinks;
  private KeySetMapView? _reachMap = null;
  private KeySetMapView? _domainMap = null;

  /// <summary>
  /// Create a new GraphAnalyzer, taking a snapshot of nodes and edges in a graph
  /// </summary>
  public GraphAnalyzer(
    Graph g)
  {
    _nodes = new KeySet(g.Nodes.Keys);
    _seeds = new KeySet();
    _sinks = new KeySet();
    var sourceEdges = new KeySetMap();
    var targetEdges = new KeySetMap();
    SourceEdges = new KeySetMapView(sourceEdges);
    TargetEdges = new KeySetMapView(targetEdges);
    foreach(var node in g.Nodes.Values)
    {
      var sourceSet = new KeySet(node.Sources.Keys);
      var targetSet = new KeySet(node.Targets.Keys);
      sourceEdges[node.Key] = sourceSet;
      targetEdges[node.Key] = targetSet;
      if(sourceSet.Count == 0)
      {
        _seeds.Add(node.Key);
      }
      if(targetSet.Count == 0)
      {
        _sinks.Add(node.Key);
      }
    }
  }

  /// <summary>
  /// A read only view on all nodes in this analyzer
  /// </summary>
  public IReadOnlySet<string> Nodes => _nodes;

  /// <summary>
  /// Keys of the nodes without sources
  /// </summary>
  public IReadOnlySet<string> Seeds => _seeds;

  /// <summary>
  /// Keys of the nodes without targets
  /// </summary>
  public IReadOnlySet<string> Sinks => _sinks;

  /// <summary>
  /// A read-only view on the incoming ("source") edges of all nodes, mapping each
  /// node key to the node keys of nodes with an edge leading to the former node.
  /// </summary>
  public KeySetMapView SourceEdges { get; }

  /// <summary>
  /// A read-only view on the outgoing ("target") edges of all nodes, mapping each
  /// node key to the node keys of nodes with an edge coming from the former node.
  /// </summary>
  public KeySetMapView TargetEdges { get; }

  /// <summary>
  /// The total number of nodes in this graph
  /// </summary>
  public int NodeCount => _nodes.Count;

  /// <summary>
  /// The total number of edges in this graph
  /// </summary>
  public int EdgeCount => TargetEdges.Values.Sum(e => e.Count);

  /// <summary>
  /// The number of seed nodes in this graph (nodes without incoming edges)
  /// </summary>
  public int SeedCount => _seeds.Count;

  /// <summary>
  /// The number of sink nodes in this graph (nodes without outgoing edges)
  /// </summary>
  public int SinkCount => _sinks.Count;

  /// <summary>
  /// Get the map that maps each node to its 'reach' (the set of nodes reachable from that node,
  /// excluding the node itself). This is calculated on first call, then cached.
  /// </summary>
  public KeySetMapView GetReachMap(bool skipCircles = false)
  {
    if(_reachMap == null)
    {
      _reachMap = CalculatePowerMap(TargetEdges, skipCircles);
    }
    return _reachMap;
  }

  /// <summary>
  /// Get the map that maps each node to its 'reach' (the set of nodes reachable from that node,
  /// excluding the node itself). This is calculated on first call, then cached.
  /// </summary>
  public KeySetMapView GetDomainMap(bool skipCircles = false)
  {
    if(_domainMap == null)
    {
      _domainMap = CalculatePowerMap(SourceEdges, skipCircles);
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
  /// <param name="skipCircles"></param>
  /// <returns></returns>
  public KeySetMapView CalculatePowerMap(
    KeySetMapView edges,
    bool skipCircles = false)
  {
    var pm = new KeySetMap();
    var guard = new KeySet();
    foreach(var seed in _nodes)
    {
      // calculate missing powermap entries starting from seed
      FillPowerSet(seed, edges, pm, guard, skipCircles);
    }
    return new KeySetMapView(pm);
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
  /// <param name="skipCircles">
  /// If true, stop recusrion when detecting a circular dependency instead
  /// of aborting. This breaks dependency cycles at a "random" point
  /// </param>
  /// <returns></returns>
  private KeySet FillPowerSet(
    string seed,
    KeySetMapView edges,
    KeyMap<KeySet> powerMap,
    KeySet circularGuard,
    bool skipCircles)
  {
    if(!powerMap.TryGetValue(seed, out var powerSet))
    {
      if(circularGuard.Contains(seed))
      {
        if(skipCircles)
        {
          powerSet = new KeySet(edges[seed]);
          powerMap[seed] = powerSet;
          return powerSet;
        }
        var guardSet = String.Join(", ", circularGuard);
        throw new InvalidOperationException(
          $"Found a circular dependency while processing '{seed}'. Guard set = {guardSet}");
      }
      circularGuard.Add(seed);
      powerSet = new KeySet();
      foreach(var next in edges[seed])
      {
        powerSet.Add(next);
        // recurse
        var nextSet = FillPowerSet(next, edges, powerMap, circularGuard, skipCircles);
        powerSet.UnionWith(nextSet);
      }
      powerMap[seed] = powerSet;
      circularGuard.Remove(seed);
    }
    return powerSet;
  }


}
