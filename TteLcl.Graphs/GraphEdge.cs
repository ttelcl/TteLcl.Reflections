using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs;

/// <summary>
/// A generic edge in a <see cref="Graph"/>
/// </summary>
public class GraphEdge: IHasMetadata
{
  /// <summary>
  /// Create a new <see cref="GraphEdge"/>.
  /// </summary>
  internal GraphEdge(GraphNode source, GraphNode target, Metadata? metadata = null)
  {
    Metadata = new Metadata();
    Source=source;
    Target=target;
    if(metadata != null)
    {
      Metadata.Import(metadata);
    }
  }

  /// <inheritdoc/>
  public Metadata Metadata { get; }

  /// <summary>
  /// The source node
  /// </summary>
  public GraphNode Source { get; }

  /// <summary>
  /// The target node
  /// </summary>
  public GraphNode Target { get; }

  /// <summary>
  /// Given one endpoint of this edge, return the other one
  /// </summary>
  public GraphNode OtherEnd(GraphNode endpoint)
  {
    if(Object.ReferenceEquals(endpoint, Source))
    {
      return Target;
    }
    if(Object.ReferenceEquals(endpoint, Target))
    {
      return Source;
    }
    throw new ArgumentException(
      $"Invalid endpoint {endpoint.Key}. Not part of edge [{Source.Key} -> {Target.Key}]");
  }

  /// <summary>
  /// Disconnect this edge. Returns true on success, false if it wasn't connected.
  /// </summary>
  /// <returns></returns>
  public bool Disconnect()
  {
    return Source.DisconnectTarget(Target.Key) == this;
  }
}
