using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Newtonsoft.Json.Converters;

namespace TteLcl.Reflections.TypesModel;

/// <summary>
/// Type visibility codes (a subset of <see cref="TypeAttributes"/>, without Flags semantics)
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TypeVisibility
{
  /// <summary>
  /// private and not nested
  /// </summary>
  [EnumMember(Value = "private")] 
  Private = TypeAttributes.NotPublic,

  /// <summary>
  /// public and not nested
  /// </summary>
  [EnumMember(Value = "public")]
  Public = TypeAttributes.Public,

  /// <summary>
  /// Nested Public
  /// </summary>
  [EnumMember(Value = "nested-public")]
  NestedPublic = TypeAttributes.NestedPublic,

  /// <summary>
  /// Nested Private
  /// </summary>
  [EnumMember(Value = "nested-private")]
  NestedPrivate = TypeAttributes.NestedPrivate,

  /// <summary>
  /// Nested Protected
  /// </summary>
  [EnumMember(Value = "nested-family")]
  NestedFamily = TypeAttributes.NestedFamily,

  /// <summary>
  /// Nested Internal
  /// </summary>
  [EnumMember(Value = "nested-assembly")]
  NestedAssembly = TypeAttributes.NestedAssembly,

  /// <summary>
  /// Nested Protected AND Internal (visible to derived types in the same assembly)
  /// No C# equivalent (last time I checked, at least)
  /// </summary>
  [EnumMember(Value = "nested-family-and-assembly")]
  NestedFamilyAndAssembly = TypeAttributes.NestedFamANDAssem,

  /// <summary>
  /// Nested Protected OR Internal (visible to derived types in any assembly and any types in the same assembly)
  /// A.k.a "protected internal" in C#
  /// </summary>
  [EnumMember(Value = "nested-family-or-assembly")]
  NestedFamilyOrAssembly = TypeAttributes.NestedFamORAssem,
}

/// <summary>
/// Static methods and extensions related to <see cref="TypeVisibility"/>
/// </summary>
public static class TypeVisibilityUtils
{
  /// <summary>
  /// Convert <see cref="TypeAttributes"/> to <see cref="TypeVisibility"/>
  /// </summary>
  public static TypeVisibility AsTypeVisibility(this TypeAttributes attributes)
  {
    return (TypeVisibility)(int)(attributes & TypeAttributes.VisibilityMask);
  }

  /// <summary>
  /// Convert the attributes of a <see cref="Type"/> to <see cref="TypeVisibility"/>
  /// </summary>
  public static TypeVisibility AsTypeVisibility(this Type type)
  {
    return (TypeVisibility)(int)(type.Attributes & TypeAttributes.VisibilityMask);
  }

  /// <summary>
  /// Assign a sort order to a visibilty, aiming to give more visibile ones
  /// a lower result.
  /// </summary>
  /// <param name="v"></param>
  /// <returns></returns>
  public static int RankOrder(this TypeVisibility v)
  {
    return v switch {
      TypeVisibility.Public => 0,
      TypeVisibility.NestedPublic => 1,
      TypeVisibility.NestedFamilyOrAssembly => 2,
      TypeVisibility.NestedAssembly => 3,
      TypeVisibility.NestedFamily => 4,
      TypeVisibility.NestedFamilyAndAssembly => 5,
      TypeVisibility.Private => 6,
      TypeVisibility.NestedPrivate => 7,
      _ => 1000,
    };
  }

}
