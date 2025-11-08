/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TteLcl.Graphs;

/// <summary>
/// An object identified by a string key
/// </summary>
public interface IHasKey
{
  /// <summary>
  /// The key identifying this object (or referencing an object)
  /// </summary>
  string Key { get; }
}

