/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TteLcl.Reflections.TypeTrees;

/// <summary>
/// A JSON serializable nonrecursive model for <see cref="TypeNode"/>
/// </summary>
public class TypeNodeModel: TypeNodeReference
{
  /// <summary>
  /// Create a new TypeNodeModel
  /// </summary>
  [JsonConstructor()]
  public TypeNodeModel(
    string? name,
    string? assembly,
    string key)
    : base(name, assembly, key)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Create a new TypeNodeModel
  /// </summary>
  public TypeNodeModel(TypeNode node)
    : base(node.TargetType)
  {
    var type = node.TargetType;
  }

}
