using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// A node in an assembly graph, describing a single assembly. This only describes
/// the node itself, not its relations.
/// JSON serializable
/// </summary>
public class AssemblyNode: IGraphNode
{
  /// <summary>
  /// Json deserialization constructor
  /// </summary>
  /// <param name="key">
  /// The full name of the assembly, parsable by <see cref="System.Reflection.AssemblyName"/>
  /// Also acts as unique identifier for this node
  /// </param>
  /// <param name="file">
  /// The full path to the file, if known
  /// </param>
  /// <param name="module">
  /// The primary tag associated with this node. This tag, if not null, should also
  /// be in <paramref name="tags"/> (and will be added automatically if it isn't).
  /// </param>
  /// <param name="tags">
  /// All tags associated with this node. If not already present, <paramref name="module"/> will
  /// be added automatically
  /// </param>
  [JsonConstructor]
  public AssemblyNode(
    string key,
    string? file = null,
    string? module = null,
    IEnumerable<string>? tags = null)
  {
    Key = key;
    AssemblyName = new AssemblyName(key);
    tags ??= [];
    Tags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
    Module = module;
    if(module != null && !Tags.Contains(module))
    {
      Tags.Add(module);
    }
    FileName = file;
  }

  /// <summary>
  /// The full assembly name (parsable to an <see cref="AssemblyName"/>).
  /// Alias for <see cref="Key"/>
  /// </summary>
  [JsonIgnore]
  public string FullName => Key;

  /// <inheritdoc/>
  [JsonProperty("label")]
  public string Label => ShortName;

  /// <inheritdoc/>
  [JsonProperty("key")]
  public string Key { get; }

  /// <summary>
  /// The file name, if available
  /// </summary>
  [JsonProperty("file")]
  public string? FileName { get; }

  /// <summary>
  /// The primary tag for this assembly, if defined. If not null, this
  /// should also be in <see cref="Tags"/>.
  /// </summary>
  [JsonProperty("module")]
  public string? Module { get; }

  /// <summary>
  /// The collection of tag strings associated with this node (mutable)
  /// </summary>
  [JsonProperty("tags")]
  public HashSet<string> Tags { get; }

  /// <summary>
  /// Prevents serializing an empty or null <see cref="FileName"/>
  /// </summary>
  /// <returns></returns>
  public bool ShouldSerializeFileName()
  {
    return !String.IsNullOrEmpty(FileName);
  }

  /// <summary>
  /// <see cref="FullName"/> parsed into an <see cref="AssemblyName"/>.
  /// </summary>
  [JsonIgnore]
  public AssemblyName AssemblyName { get; }

  /// <summary>
  /// The short assembly name. Throws an exception if not defined.
  /// </summary>
  [JsonIgnore]
  public string ShortName =>
    AssemblyName.Name ?? throw new InvalidOperationException("Not expecting an uninitialized assembly name");

  IReadOnlySet<string> IGraphNode.Tags => Tags;
}
