using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
  /// <param name="components"></param>
  /// <param name="name"></param>
  /// <param name="index"></param>
  public StronglyConnectedComponent(
    IReadOnlySet<string> components,
    string name,
    int index)
  {
    Components = components;
    Name = name;
    Index = index;
  }

  /// <summary>
  /// The components in this strongly connected component
  /// </summary>
  public IReadOnlySet<string> Components { get; }

  /// <summary>
  /// A name for this component
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The 0-based index
  /// </summary>
  public int Index { get; }
}
