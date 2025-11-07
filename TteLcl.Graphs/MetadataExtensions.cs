using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs;

/// <summary>
/// Extension methods on <see cref="IHasMetadata"/>
/// </summary>
public static class MetadataExtensions
{

  /// <summary>
  /// Check if the <paramref name="owner"/>'s metadata has the specified keyed tag
  /// </summary>
  public static bool HasTag(this IHasMetadata owner, string key, string tag)
  {
    return owner.Metadata[key, tag];
  }

  /// <summary>
  /// Check if the <paramref name="owner"/>'s metadata has the specified unkeyed tag
  /// </summary>
  public static bool HasTag(this IHasMetadata owner, string tag)
  {
    return owner.Metadata["", tag];
  }

  /// <summary>
  /// Add the specified keyed tag to the <paramref name="owner"/>'s metadata
  /// </summary>
  public static bool AddTag(this IHasMetadata owner, string key, string tag)
  {
    var tags = owner.Metadata[key];
    return tags.Add(tag);
  }

  /// <summary>
  /// Add the specified unkeyed tag to the <paramref name="owner"/>'s metadata
  /// </summary>
  public static bool AddTag(this IHasMetadata owner, string tag)
  {
    var tags = owner.Metadata[""];
    return tags.Add(tag);
  }

  /// <summary>
  /// Return the property collection of the <paramref name="owner"/>'s metadata
  /// </summary>
  public static Dictionary<string, string> GetProperties(this IHasMetadata owner)
  {
    return owner.Metadata.Properties;
  }

  /// <summary>
  /// Import metadata from another metadata owner
  /// </summary>
  /// <param name="owner"></param>
  /// <param name="source"></param>
  /// <param name="tags"></param>
  /// <param name="properties"></param>
  public static void ImportMetadata(this IHasMetadata owner, IHasMetadata source, bool tags=true, bool properties=true)
  {
    owner.Metadata.Import(source.Metadata, tags, properties);
  }
}
