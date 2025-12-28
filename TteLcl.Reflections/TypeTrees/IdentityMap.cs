/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.TypeTrees;

/// <summary>
/// Description of IdentityMap
/// </summary>
public class IdentityMap<T> where T : class
{
  private int _counter = 1;
  private readonly Dictionary<T, int> _mapping;

  /// <summary>
  /// Create a new IdentityMap
  /// </summary>
  public IdentityMap()
  {
    _mapping = [];
  }

  /// <summary>
  /// Get the id for the given key, assigning one if none was assigned yet.
  /// Passing null returns 0; any other value returns a positive number.
  /// </summary>
  /// <param name="key"></param>
  /// <returns></returns>
  public int GetId(T? key)
  {
    if(key == null)
    {
      return 0;
    }
    if(!_mapping.TryGetValue(key, out var id))
    {
      id = _counter++;
      _mapping[key] = id;
    }
    return id;
  }

}
