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
    : base(node.Identity)
  {
    var type = node.TargetType;
    Base = node.BaseNode?.Key;
    var interfaces = new List<string>();
    Interfaces = interfaces;
    foreach(var intf in node.Interfaces.OrderBy(node => node.Key))
    {
      interfaces.Add(intf.Key);
    }
    Label = type.ToString();
    Visibility = type.AsTypeVisibility();
  }

  /// <summary>
  /// A label to refer to the type. Not necessarily unique
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// A reference to the base type (if any)
  /// </summary>
  [JsonProperty("base")]
  public string? Base { get; }

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
  /// The interfaces implemented by this type, if any
  /// </summary>
  [JsonProperty("interfaces")]
  public IReadOnlyList<string> Interfaces { get; }

}
