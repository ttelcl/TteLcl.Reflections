using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TteLcl.Reflections.TypesModel;

/// <summary>
/// A Json serializable model of a type
/// </summary>
public class TypeModel
{
  /// <summary>
  /// JSON Deserialization constructor
  /// </summary>
  [JsonConstructor]
  public TypeModel()
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Construct based on a <see cref="Type"/> instance
  /// </summary>
  /// <param name="type"></param>
  public TypeModel(Type type)
  {
    Name = type.Name;
    FullName = type.FullName;
    AssemblyQualifiedName = type.AssemblyQualifiedName;
    Visibility = (TypeVisibility)(int)(type.Attributes & TypeAttributes.VisibilityMask);
    IsAbstract = type.IsAbstract;
    IsSealed = type.IsSealed;
    Kind = Categorize(type);
    Generic = CategorizeGeneric(type);
    var declaringType = type.DeclaringType;
    DeclaringType = declaringType?.AssemblyQualifiedName ?? declaringType?.FullName ?? declaringType?.Name;
    var baseType = type.BaseType;
    BaseType = baseType?.AssemblyQualifiedName ?? baseType?.FullName ?? baseType?.Name;
  }

  /// <summary>
  /// The visibility bits of the type attributes
  /// </summary>
  [JsonProperty("visibility")]
  [JsonConverter(typeof(StringEnumConverter))]
  public TypeVisibility Visibility { get; }

  /// <summary>
  /// The categorization of the type
  /// </summary>
  [JsonProperty("kind")]
  public string Kind { get; }

  /// <summary>
  /// The kind of generic type (or null of not generic at all)
  /// </summary>
  [JsonProperty("generic")]
  public string? Generic { get; }

  /// <summary>
  /// The short name
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

  /// <summary>
  /// The full name (null for generic type parameters)
  /// </summary>
  [JsonProperty("fullname")]
  public string? FullName { get; }

  /// <summary>
  /// The assembly qualified name (null for generic type parameters)
  /// </summary>
  [JsonProperty("qualifiedname")]
  public string? AssemblyQualifiedName { get; }

  /// <summary>
  /// Tell the serializer not to serialize <see cref="Generic"/> if it is null
  /// </summary>
  public bool ShouldSerializeGeneric() => Generic != null;

  /// <summary>
  /// The <see cref="AssemblyQualifiedName"/>, <see cref="FullName"/> or <see cref="Name"/>
  /// of the base type, whichever is not null. Will be null for the System.Object type.
  /// </summary>
  [JsonProperty("basetype")]
  public string? BaseType { get; }

  /// <summary>
  /// The <see cref="AssemblyQualifiedName"/>, <see cref="FullName"/> or <see cref="Name"/>
  /// of the declaring type, whichever is not null. Will be null if there is no declaring type.
  /// </summary>
  [JsonProperty("declaringtype")]
  public string? DeclaringType { get; }

  /// <summary>
  /// Tell the serializer not to serialize <see cref="DeclaringType"/> if it is null
  /// </summary>
  public bool ShouldSerializeDeclaringType() => DeclaringType != null;

  /// <summary>
  /// True if abstract
  /// </summary>
  [JsonProperty("abstract")]
  public bool IsAbstract { get; }

  /// <summary>
  /// True if sealed
  /// </summary>
  [JsonProperty("sealed")]
  public bool IsSealed { get; }

  /// <summary>
  /// Categorize the type
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static string Categorize(Type type)
  {
    if(type.IsInterface)
    {
      return "interface";
    }
    if(type.IsArray)
    {
      return "array";
    }
    if(type.IsEnum)
    {
      return "enum";
    }
    if(type.IsValueType)
    {
      return "valuetype";
    }
    if(type.IsGenericParameter)
    {
      return "typeparam";
    }
    if(type.IsClass)
    {
      var baseType = type.BaseType;
      if(baseType != null && baseType.FullName == "System.MulticastDelegate")
      {
        return "delegate";
      }
      return "class";
    }
    return "unknown";
  }

  /// <summary>
  /// Categorize a generic type
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static string? CategorizeGeneric(Type type)
  {
    if(type.IsGenericType)
    {
      if(type.IsGenericTypeDefinition)
      {
        return "definition";
      }
      if(type.ContainsGenericParameters)
      {
        return "open";
      }
      return "closed";
    }
    return null;
  }

  /// <summary>
  /// Sort order based on <see cref="Visibility"/>. The aim is to put the most 
  /// open visibilities first and the most restrictive last
  /// </summary>
  [JsonIgnore]
  public int VisibilityOrder =>
    Visibility switch {
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
