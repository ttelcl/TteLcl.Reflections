using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs;

/// <summary>
/// An object that has an associated <see cref="TteLcl.Graphs.Metadata"/> instance.
/// </summary>
public interface IHasMetadata
{
  /// <summary>
  /// The <see cref="TteLcl.Graphs.Metadata"/> associated with this object
  /// </summary>
  Metadata Metadata { get; }
}
