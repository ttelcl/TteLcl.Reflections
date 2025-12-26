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
/// A serializable reference to a <see cref="TypeNodeModel"/>
/// </summary>
public class TypeNodeReference
{
  /// <summary>
  /// Deserialization constructor
  /// </summary>
  public TypeNodeReference(
    string? name,
    string? assembly,
    string key)
  {
    Name = name;
    AssemblyName = assembly;
    Key = key;
  }

  /// <summary>
  /// Create a new TypeNodeReference from a type
  /// </summary>
  public TypeNodeReference(
    Type t)
  {
    Name = t.FullName;
    AssemblyName = t.Assembly.GetName().Name;
    if(!String.IsNullOrEmpty(Name) && !String.IsNullOrEmpty(AssemblyName))
    {
      Key = $"{Name}, {AssemblyName}";
    }
    else
    {
      var name = t.ToString();
      var an = t.Assembly.GetName().Name ?? t.Assembly.ToString();
      Key = $"? {name}, {an}";
    }
  }

  /// <summary>
  /// Create a clone of <paramref name="template"/>.
  /// </summary>
  /// <param name="template"></param>
  public TypeNodeReference(TypeNodeReference template)
    : this(template.Name, template.AssemblyName, template.Key)
  {
  }

  /// <summary>
  /// The key used to identify this type. Normally formed from
  /// <see cref="Name"/> and <see cref="AssemblyName"/>.
  /// If either of those is missing, a fallback starting with '?' is used
  /// </summary>
  [JsonProperty("key")]
  public string Key { get; }

  /// <summary>
  /// The type name, if available
  /// </summary>
  [JsonProperty("name")]
  public string? Name { get; }

  /// <summary>
  /// The assembly name, if available
  /// </summary>
  [JsonProperty("assembly")]
  public string? AssemblyName { get; }
}
