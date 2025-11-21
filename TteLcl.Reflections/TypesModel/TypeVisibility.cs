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
