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
    Components = components;
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
    }
  }

  /// <summary>
  /// The components as a topologically ordered list
  /// </summary>
  public IReadOnlyList<StronglyConnectedComponent> Components { get; }
}
