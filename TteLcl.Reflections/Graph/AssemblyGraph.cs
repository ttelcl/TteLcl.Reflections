using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Newtonsoft.Json;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// Describes a collection of assemblies and their dependencies
/// </summary>
public class AssemblyGraph
{
  private readonly Dictionary<string, AssemblyNode> _nodes;

  /// <summary>
  /// Create a new <see cref="AssemblyGraph"/>.
  /// This is the Json deserialization constructor.
  /// </summary>
  /// <param name="nodes"></param>
  /// <exception cref="InvalidOperationException"></exception>
  [JsonConstructor]
  public AssemblyGraph(
    IEnumerable<AssemblyNode> nodes)
  {
    _nodes = new Dictionary<string, AssemblyNode>();
    foreach(var node in nodes)
    {
      var key = node.Key;
      if(_nodes.TryGetValue(key, out var existingNode))
      {
        throw new InvalidOperationException(
          $"Duplicate node '{key}'");
      }
      _nodes.Add(key, node);
    }
  }

  /// <summary>
  /// The collection of nodes. This is just a view on the values in
  /// <see cref="NodeMap"/>.
  /// </summary>
  [JsonProperty("nodes")]
  public IReadOnlyCollection<AssemblyNode> Nodes => _nodes.Values;

  /// <summary>
  /// The mapping of full assembly names to assembly nodes
  /// </summary>
  [JsonIgnore]
  public IReadOnlyDictionary<string, AssemblyNode> NodeMap => _nodes;

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
    _nodes[node.Key] = node;
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


}
