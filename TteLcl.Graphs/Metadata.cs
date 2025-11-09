using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TteLcl.Graphs;

/// <summary>
/// Generic metadata class used by <see cref="Graph"/> and its components.
/// </summary>
public class Metadata: IHasMetadata
{
  private readonly Dictionary<string, HashSet<string>> _keyedTags;

  /// <summary>
  /// Create an empty <see cref="Metadata"/> instance
  /// </summary>
  public Metadata()
  {
    _keyedTags = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    _keyedTags[""] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// "properties": string-string key-value pairs with a unique,
  /// case insensitive, key. Fully mutable.
  /// </summary>
  public Dictionary<string, string> Properties { get; }

  /// <summary>
  /// Set or clear a property
  /// </summary>
  /// <param name="key">
  /// The property key
  /// </param>
  /// <param name="value">
  /// The property value. If null, the property will be deleted instead of set
  /// </param>
  public void SetProperty(string key, string? value)
  {
    if(value == null)
    {
      Properties.Remove(key);
    }
    else
    {
      Properties[key] = value;
    }
  }

  /// <summary>
  /// "keyed tags": string-stringset key-values pairs with a unique,
  /// case insensitive, key. The sets are automatically created upon access.
  /// If creating a set upon access is undesirable (read-only scenarios), use 
  /// <see cref="TryGetTags"/> instead.
  /// </summary>
  /// <param name="key"></param>
  /// <returns></returns>
  public HashSet<string> this[string key] {
    get {
      if(!_keyedTags.TryGetValue(key, out var tags))
      {
        tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _keyedTags[key] = tags;
      }
      return tags;
    }
  }

  /// <summary>
  /// Check if a keyed tag is present, or add or remove it.
  /// </summary>
  /// <param name="key">
  /// The tag key. Use "" for unkeyed tags
  /// </param>
  /// <param name="tag">
  /// The tag to query, add, or remove.
  /// </param>
  /// <returns></returns>
  public bool this[string key, string tag] {
    get => _keyedTags.TryGetValue(key, out var tags) && tags.Contains(tag);
    set {
      if(value)
      {
        this[key].Add(tag);
      }
      else if(_keyedTags.TryGetValue(key, out var tags0))
      {
        tags0.Remove(tag);
      }
    }
  }

  /// <summary>
  /// Returns true if this metadata has at least one of the specified
  /// keyed tags
  /// </summary>
  /// <param name="key">
  /// The tag key (category). Pass "" for unkeyed tags
  /// </param>
  /// <param name="tags">
  /// The tags to find
  /// </param>
  /// <returns></returns>
  public bool HasAnyTag(string key, IEnumerable<string> tags)
  {
    if(TryGetTags(key, out var keytags))
    {
      return keytags.Overlaps(tags);
    }
    return false;
  }

  /// <summary>
  /// Returns true if this metadata has at least one of the specified
  /// unkeyed tags
  /// </summary>
  /// <param name="tags">
  /// The tags to find
  /// </param>
  /// <returns></returns>
  public bool HasAnyTag(IEnumerable<string> tags)
  {
    return HasAnyTag("", tags);
  }

  /// <summary>
  /// Try accessing an existing keyed tag set for reading purposes.
  /// Empty tag sets are treated as if not existing.
  /// For writing purposes use <see cref="this[string]"/> instead.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="tags"></param>
  /// <returns></returns>
  public bool TryGetTags(
    string key,
    [NotNullWhen(true)] out IReadOnlySet<string>? tags)
  {
    if(_keyedTags.TryGetValue(key, out var tags0) && tags0.Count > 0)
    {
      tags = tags0;
      return true;
    }
    tags = null;
    return false;
  }

  /// <summary>
  /// Get the collection of "unkeyed tags". Those are just keyed tags with
  /// a key that is an empty string.
  /// </summary>
  public IReadOnlySet<string> Tags => this[""];

  /// <summary>
  /// The set of tag keys.
  /// </summary>
  public IReadOnlyCollection<string> TagKeys => _keyedTags.Keys;

  /// <summary>
  /// Import metadata from another metadata object
  /// </summary>
  /// <param name="source">
  /// The object to import from
  /// </param>
  /// <param name="tags">
  /// If true, then import all tags (using the same keys)
  /// </param>
  /// <param name="properties">
  /// If true, then import all properties
  /// </param>
  public void Import(Metadata source, bool tags = true, bool properties = true)
  {
    if(properties)
    {
      foreach(var kvp in source.Properties)
      {
        Properties[kvp.Key] = kvp.Value;
      }
    }
    if(tags)
    {
      foreach(var key in source.TagKeys)
      {
        var sourceTags = source[key];
        if(sourceTags.Count > 0)
        {
          this[key].UnionWith(sourceTags);
        }
      }
    }
  }

  Metadata IHasMetadata.Metadata => this;

  /// <summary>
  /// Add this metadata into the given JSON object
  /// </summary>
  /// <param name="o"></param>
  public void AddToObject(JObject o)
  {
    // Add properties directly into the object
    foreach(var kvp in Properties)
    {
      o.Add(kvp.Key, kvp.Value);
    }
    var ktags = new JObject();
    var tags = new JArray();
    foreach(var kvp in _keyedTags)
    {
      if(kvp.Key == "")
      {
        foreach(var tag in kvp.Value)
        {
          tags.Add(tag);
        }
      }
      else
      {
        var a = new JArray();
        foreach(var tag in kvp.Value)
        {
          a.Add(tag);
        }
        if(a.Count > 1)
        {
          ktags[kvp.Key] = a;
        }
        else if(a.Count == 1)
        {
          ktags[kvp.Key] = a[0];
        }
        // else: skip this key
      }
    }
    if(tags.Count > 0)
    {
      o["tags"] = tags;
    }
    if(ktags.Count > 0)
    {
      o["keytags"] = ktags;
    }
  }

  /// <summary>
  /// Add properties and tags from their JSON representation in
  /// <paramref name="o"/>, reversing <see cref="AddToObject"/>.
  /// </summary>
  /// <param name="o">
  /// The object to get property and tag values from
  /// </param>
  /// <param name="exclude">
  /// Names of properties in <paramref name="o"/> to skip. Names
  /// "tags" and "keytags" will be skipped too.
  /// </param>
  public void FillFromObject(JObject o, IEnumerable<string> exclude)
  {
    var excludeSet = exclude.ToHashSet();
    excludeSet.UnionWith(["keytags", "tags"]);
    foreach(var prop in o.Properties())
    {
      if(excludeSet.Contains(prop.Name))
      {
        continue;
      }
      if(prop.Value is JValue v && v.Type == JTokenType.String)
      {
        var s = (string)v!;
        Properties[prop.Name] = s;
      }
      // else ???
    }
    var keytagsToken = o["keytags"];
    if(keytagsToken is JObject keytags)
    {
      foreach(var prop in keytags.Properties())
      {
        if(prop.Value is JArray a1 && a1.Count > 0)
        {
          var kt = this[prop.Name];
          foreach(var tag in a1)
          {
            if(tag is JValue v && v.Type == JTokenType.String)
            {
              var s = (string)v!;
              kt.Add(s);
            }
          }
        }
        else if(prop.Value is JValue v && v.Type == JTokenType.String)
        {
          var s = (string)v!;
          this[prop.Name].Add(s);
        }
      }
    }
    var tagsToken = o["tags"];
    if(tagsToken is JArray a2)
    {
      foreach(var tag in a2)
      {
        var kt = this[""];
        if(tag is JValue v && v.Type == JTokenType.String)
        {
          var s = (string)v!;
          kt.Add(s);
        }
      }
    }
  }
}
