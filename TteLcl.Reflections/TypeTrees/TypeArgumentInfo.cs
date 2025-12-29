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
/// A serializable description of a type argument or other type that may be
/// a generic type argument.
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
      TypeId = 0L;
      Label = typeArgument.ToString();
    }
    else
    {
      var node = host.AddNode(typeArgument);
      TypeKey = node.Key;
      TypeId = node.Id;
      Label = typeArgument.ToString();
    }
  }

  /// <summary>
  /// Create a new <see cref="TypeArgumentInfo"/> if <paramref name="typeArgument"/>
  /// is not null
  /// </summary>
  /// <param name="host"></param>
  /// <param name="typeArgument"></param>
  /// <returns></returns>
  public static TypeArgumentInfo? FromType(TypeNodeMap host, Type? typeArgument)
  {
    if(typeArgument != null)
    {
      return new TypeArgumentInfo(host, typeArgument);
    }
    return null;
  }

  /// <summary>
  /// The label used for this type argument. If <see cref="TypeKey"/> is non-null,
  /// this matches the referenced type's label.
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// The ID of the concrete type (0 for placeholder type arguments)
  /// </summary>
  [JsonProperty("typeid")]
  public long TypeId { get; }

  /// <summary>
  /// The key of the concrete type (null for placeholder type arguments)
  /// </summary>
  [JsonProperty("typekey")]
  public string? TypeKey { get; }
}
