/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// Wraps a (potentially mutable) dictionary mapping keys to (potentially mutable)
/// sets to expose it as a read-only dictionary mapping those keys to read-only sets
/// </summary>
/// <typeparam name="K">
/// The key type
/// </typeparam>
/// <typeparam name="V">
/// The type implementing the "set" values of the host dictionary being wrapped.
/// This must implement <see cref="IReadOnlySet{T}"/> but may actually be a mutable
/// supertype.
/// </typeparam>
/// <typeparam name="T">
/// The type of the items in the sets <typeparamref name="V"/>.
/// </typeparam>
public class MapView<K, V, T>: IReadOnlyDictionary<K, IReadOnlySet<T>>
  where V: IReadOnlySet<T>
{
  private readonly IReadOnlyDictionary<K,V> _host;

  /// <summary>
  /// Create a new <see cref="MapView{K,V,T}"/> wrapping <paramref name="host"/>
  /// </summary>
  public MapView(IReadOnlyDictionary<K,V> host)
  {
    _host = host;
  }

  /// <inheritdoc/>
  public bool ContainsKey(K key)
  {
    return _host.ContainsKey(key);
  }

  /// <inheritdoc/>
  public bool TryGetValue(K key, [MaybeNullWhen(false)] out IReadOnlySet<T> value)
  {
    if(_host.TryGetValue(key, out var v))
    {
      value = v;
      return true;
    }
    value = null;
    return false;
  }

  /// <inheritdoc/>
  public IReadOnlySet<T> this[K key] { get => _host[key]; }

  /// <inheritdoc/>
  public IEnumerable<K> Keys => _host.Keys;

  /// <inheritdoc/>
  public IEnumerable<IReadOnlySet<T>> Values => _host.Values.Select(v => (IReadOnlySet<T>)v);

  /// <inheritdoc/>
  public int Count => _host.Count;

  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<K, IReadOnlySet<T>>> GetEnumerator()
  {
    foreach(var kvp in _host)
    {
      yield return new KeyValuePair<K, IReadOnlySet<T>>(kvp.Key, kvp.Value);
    }
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
