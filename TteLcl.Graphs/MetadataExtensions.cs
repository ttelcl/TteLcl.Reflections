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
  /// Check if the <paramref name="owner"/>'s metadata has any of the specified keyed tags
  /// </summary>
  public static bool HasAnyTag(this IHasMetadata owner, string key, IEnumerable<string> tags)
  {
    return owner.Metadata.HasAnyTag(key, tags);
  }

  /// <summary>
  /// Check if the <paramref name="owner"/>'s metadata has any of the specified unkeyed tags
  /// </summary>
  public static bool HasAnyTag(this IHasMetadata owner, IEnumerable<string> tags)
  {
    return owner.Metadata.HasAnyTag("", tags);
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
  /// Import metadata copied from another metadata owner
  /// </summary>
  /// <param name="owner"></param>
  /// <param name="source"></param>
  /// <param name="tags"></param>
  /// <param name="properties"></param>
  public static void ImportMetadata(this IHasMetadata owner, IHasMetadata source, bool tags = true, bool properties = true)
  {
    owner.Metadata.Import(source.Metadata, tags, properties);
  }
  
  /// <summary>
  /// Import metadata into the metadata of <paramref name="owner"/>
  /// </summary>
  /// <param name="owner">
  /// The owner of the metadata to modifiy
  /// </param>
  /// <param name="metadata">
  /// The metadata to import into the <paramref name="owner"/>'s metadata, or null 
  /// to not import anything.
  /// </param>
  /// <param name="tags"></param>
  /// <param name="properties"></param>
  public static void ImportMetadata(this IHasMetadata owner, Metadata? metadata, bool tags = true, bool properties = true)
  {
    if(metadata != null)
    {
      owner.Metadata.Import(metadata, tags, properties);
    }
  }

  /// <summary>
  /// Import metadata. This is an extension method nearly equal to the underlying <see cref="Metadata.Import(Metadata, bool, bool)"/>,
  /// but allows the imported metadata to be null (to skip the operation), making the API smoother for many use cases.
  /// </summary>
  /// <param name="target"></param>
  /// <param name="metadata"></param>
  /// <param name="tags"></param>
  /// <param name="properties"></param>
  public static void ImportFrom(this Metadata target, Metadata? metadata, bool tags = true, bool properties = true)
  {
    if(metadata != null)
    {
      target.Import(metadata, tags, properties);
    }
  }

  /// <summary>
  /// Import the potentially-not-existing <paramref name="metadata"/> into the existing <paramref name="target"/> metadata
  /// </summary>
  /// <param name="metadata">
  /// The metadata to import into <paramref name="target"/>, or null to ignore the call
  /// </param>
  /// <param name="target">
  /// The target metadata object to modify
  /// </param>
  /// <param name="tags"></param>
  /// <param name="properties"></param>
  public static void ImportInto(this Metadata? metadata, Metadata target, bool tags = true, bool properties = true)
  {
    if(metadata != null)
    {
      target.Import(metadata, tags, properties);
    }
  }

  /// <summary>
  /// Import the potentially-not-existing <paramref name="metadata"/> into the metadata of the
  /// existing metadata owner <paramref name="target"/>
  /// </summary>
  /// <param name="metadata">
  /// The metadata to import into <paramref name="target"/>, or null to ignore the call
  /// </param>
  /// <param name="target">
  /// The owner of the target metadata object to modify
  /// </param>
  /// <param name="tags"></param>
  /// <param name="properties"></param>
  public static void ImportInto(this Metadata? metadata, IHasMetadata target, bool tags = true, bool properties = true)
  {
    if(metadata != null)
    {
      target.Metadata.Import(metadata, tags, properties);
    }
  }

  /// <summary>
  /// Return a set of all property names found in the metadata instances of the given metadata owners
  /// </summary>
  /// <param name="owners"></param>
  /// <returns></returns>
  public static IReadOnlyCollection<string> AllPropertyNames<T>(this IEnumerable<T> owners)
    where T : IHasMetadata
  {
    return Metadata.AllPropertyNames(owners.Select(owner => owner.Metadata));
  }

  /// <summary>
  /// Return a set of all tag keys found in the metadata instances of the given metadata owners
  /// </summary>
  /// <param name="owners"></param>
  /// <returns></returns>
  public static IReadOnlyCollection<string> AllTagKeys<T>(this IEnumerable<T> owners)
    where T : IHasMetadata
  {
    return Metadata.AllTagKeys(owners.Select(owner => owner.Metadata));
  }
}
