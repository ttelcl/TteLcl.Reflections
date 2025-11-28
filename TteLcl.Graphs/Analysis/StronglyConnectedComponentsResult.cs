using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// Describes the result of a strongly connected components analysis of a graph
/// </summary>
public class StronglyConnectedComponentsResult
{

  /// <summary>
  /// Create a new <see cref="StronglyConnectedComponentsResult"/>
  /// </summary>
  /// <param name="result">
  /// The result of the raw strongly connected component analysis,
  /// in the form of a list of sets of node IDs (vertex IDs), ordered
  /// in a forward topological sort order (that is: already reversed).
  /// </param>
  /// <param name="namePrefix">
  /// A prefix used in generating SCC names, or null to generate names
  /// based on a randomly picked node in the SCC, with a suffix if
  /// there are multiple nodes
  /// </param>
  public StronglyConnectedComponentsResult(
    List<KeySet> result,
    string? namePrefix = "SCC-")
  {
    var numberFormat = "D3";
    if(result.Count > 9999)
    {
      numberFormat = "D5";
    }
    else if(result.Count > 999)
    {
      numberFormat = "D4";
    }
    var components = new List<StronglyConnectedComponent>();
    var componentsByName = new KeyMap<StronglyConnectedComponent>();
    var componentForNode = new KeyMap<StronglyConnectedComponent>();
    Components = components;
    ComponentsByName = componentsByName;
    ComponentForNode = componentForNode;
    for(var i = 0; i < result.Count; i++)
    {
      var ks = result[i];
      string name;
      if(namePrefix == null)
      {
        if(ks.Count == 1)
        {
          name = ks.First();
        }
        else
        {
          name = ks.First() + $"+{ks.Count-1}";
        }
      }
      else
      {
        name = namePrefix + i.ToString(numberFormat);
      }
      var component = new StronglyConnectedComponent(ks, name, i);
      components.Add(component);
      componentsByName.Add(name, component);
      foreach(var node in ks)
      {
        componentForNode.Add(node, component);
      }
    }
  }

  /// <summary>
  /// The components as a topologically ordered list
  /// </summary>
  public IReadOnlyList<StronglyConnectedComponent> Components { get; }

  /// <summary>
  /// The components indexed by component name
  /// </summary>
  public IReadOnlyDictionary<string, StronglyConnectedComponent> ComponentsByName { get; }

  /// <summary>
  /// A mapping from node names to components
  /// </summary>
  public IReadOnlyDictionary<string, StronglyConnectedComponent> ComponentForNode { get; }

  /// <summary>
  /// Create the Strongly Connected Component graph based on this SCC decomposition
  /// of the given <paramref name="source"/> graph
  /// </summary>
  /// <param name="source"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public Graph ComponentGraph(Graph source)
  {
    var componentGraph = new Graph();
    foreach(var component in Components)
    {
      // Start by creating disconnected nodes
      var cnode = componentGraph.AddNode(component.Name);
      cnode.Metadata.SetProperty("sccindex", component.Index.ToString());
    }
    foreach(var component in Components)
    {
      // Then add edges
      foreach(var sourceName in component.Components)
      {
        if(!source.Nodes.TryGetValue(sourceName, out var sourceNode))
        {
          throw new InvalidOperationException(
            $"Incompatible graph: node '{sourceName}' is missing");
        }
        foreach(var targetName in sourceNode.Targets.Keys)
        {
          // Allow the source graph to be bigger than this SCC graph: ignore missing target nodes
          if(ComponentForNode.TryGetValue(targetName, out var targetComponent))
          {
            if(component.Name != targetComponent.Name)
            {
              componentGraph.ConnectOrMergeEdge(component.Name, targetComponent.Name);
            }
          }
        }
      }
    }
    return componentGraph;
  }
}
