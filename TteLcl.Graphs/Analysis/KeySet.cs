/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// A case insensitive set of strings
/// </summary>
public class KeySet: ISet<string>, IReadOnlySet<string>
{
  private readonly HashSet<string> _set;

  /// <summary>
  /// Create a new empty <see cref="KeySet"/>
  /// </summary>
  public KeySet()
  {
    _set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Create a new <see cref="KeySet"/> initialized with the given
  /// <paramref name="keys"/>
  /// </summary>
  /// <param name="keys">
  /// The entries to add to this set
  /// </param>
  public KeySet(IEnumerable<string> keys)
  {
    _set = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// True if this set contains the key of the provided object
  /// </summary>
  /// <param name="ihk"></param>
  /// <returns></returns>
  public bool Contains(IHasKey ihk)
  {
    return _set.Contains(ihk.Key);
  }

  /// <summary>
  /// Add the key of the provided object
  /// </summary>
  /// <param name="ihk"></param>
  /// <returns></returns>
  public bool Add(IHasKey ihk)
  {
    return _set.Add(ihk.Key);
  }

  /// <inheritdoc/>
  public bool Add(string item)
  {
    return _set.Add(item);
  }

  /// <inheritdoc/>
  public void ExceptWith(IEnumerable<string> other)
  {
    _set.ExceptWith(other);
  }

  /// <inheritdoc/>
  public void IntersectWith(IEnumerable<string> other)
  {
    _set.IntersectWith(other);
  }

  /// <inheritdoc/>
  public bool IsProperSubsetOf(IEnumerable<string> other)
  {
    return _set.IsProperSubsetOf(other);
  }

  /// <inheritdoc/>
  public bool IsProperSupersetOf(IEnumerable<string> other)
  {
    return _set.IsProperSupersetOf(other);
  }

  /// <inheritdoc/>
  public bool IsSubsetOf(IEnumerable<string> other)
  {
    return _set.IsSubsetOf(other);
  }

  /// <inheritdoc/>
  public bool IsSupersetOf(IEnumerable<string> other)
  {
    return _set.IsSupersetOf(other);
  }

  /// <inheritdoc/>
  public bool Overlaps(IEnumerable<string> other)
  {
    return _set.Overlaps(other);
  }

  /// <inheritdoc/>
  public bool SetEquals(IEnumerable<string> other)
  {
    return _set.SetEquals(other);
  }

  /// <inheritdoc/>
  public void SymmetricExceptWith(IEnumerable<string> other)
  {
    _set.SymmetricExceptWith(other);
  }

  /// <inheritdoc/>
  public void UnionWith(IEnumerable<string> other)
  {
    _set.UnionWith(other);
  }

  /// <inheritdoc/>
  void ICollection<string>.Add(string item)
  {
    Add(item);
  }

  /// <inheritdoc/>
  public void Clear()
  {
    _set.Clear();
  }

  /// <inheritdoc/>
  public bool Contains(string item)
  {
    return _set.Contains(item);
  }

  /// <inheritdoc/>
  public void CopyTo(string[] array, int arrayIndex)
  {
    _set.CopyTo(array, arrayIndex);
  }

  /// <inheritdoc/>
  public bool Remove(string item)
  {
    return _set.Remove(item);
  }

  /// <inheritdoc/>
  public int Count => _set.Count;

  /// <inheritdoc/>
  public bool IsReadOnly => false;

  /// <inheritdoc/>
  public IEnumerator<string> GetEnumerator()
  {
    return _set.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

}
