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
/// A case insensitive mapping of strings to <typeparamref name="T"/>.
/// </summary>
public class KeyMap<T>: IDictionary<string, T>, IReadOnlyDictionary<string, T>
{
  private readonly Dictionary<string, T> _map;

  /// <summary>
  /// Create a new KeyMap
  /// </summary>
  public KeyMap()
  {
    _map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
  }

  /// <inheritdoc/>
  public void Add(string key, T value)
  {
    _map.Add(key, value);
  }

  /// <inheritdoc/>
  public bool ContainsKey(string key)
  {
    return _map.ContainsKey(key);
  }

  /// <inheritdoc/>
  public bool Remove(string key)
  {
    return _map.Remove(key);
  }

  /// <inheritdoc/>
  public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
  {
    return _map.TryGetValue(key, out value);
  }

  /// <inheritdoc/>
  public T this[string key] {
    get => _map[key];
    set => _map[key]=value;
  }

  /// <inheritdoc/>
  public ICollection<string> Keys => _map.Keys;

  IEnumerable<string> IReadOnlyDictionary<string, T>.Keys => ((IReadOnlyDictionary<string, T>)_map).Keys;

  /// <inheritdoc/>
  public ICollection<T> Values => _map.Values;

  IEnumerable<T> IReadOnlyDictionary<string, T>.Values => ((IReadOnlyDictionary<string, T>)_map).Values;

  /// <inheritdoc/>
  public void Add(KeyValuePair<string, T> item)
  {
    Add(item.Key, item.Value);
  }

  /// <inheritdoc/>
  public void Clear()
  {
    _map.Clear();
  }

  /// <inheritdoc/>
  public bool Contains(KeyValuePair<string, T> item)
  {
    return _map.Contains(item);
  }

  /// <inheritdoc/>
  public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
  {
    ((ICollection<KeyValuePair<string, T>>)_map).CopyTo(array, arrayIndex);
  }

  /// <inheritdoc/>
  public bool Remove(KeyValuePair<string, T> item)
  {
    return _map.Remove(item.Key);
  }

  /// <inheritdoc/>
  public int Count => _map.Count;

  /// <inheritdoc/>
  public bool IsReadOnly => false;

  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
  {
    return _map.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return ((IEnumerable)_map).GetEnumerator();
  }
}
