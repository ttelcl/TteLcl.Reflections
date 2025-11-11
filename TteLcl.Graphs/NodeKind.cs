using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs;

/// <summary>
/// Categories for nodes based on incoming and outgoing edges
/// </summary>
public enum NodeKind
{
  /// <summary>
  /// A node that is neither a seed nor a sink: it has both incoming and outgoing edges
  /// </summary>
  Other = 0,

  /// <summary>
  /// A true seed node: a node that has no incoming edges but does have outgoing edges
  /// </summary>
  Seed = 1,

  /// <summary>
  /// A true sink node: a node that has incoming edges but no outgoing edges
  /// </summary>
  Sink = 2,

  /// <summary>
  /// A node with no connected edges at all. In a way both a seed and a sink.
  /// </summary>
  Loose = 3,
}
