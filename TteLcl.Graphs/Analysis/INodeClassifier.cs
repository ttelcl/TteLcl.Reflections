using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Analysis;

/// <summary>
/// A classification strategy that maps a node key to a "class"
/// </summary>
public interface INodeClassifier
{
  /// <summary>
  /// Classify a node in a graph, returning the resulting class name,
  /// or null to keep the node outside the implementation's classification
  /// </summary>
  /// <param name="nodeKey"></param>
  /// <returns></returns>
  string? ClassifyNode(string nodeKey);
}

/// <summary>
/// Extension methods related to <see cref="INodeClassifier"/>
/// </summary>
public static class NodeClassifierExtensions
{
  /// <summary>
  /// Try classifing node <paramref name="nodeKey"/> using the given
  /// <paramref name="classifier"/>
  /// </summary>
  /// <param name="classifier">
  /// The classifier instance to use
  /// </param>
  /// <param name="nodeKey">
  /// The key of the node to classify
  /// </param>
  /// <param name="classification">
  /// On success: the non-null classification
  /// </param>
  /// <returns>
  /// True if classification succeeded, false if it returned null
  /// </returns>
  public static bool TryClassifyNode(
    this INodeClassifier classifier,
    string nodeKey,
    [NotNullWhen(true)] out string? classification)
  {
    classification = classifier.ClassifyNode(nodeKey);
    return classification != null;
  }

  /// <summary>
  /// Classify all nodes in <paramref name="nodeKeys"/> and return a mapping of
  /// classifications to lists of node keys
  /// </summary>
  /// <param name="classifier"></param>
  /// <param name="nodeKeys"></param>
  /// <returns></returns>
  public static IReadOnlyDictionary<string, List<string>> ClassifyAll(
    this INodeClassifier classifier, IEnumerable<string> nodeKeys)
  {
    var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    foreach(var nodeKey in nodeKeys)
    {
      if(classifier.TryClassifyNode(nodeKey, out var classification))
      {
        if(!result.TryGetValue(classification, out var list))
        {
          list = new List<string>();
          result[classification] = list;
        }
        list.Add(nodeKey);
      }
    }
    return result;
  }
}
