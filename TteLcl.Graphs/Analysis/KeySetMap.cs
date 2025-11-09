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
/// Alias for <see cref="KeyMap{KeySet}"/> with a collection of related
/// functions. A mapping of strings to sets of strings (where both the mapping
/// and sets are case insensitive).
/// </summary>
public class KeySetMap: KeyMap<KeySet>
{
  /// <summary>
  /// Create a new KeySetMap
  /// </summary>
  public KeySetMap()
    : base()
  {
  }

}