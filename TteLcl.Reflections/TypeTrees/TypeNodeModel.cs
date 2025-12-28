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
using Newtonsoft.Json.Converters;

using TteLcl.Reflections.TypesModel;

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
    long id,
    string? name,
    string? assembly,
    string key)
    : base(id, name, assembly, key)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Create a new TypeNodeModel
  /// </summary>
  public TypeNodeModel(TypeNode node)
    : base(node.Identity)
  {
    var host = node.Owner;
    var type = node.TargetType;
    Base = node.BaseNode?.Id ?? 0;
    var interfaces = new List<long>();
    Interfaces = interfaces;
    foreach(var intf in node.Interfaces.OrderBy(node => node.Key))
    {
      interfaces.Add(intf.Id);
    }
    Label = type.ToString();
    ShortName = type.Name;
    Visibility = type.AsTypeVisibility();
    DeclaringType = node.DeclaringNode?.Id ?? 0;
    IsAbstract = node.IsAbstract;
    IsSealed = node.IsSealed;
    TypeKind = node.TypeKind;
    GenericKind = node.GenericKind;
    GenericArgs = node.GenericArguments;
    GenericDefinition = node.GenericDefinitionNode?.Key;
    IsVisible = type.IsVisible;
    Tree = node.Tree;
    LinkedTypes = 
      node.ImplementationTypes.Select(node => node.Key).OrderBy(key => key).ToList();
    AssemblyFull = type.Assembly.FullName;
    ElementType = node.ElementType;
  }

  /// <summary>
  /// A label to refer to the type. Not necessarily unique
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// The short name for the type
  /// </summary>
  [JsonProperty("shortname")]
  public string ShortName { get; }

  /// <summary>
  /// The kind of type
  /// </summary>
  [JsonProperty("kind")]
  public string TypeKind { get; }

  /// <summary>
  /// True if the type is public or nested in a way that is publicly accessible
  /// </summary>
  [JsonProperty("visible")]
  public bool IsVisible { get; }

  /// <summary>
  /// The visibility of the type
  /// </summary>
  [JsonProperty("visibility")]
  [JsonConverter(typeof(StringEnumConverter))]
  public TypeVisibility Visibility { get; }

  /// <summary>
  /// A numerical rank for <see cref="Visibility"/> to aid sorting
  /// </summary>
  [JsonProperty("visrank")]
  public int VisRank => Visibility.RankOrder();

  /// <summary>
  /// A reference to the base type (if any; 0 if none)
  /// </summary>
  [JsonProperty("base")]
  public long Base { get; }

  /// <summary>
  /// The IDs of the interfaces implemented by this type, if any
  /// </summary>
  [JsonProperty("interfaces")]
  public IReadOnlyList<long> Interfaces { get; }

  /// <summary>
  /// Reference to the declaring type, if any (0 if none)
  /// </summary>
  [JsonProperty("declaringtype")]
  public long DeclaringType { get; }

  /// <summary>
  /// Reference to the (array/pointer/reference) element type, if any
  /// </summary>
  [JsonProperty("element")]
  public TypeArgumentInfo? ElementType { get; }

  /// <summary>
  /// True if the type is sealed
  /// </summary>
  [JsonProperty("sealed")]
  public bool IsSealed { get; }

  /// <summary>
  /// True if the type is abstract
  /// </summary>
  [JsonProperty("abstract")]
  public bool IsAbstract { get; }

  /// <summary>
  /// True if this type is "static". Equivalent to <see cref="IsSealed"/> AND <see cref="IsAbstract"/>
  /// (there is no "static" concept in IL)
  /// </summary>
  [JsonProperty("static")]
  public bool IsStatic => IsSealed && IsAbstract;

  /// <summary>
  /// True if the type is nested
  /// </summary>
  [JsonProperty("nested")]
  public bool IsNested =>
    Visibility switch {
      TypeVisibility.Private => false,
      TypeVisibility.Public => false,
      _ => true
    };

  /// <summary>
  /// The kind of generic type this is (or null if none at all)
  /// </summary>
  [JsonProperty("generic")]
  public string? GenericKind { get; }

  /// <summary>
  /// The generic type argument descriptors (empty if not relevant)
  /// </summary>
  [JsonProperty("arguments")]
  public IReadOnlyList<TypeArgumentInfo> GenericArgs { get; }

  /// <summary>
  /// The generic type definition, is applicable. This may be a self-reference
  /// </summary>
  [JsonProperty("definition")]
  public string? GenericDefinition { get; }

  /// <summary>
  /// A tree model for composite types
  /// </summary>
  [JsonProperty("tree")]
  public TypeTreeNode Tree { get; }

  /// <summary>
  /// Types referenced in the members of the type
  /// </summary>
  [JsonProperty("linkedtypes")]
  public IReadOnlyList<string> LinkedTypes { get; }

  /// <summary>
  /// Full assembly name
  /// </summary>
  [JsonProperty("asmfull")]
  public string? AssemblyFull { get; }
}
