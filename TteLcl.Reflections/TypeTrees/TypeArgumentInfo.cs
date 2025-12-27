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
/// A serializable description of a type argument
/// </summary>
public class TypeArgumentInfo
{
  /// <summary>
  /// Create a new TypeArgumentInfo
  /// </summary>
  public TypeArgumentInfo(TypeNodeMap host, Type typeArgument)
  {
    if(typeArgument.IsGenericParameter)
    {
      TypeKey = null;
      Label = typeArgument.ToString();
    }
    else
    {
      var node = host.AddNode(typeArgument);
      TypeKey = node.Key;
      Label = typeArgument.ToString();
    }
  }

  /// <summary>
  /// The label used for this type argument. If <see cref="TypeKey"/> is non-null,
  /// this matches the referenced type's label.
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// The key of the concrete type (null for placeholder type arguments)
  /// </summary>
  [JsonProperty("typekey")]
  public string? TypeKey { get; }

}
