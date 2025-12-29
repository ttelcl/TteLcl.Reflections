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
  public TypeArgumentInfo(TypeNodeMap host, Type typeArgument, int index)
  {
    if(typeArgument.IsGenericParameter)
    {
      TypeKey = null;
      TypeId = 0L;
      Label = typeArgument.ToString();
      Index = index;
      // Index = typeArgument.GenericParameterPosition; // Not correct: that only counts unassigned slots
    }
    else
    {
      var node = host.AddNode(typeArgument);
      TypeKey = node.Key;
      TypeId = node.Id;
      Label = typeArgument.ToString();
      Index = index;
    }
  }

  /// <summary>
  /// Create a new <see cref="TypeArgumentInfo"/> if <paramref name="typeArgument"/>
  /// is not null
  /// </summary>
  /// <returns></returns>
  public static TypeArgumentInfo? FromType(TypeNodeMap host, Type? typeArgument, int index=0)
  {
    if(typeArgument != null)
    {
      return new TypeArgumentInfo(host, typeArgument, index);
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

  /// <summary>
  /// The index of the type argument in the list of all type arguments.
  /// </summary>
  [JsonProperty("index")]
  public int Index { get; }
}
