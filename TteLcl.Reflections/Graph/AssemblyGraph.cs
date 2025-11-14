using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Newtonsoft.Json;

using TteLcl.Graphs;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// Describes a collection of assemblies and their dependencies
/// </summary>
public class AssemblyGraph
{
  private readonly Dictionary<string, AssemblyNode> _nodes;
  private readonly Dictionary<string, NodeModel> _nodeModels;
  private readonly List<AssemblyEdge> _edges;

  /// <summary>
  /// Create a new <see cref="AssemblyGraph"/>.
  /// This is the Json deserialization constructor.
  /// </summary>
  /// <param name="nodes"></param>
  /// <param name="edges"></param>
  /// <exception cref="InvalidOperationException"></exception>
  [JsonConstructor]
  public AssemblyGraph(
    IEnumerable<AssemblyNode> nodes,
    IEnumerable<AssemblyEdge> edges)
  {
    _nodes = new Dictionary<string, AssemblyNode>();
    _nodeModels = new Dictionary<string, NodeModel>();
    _edges = new List<AssemblyEdge>();
    foreach(var node in nodes)
    {
      var key = node.Key;
      if(_nodes.TryGetValue(key, out var existingNode))
      {
        throw new InvalidOperationException(
          $"Duplicate node '{key}'");
      }
      _nodes.Add(key, node);
      var model = new NodeModel(node);
      _nodeModels.Add(key, model);
    }
    foreach(var edge in edges)
    {
      AddEdge(edge);
    }
  }

  /// <summary>
  /// The collection of nodes. This is just a view on the values in
  /// <see cref="NodeMap"/>.
  /// </summary>
  [JsonProperty("nodes")]
  public IReadOnlyCollection<AssemblyNode> Nodes => _nodes.Values;

  /// <summary>
  /// The collection of edges, including their tags
  /// </summary>
  [JsonProperty("edges")]
  public IReadOnlyCollection<AssemblyEdge> Edges => _edges;

  /// <summary>
  /// The mapping of full assembly names to assembly nodes
  /// </summary>
  [JsonIgnore]
  public IReadOnlyDictionary<string, AssemblyNode> NodeMap => _nodes;

  /// <summary>
  /// The mapping of full assembly names to fully connected node models
  /// </summary>
  [JsonIgnore]
  public IReadOnlyDictionary<string, NodeModel> NodeModels => _nodeModels;

  /// <summary>
  /// Add an <see cref="AssemblyNode"/> if it isn't already present.
  /// Returns the node in the graph (either <paramref name="node"/> if it was new
  /// or the existing node)
  /// </summary>
  /// <param name="node"></param>
  public AssemblyNode AddNode(AssemblyNode node)
  {
    if(_nodes.TryGetValue(node.Key, out var existingNode))
    {
      return existingNode;
    }
    _nodes.Add(node.Key, node);
    var model = new NodeModel(node);
    _nodeModels.Add(node.Key, model);
    return node;
  }

  /// <summary>
  /// Create an <see cref="AssemblyNode"/>, add it to this graph, and return true.
  /// If the node already existed return false. In both cases
  /// <paramref name="node"/> will containing the node in this graph (new or existing)
  /// </summary>
  /// <param name="asm">
  /// The assembly to create a node for
  /// </param>
  /// <param name="registry">
  /// The registry providing tags for the node
  /// </param>
  /// <param name="node">
  /// The node that was added or the node that already existed
  /// </param>
  /// <returns>
  /// True if a new node was added, false if the node already existed
  /// </returns>
  /// <exception cref="InvalidOperationException"></exception>
  public bool AddNode(
    Assembly asm,
    AssemblyFileCollection registry,
    out AssemblyNode node)
  {
    var assemblyName = asm.GetName();
    var key = assemblyName.FullName;
    if(_nodes.TryGetValue(key, out var existingNode))
    {
      node = existingNode;
      return false;
    }
    if(!registry.TryCreateNode(
      asm, out var node2))
    {
      throw new InvalidOperationException(
        $"Cannot add unregistered assemblies: {asm.FullName}");
    }
    node = AddNode(node2);
    return true;
  }

  /// <summary>
  /// Get the full model for an <see cref="AssemblyNode"/>
  /// </summary>
  /// <param name="node"></param>
  /// <returns></returns>
  public NodeModel ModelForNode(AssemblyNode node)
  {
    return _nodeModels[node.Key];
  }

  /// <summary>
  /// Add an edge
  /// </summary>
  /// <param name="edge"></param>
  /// <exception cref="InvalidOperationException"></exception>
  public bool AddEdge(AssemblyEdge edge)
  {
    if(!_nodes.TryGetValue(edge.DependentKey, out var sourceNode))
    {
      throw new InvalidOperationException(
        $"Invalid edge: Source node unknown: {edge.DependentKey}");
    }
    if(!_nodes.TryGetValue(edge.DependencyKey, out var targetNode))
    {
      throw new InvalidOperationException(
        $"Invalid edge: Target node unknown: {edge.DependencyKey}");
    }
    var sourceModel = _nodeModels[sourceNode.Key];
    var targetModel = _nodeModels[targetNode.Key];
    if(sourceModel.Targets.ContainsKey(targetModel.Key))
    {
      // Attempt to double connect. Maybe because of an aliased assembly. Abort!
      return false;
    }
    _edges.Add(edge);
    sourceModel.ConnectTarget(edge, targetModel);
    targetModel.ConnectSource(edge, sourceModel);
    return true;
  }

  /// <summary>
  /// Create a new generic graph from this specialized one
  /// </summary>
  /// <returns></returns>
  public TteLcl.Graphs.Graph ExportAsGraph()
  {
    var graph = new TteLcl.Graphs.Graph();
    // First pass: create nodes
    foreach(var nodeModel in NodeModels.Values)
    {
      var anode = nodeModel.Node;
      var name = anode.ShortName;
      var node = graph.AddNode(name);
      var meta = node.Metadata;
      meta.SetProperty("version", anode.AssemblyName.Version?.ToString());
      meta.SetProperty("fullname", anode.FullName);
      meta.SetProperty("file", anode.FileName);
      meta.SetProperty("module", anode.Module);
      foreach(var tag in anode.Tags)
      {
        var parts = tag.Split("::", 2);
        if(parts.Length == 2)
        {
          meta.AddTag(parts[0], parts[1]);
        }
        else
        {
          meta.AddTag(tag);
        }
      }
    }
    // second pass
    foreach(var nodeModel in NodeModels.Values)
    {
      var source = graph.Nodes[nodeModel.Node.ShortName];
      foreach(var targetEdge in nodeModel.Targets.Values)
      {
        
        var targetName = targetEdge.Target.Node.ShortName;
        var target = graph.Nodes[targetName];
        var edge = source.Connect(target);
        // no metadata to add yet
      }
    }
    return graph;
  }
}
