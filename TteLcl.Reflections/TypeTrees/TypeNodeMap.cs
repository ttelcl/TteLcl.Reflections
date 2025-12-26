/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.TypeTrees;

/// <summary>
/// Index for type nodes
/// </summary>
public class TypeNodeMap
{
  private readonly Dictionary<Type, TypeNode> _map;
  private readonly Queue<TypeNode> _pendingTypes;

  /// <summary>
  /// Create a new TypeNodeMap
  /// </summary>
  public TypeNodeMap(TypeEdgeKind relations)
  {
    _map = new Dictionary<Type, TypeNode>();
    _pendingTypes = new Queue<TypeNode>();
    AnalysisRelations = relations;
  }

  /// <summary>
  /// Get the type node for the given type, creating a new node if needed.
  /// </summary>
  /// <param name="t"></param>
  /// <returns></returns>
  public TypeNode this[Type t] {
    get {
      if(!_map.TryGetValue(t, out var node))
      {
        node = new TypeNode(t, this);
        _map[t] = node;
        _pendingTypes.Enqueue(node);
      }
      return node;
    }
  }

  /// <summary>
  /// The kinds of type relations to include beyond the basics
  /// </summary>
  public TypeEdgeKind AnalysisRelations { get; }

  /// <summary>
  /// Load the next pending <see cref="TypeNode"/>.
  /// Returns the node that was loaded or null if theren are more nodes to load.
  /// Alternatively call <see cref="LoadNodes()"/> to repeatedly call this method
  /// until done.
  /// </summary>
  /// <returns></returns>
  public TypeNode? LoadNext()
  {
    if(_pendingTypes.TryDequeue(out var nextNode))
    {
      nextNode.Load();
    }
    return nextNode;
  }

  /// <summary>
  /// The number of <see cref="TypeNode"/>s remaining to load.
  /// </summary>
  public int PendingNodeCount => _pendingTypes.Count;

  /// <summary>
  /// The total number of <see cref="TypeNode"/>s in this map (both loaded and unloaded)
  /// </summary>
  public int NodeCount => _map.Count;

  /// <summary>
  /// Keep loading nodes until there are none left to load
  /// </summary>
  /// <returns>
  /// The sequence of nodes, as they are loaded
  /// </returns>
  public IEnumerable<TypeNode> LoadNodes()
  {
    TypeNode? loadedNode;
    do
    {
      loadedNode = LoadNext();
      if(loadedNode != null)
      {
        yield return loadedNode;
      }
    } while(loadedNode != null);
  }

  /// <summary>
  /// Add a node for the type, or return the existing node.
  /// Functionally equivalent to <see cref="this[Type]"/>.
  /// </summary>
  /// <param name="t"></param>
  /// <returns></returns>
  public TypeNode AddNode(Type t)
  {
    return this[t];
  }

  /// <summary>
  /// Add the <paramref name="type"/> if it is not null and return its <see cref="TypeNode"/>.
  /// Otherwise return null.
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public TypeNode? TryAddNode(Type? type)
  {
    if(type != null)
    {
      return AddNode(type);
    }
    return null;
  }

  /// <summary>
  /// Add all types in the assembly (non-public types included) to this map
  /// </summary>
  /// <param name="assembly"></param>
  public void AddAssembly(Assembly assembly)
  {
    foreach(var t in assembly.GetTypes())
    {
      AddNode(t);
    }
  }

  /// <summary>
  /// Get all nodes in this map
  /// </summary>
  public IReadOnlyCollection<TypeNode> Nodes => _map.Values;

  /// <summary>
  /// Convert all nodes to their model and return a dictionary grouped
  /// by assembly
  /// </summary>
  /// <returns></returns>
  public Dictionary<string, List<TypeNodeModel>> ToAssemblyGroupedModel()
  {
    var list =
      Nodes
      .Select(node => node.ToModel())
      .OrderBy(model => model.AssemblyName ?? "")
      .ThenBy(model => model.Name ?? model.Key)
      .ToList();
    var grouped =
      list.GroupBy(model => model.AssemblyName ?? "");
    return
      grouped.ToDictionary(
        g => g.Key,
        g => g.ToList());
  }
}
