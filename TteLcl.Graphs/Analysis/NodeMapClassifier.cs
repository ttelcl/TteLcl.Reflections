using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// Implements <see cref="INodeClassifier"/> based on a given mapping from
/// node keys to node classifications
/// </summary>
public class NodeMapClassifier: INodeClassifier
{
  private readonly Dictionary<string, string> _map;

  /// <summary>
  /// Create a new <see cref="NodeMapClassifier"/> with a snapshot of the
  /// key to classification mapping in <paramref name="map"/>.
  /// </summary>
  /// <param name="map"></param>
  public NodeMapClassifier(
    IReadOnlyDictionary<string, string> map)
  {
    _map = new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Create a new <see cref="NodeMapClassifier"/> from an existing classification-to-node-keys mapping
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="classifications"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static NodeMapClassifier FromClassificationMap<T>(IReadOnlyDictionary<string, T> classifications)
    where T: IEnumerable<string>
  {
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach(var kvp in classifications)
    {
      foreach(var nodeKey in kvp.Value)
      {
        if(map.TryGetValue(nodeKey, out var conflict))
        {
          throw new InvalidOperationException(
            $"conflicting classification of node '{nodeKey}': '{kvp.Key}' and '{conflict}'");
        }
        map.Add(nodeKey, kvp.Key);
      }
    }
    return new NodeMapClassifier(map);
  }

  /// <summary>
  /// Create a new <see cref="NodeMapClassifier"/> from an existing classification-to-nodes-with-keys mapping
  /// </summary>
  /// <typeparam name="TCollection"></typeparam>
  /// <typeparam name="TItem"></typeparam>
  /// <param name="classifications"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static NodeMapClassifier FromNodeClassificationMap<TCollection, TItem>(IReadOnlyDictionary<string, TCollection> classifications)
    where TCollection : IEnumerable<TItem>
    where TItem : IHasKey
  {
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach(var kvp in classifications)
    {
      foreach(var nodeWithKey in kvp.Value)
      {
        var nodeKey = nodeWithKey.Key;
        if(map.TryGetValue(nodeKey, out var conflict))
        {
          throw new InvalidOperationException(
            $"conflicting classification of node '{nodeKey}': '{kvp.Key}' and '{conflict}'");
        }
        map.Add(nodeKey, kvp.Key);
      }
    }
    return new NodeMapClassifier(map);
  }

  /// <inheritdoc/>
  public string? ClassifyNode(string nodeKey)
  {
    return
      _map.TryGetValue(nodeKey, out var classification) ? classification : null;
  }
}
