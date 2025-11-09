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
/// Provides a read-only view on a <see cref="KeySetMap"/>
/// (but also exposes that underlying object as <see cref="Host"/>.
/// Many operations in <see cref="GraphAnalyzer"/> operate on this type.
/// </summary>
public class KeySetMapView: MapView<string, KeySet, string>, IReadOnlyDictionary<string, IReadOnlySet<string>>
{
  /// <summary>
  /// Create a new KeySetMap
  /// </summary>
  public KeySetMapView(KeySetMap host)
    : base(host)
  {
    Host = host;
  }

  /// <summary>
  /// The underlying (mutable) host map
  /// </summary>
  public KeySetMap Host { get; }

  /// <summary>
  /// Map each key in <paramref name="seed"/> to its set in this <see cref="KeySetMapView"/>,
  /// then insert each key in that set into <paramref name="target"/>. Missing seed keys
  /// are ignored.
  /// </summary>
  /// <param name="seed"></param>
  /// <param name="target"></param>
  public void ProjectInto(IEnumerable<string> seed, ISet<string> target)
  {
    foreach(var seedKey in seed)
    {
      if(TryGetValue(seedKey, out var keys))
      {
        target.UnionWith(keys);
      }
    }
  }

  /// <summary>
  /// Map each key in <paramref name="seed"/> to its set (ignoring missing keys)
  /// and return the union of those sets
  /// </summary>
  /// <param name="seed"></param>
  /// <returns></returns>
  public KeySet Project(IEnumerable<string> seed)
  {
    var result = new KeySet();
    ProjectInto(seed, result);
    return result;
  }

  /// <summary>
  /// Return a new <see cref="KeySetMap"/> that projects each entry in
  /// <paramref name="seedMap"/> using <see cref="Project(IEnumerable{string})"/>.
  /// </summary>
  /// <param name="seedMap"></param>
  /// <returns></returns>
  public KeySetMap Project(
    KeySetMapView seedMap)
  {
    var result = new KeySetMap();
    foreach(var seedKvp in seedMap)
    {
      var projection = Project(seedKvp.Value);
      result.Add(seedKvp.Key, projection);
    }
    return result;
  }

  /// <summary>
  /// Returns the subset of <paramref name="keys"/> that do <i>not</i> appear in
  /// the projection of <paramref name="seeds"/> (without ever materializing that projection)
  /// </summary>
  /// <param name="keys"></param>
  /// <param name="seeds"></param>
  /// <returns></returns>
  public KeySet NotInProjection(IEnumerable<string> keys, IEnumerable<string> seeds)
  {
    var result = new KeySet();
    foreach(var key in keys)
    {
      var found = false;
      foreach(var seed in seeds)
      {
        if(TryGetValue(seed, out var seedProjection))
        {
          if(seedProjection.Contains(key))
          {
            found = true;
            break;
          }
        }
      }
      if(!found)
      {
        result.Add(key);
      }
    }
    return result;
  }

  /// <summary>
  /// Like <see cref="NotInProjection(IEnumerable{string}, IEnumerable{string})"/>, but
  /// both arguments are the same.
  /// </summary>
  /// <param name="keysAndSeeds"></param>
  /// <returns></returns>
  public KeySet NotInSelfProjection(IEnumerable<string> keysAndSeeds)
  {
    return NotInProjection(keysAndSeeds, keysAndSeeds);
  }

  /// <summary>
  /// Returns a new <see cref="KeySetMap"/> containing the data in
  /// <paramref name="seedMap"/>, but only those keys that do not appear
  /// in the projection of the set using this <see cref="KeySetMapView"/>.
  /// This method avoids actually ever creating those projections.
  /// </summary>
  /// <param name="seedMap"></param>
  /// <returns></returns>
  public KeySetMap NotInSelfProjection(KeySetMapView seedMap)
  {
    var result = new KeySetMap();
    foreach(var kvp in seedMap)
    {
      result.Add(kvp.Key, NotInSelfProjection(kvp.Value));
    }
    return result;
  }

}
