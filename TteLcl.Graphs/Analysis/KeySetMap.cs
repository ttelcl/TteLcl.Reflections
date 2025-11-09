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
public class KeySetMap: KeyMap<KeySet>, IDictionary<string, KeySet>, IReadOnlyDictionary<string, KeySet>
{
  /// <summary>
  /// Create a new empty <see cref="KeySetMap"/>
  /// </summary>
  public KeySetMap()
    : base()
  {
  }

  /// <summary>
  /// Cloning constructor, cloning another <see cref="KeySetMap"/> (or its more
  /// general half-read-only interface)
  /// </summary>
  /// <param name="map">
  /// The map to clone
  /// </param>
  public KeySetMap(IReadOnlyDictionary<string, KeySet> map)
    : this()
  {
    foreach(var kvp in map)
    {
      var valueClone = new KeySet(kvp.Value);
      Add(kvp.Key, valueClone);
    }
  }

  /// <summary>
  /// Cloning constructor, cloning a <see cref="KeySetMapView"/> (or
  /// its mpore general doubly-read-only interface)
  /// </summary>
  /// <param name="map"></param>
  public KeySetMap(IReadOnlyDictionary<string, IReadOnlySet<string>> map)
    : this()
  {
    foreach(var kvp in map)
    {
      var valueClone = new KeySet(kvp.Value);
      Add(kvp.Key, valueClone);
    }
  }

  /// <summary>
  /// Add each value set in <paramref name="other"/> to the corresponding
  /// item in this <see cref="KeySetMap"/> (adding missing items if necessary)
  /// </summary>
  /// <param name="other">
  /// A mapping of strings to <see cref="KeySet"/>s, e.g. another <see cref="KeySetMap"/>.
  /// </param>
  public void UnionWith(IReadOnlyDictionary<string, KeySet> other)
  {
    foreach(var kvp in other)
    {
      if(!TryGetValue(kvp.Key, out var s))
      {
        s = new KeySet(kvp.Value);
        Add(kvp.Key, s);
      }
      else
      {
        s.UnionWith(kvp.Value);
      }
    }
  }

  /// <summary>
  /// Add each value set in <paramref name="other"/> to the corresponding
  /// item in this <see cref="KeySetMap"/> (adding missing items if necessary)
  /// </summary>
  /// <param name="other">
  /// A mapping of strings to a readonly set of strings, e.g. a <see cref="KeySetMapView"/>.
  /// </param>
  public void UnionWith(IReadOnlyDictionary<string, IReadOnlySet<string>> other)
  {
    foreach(var kvp in other)
    {
      if(!TryGetValue(kvp.Key, out var s))
      {
        s = new KeySet(kvp.Value);
        Add(kvp.Key, s);
      }
      else
      {
        s.UnionWith(kvp.Value);
      }
    }
  }

}
