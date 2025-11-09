using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// Describes a dependency / edge between two <see cref="AssemblyNode"/>s.
/// </summary>
public class AssemblyEdge
{
  /// <summary>
  /// Create a new <see cref="AssemblyEdge"/>
  /// </summary>
  /// <param name="source">
  /// The key of the source / dependent node
  /// </param>
  /// <param name="target">
  /// The key of the target / dependency node
  /// </param>
  /// <param name="tags">
  /// A collection of tags associated with the edge
  /// </param>
  [JsonConstructor]
  public AssemblyEdge(
    string source,
    string target,
    IEnumerable<string> tags)
  {
    DependentKey = source;
    DependencyKey = target;
    Tags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// The node key of the dependent assembly ("source")
  /// </summary>
  [JsonProperty("source")]
  public string DependentKey { get; }

  /// <summary>
  /// The node key of the dependency ("target")
  /// </summary>
  [JsonProperty("target")]
  public string DependencyKey { get; }

  /// <summary>
  /// Tags associated with this edge
  /// </summary>
  [JsonProperty("tags")]
  public HashSet<string> Tags { get; }
}
