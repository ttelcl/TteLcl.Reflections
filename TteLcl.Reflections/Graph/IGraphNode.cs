using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// Content for nodes in a graph. This does not include relations
/// between nodes
/// </summary>
public interface IGraphNode
{
  /// <summary>
  /// A label for this node
  /// </summary>
  string Label { get; }

  /// <summary>
  /// The key uniquely identifying this node
  /// </summary>
  string Key { get; }

  /// <summary>
  /// Tags associated with this node
  /// </summary>
  IReadOnlySet<string> Tags { get; }
}
