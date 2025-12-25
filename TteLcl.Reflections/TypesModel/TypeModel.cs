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
    // Deserialization is not supported yet
    throw new NotSupportedException();
  }

  /// <summary>
  /// Construct based on a <see cref="Type"/> instance
  /// </summary>
  /// <param name="type"></param>
  public TypeModel(Type type)
  {
    Name = type.Name;
    Label = type.ToString();
    AssemblyName = type.Assembly.FullName;
    AssemblyTag = AssemblyString(type.Assembly);
    Visibility = (TypeVisibility)(int)(type.Attributes & TypeAttributes.VisibilityMask);
    IsAbstract = type.IsAbstract;
    IsSealed = type.IsSealed;
    Kind = Categorize(type);
    Generic = CategorizeGeneric(type);
    var declaringType = type.DeclaringType;
    DeclaringType = declaringType?.ToString();
    DeclaringTypeVisibility =
      declaringType == null
      ? null
      : (TypeVisibility)(int)(declaringType.Attributes & TypeAttributes.VisibilityMask);
    DeclaringAssembly = AssemblyString(declaringType?.Assembly);
    var baseType = type.BaseType;
    // BaseType0 = baseType?.AssemblyQualifiedName ?? baseType?.FullName ?? baseType?.Name;
    BaseType = baseType?.ToString();
    BaseAssembly = AssemblyString(baseType?.Assembly);
    var interfaces = type.GetInterfaces();
    Interfaces = interfaces.Select(i => i.ToString()).ToList();
  }

  /// <summary>
  /// The short name
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// The visibility bits of the type attributes
  /// </summary>
  [JsonProperty("visibility")]
  [JsonConverter(typeof(StringEnumConverter))]
  public TypeVisibility Visibility { get; }

  /// <summary>
  /// Sort order based on <see cref="Visibility"/>.
  /// </summary>
  [JsonProperty("visrank")]
  public int VisibilityOrder => VisibilityRankOrder(Visibility);

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
  /// The name of the assembly (long version)
  /// </summary>
  [JsonProperty("assemblyname")]
  public string? AssemblyName { get; }

  /// <summary>
  /// The short assembly name
  /// </summary>
  [JsonProperty("assembly")]
  public string? AssemblyTag { get; }

  ///// <summary>
  ///// Tell the serializer not to serialize <see cref="Generic"/> if it is null
  ///// </summary>
  //public bool ShouldSerializeGeneric() => Generic != null;

  /// <summary>
  /// The label of the base type (null for System.Object)
  /// </summary>
  [JsonProperty("basetype")]
  public string? BaseType { get; }

  /// <summary>
  /// The assembly of the base type (null for System.Object)
  /// </summary>
  [JsonProperty("baseassembly")]
  public string? BaseAssembly { get; }

  ///// <summary>
  ///// Tell the serializer not to serialize <see cref="BaseAssembly"/> if it is null
  ///// or if it is the same as <see cref="AssemblyName"/>
  ///// </summary>
  //public bool ShouldSerializeBaseAssembly() => BaseAssembly != null && BaseAssembly != AssemblyName;

  /// <summary>
  /// The label
  /// of the declaring type, whichever is not null. Will be null if there is no declaring type.
  /// </summary>
  [JsonProperty("declaringtype")]
  public string? DeclaringType { get; }

  /// <summary>
  /// Visibility of the declaring type (if any)
  /// </summary>
  [JsonProperty("dt-visibility")]
  [JsonConverter(typeof(StringEnumConverter))]
  public TypeVisibility? DeclaringTypeVisibility { get; }

  /// <summary>
  /// Visibilty rank of the declaring type.
  /// </summary>
  [JsonProperty("dt-vis-rank")]
  public int? DeclaringTypeVisibilityRank =>
    DeclaringTypeVisibility==null ? null : VisibilityRankOrder(DeclaringTypeVisibility.Value);

  ///// <summary>
  ///// Tell the serializer not to serialize <see cref="DeclaringType"/> if it is null
  ///// </summary>
  //public bool ShouldSerializeDeclaringType() => DeclaringType != null;

  /// <summary>
  /// The assembly of the declaring type, if defined
  /// </summary>
  [JsonProperty("declaringassembly")]
  public string? DeclaringAssembly { get; }

  ///// <summary>
  ///// Tell the serializer not to serialize <see cref="DeclaringAssembly"/> if it is null
  ///// or if it is the same as <see cref="AssemblyName"/>
  ///// </summary>
  //public bool ShouldSerializeDeclaringAssembly() => DeclaringAssembly != null && DeclaringAssembly != AssemblyName;

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
  /// Interfaces implemented or inherited
  /// </summary>
  [JsonProperty("interfaces")]
  public IReadOnlyList<string> Interfaces { get; }

  ///// <summary>
  ///// Tell the serializer not to serialize <see cref="DeclaringType"/> if it is empty
  ///// </summary>
  //public bool ShouldSerializeInterfaces() => Interfaces.Count > 0;

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
  /// Assign a sort order to a visibilty, aiming to give more visibile ones
  /// a lower result.
  /// </summary>
  /// <param name="v"></param>
  /// <returns></returns>
  public static int VisibilityRankOrder(TypeVisibility v)
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

  /// <summary>
  /// Get the short name for the assembly. In unusual cases this may return null
  /// even if <paramref name="assembly"/> is not null.
  /// </summary>
  /// <param name="assembly"></param>
  /// <returns></returns>
  public static string? AssemblyString(Assembly? assembly)
  {
    return assembly?.GetName().Name;
  }
}
