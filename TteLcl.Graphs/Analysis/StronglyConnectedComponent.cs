using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// One single Strongly Connected Component in a <see cref="StronglyConnectedComponentsResult"/>,
/// describing a set of vertexes (nodes) and assigning a name
/// </summary>
public class StronglyConnectedComponent
{
  /// <summary>
  /// Create a new <see cref="StronglyConnectedComponent"/> instance
  /// </summary>
  /// <param name="nodes"></param>
  /// <param name="name"></param>
  /// <param name="index"></param>
  [JsonConstructor]
  public StronglyConnectedComponent(
    IReadOnlySet<string> nodes,
    string name,
    int index)
  {
    Nodes = nodes;
    Name = name;
    Index = index;
  }

  /// <summary>
  /// The 0-based index
  /// </summary>
  [JsonProperty("index")]
  public int Index { get; }

  /// <summary>
  /// A name for this component
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

  /// <summary>
  /// The nodes in this strongly connected component
  /// </summary>
  [JsonProperty("nodes")]
  public IReadOnlySet<string> Nodes { get; }
}
