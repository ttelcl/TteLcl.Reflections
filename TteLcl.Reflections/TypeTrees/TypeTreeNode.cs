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
/// A node in the description of a composite type, such as generic types
/// </summary>
public class TypeTreeNode
{
  /// <summary>
  /// Create a new TypeTreeNode
  /// </summary>
  public TypeTreeNode(
    TypeNodeMap host, Type type)
  {
    Arguments = [];
    if(type.IsGenericParameter)
    {
      // There is no TypeNode related to this tree node, just a name for a type parameter and no deeper arguments
      TypeKey = 0L;
      Label = type.ToString();
    }
    else if(type.IsGenericType)
    {
      // further breakdown possible
      var deftype = type.GetGenericTypeDefinition();
      var node = host[deftype];
      TypeKey = node.Id;
      Label = deftype.ToString();
      foreach(var typeArgument in type.GetGenericArguments())
      {
        Arguments.Add(new TypeTreeNode(host, typeArgument));
      }
    }
    else
    {
      // plain type. Arrays are not yet handled, so they end up here too
      var node = host[type];
      TypeKey = node.Id;
      Label = type.ToString();
    }
  }

  /// <summary>
  /// The label used for this node. If <see cref="TypeKey"/> is non-null,
  /// this matches the referenced type's label.
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// The key of the concrete type's definition (0 for placeholder type arguments)
  /// </summary>
  [JsonProperty("typekey")]
  public long TypeKey { get; }

  /// <summary>
  /// The type arguments (empty for leaf types)
  /// </summary>
  [JsonProperty("arguments")]
  public List<TypeTreeNode> Arguments { get; }

}
