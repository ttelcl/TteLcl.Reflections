/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// Provides a read-only view on a <see cref="KeyMap{KeySet}"/>
/// (but also exposes that underlying object as <see cref="Host"/>.
/// Many operations in <see cref="GraphAnalyzer"/> operate on this type.
/// </summary>
public class KeySetMapView: MapView<string, KeySet, string>, IReadOnlyDictionary<string, IReadOnlySet<string>>
{
  /// <summary>
  /// Create a new KeySetMap
  /// </summary>
  public KeySetMapView(KeyMap<KeySet> host)
    : base(host)
  {
    Host = host;
  }

  /// <summary>
  /// The underlying (mutable) host map
  /// </summary>
  public KeyMap<KeySet> Host { get; }
}
