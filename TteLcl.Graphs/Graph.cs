using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TteLcl.Graphs.Analysis;

namespace TteLcl.Graphs;

/// <summary>
/// General purpose graph model. While not JSON serializable by itself,
/// methods for creating or loading a JSON serialized form are available.
/// </summary>
public class Graph: IHasMetadata
{
  private readonly Dictionary<string, GraphNode> _nodes;

  /// <summary>
  /// Create a new empty graph
  /// </summary>
  /// <param name="metadata">
  /// If provided: the metadata to copy into this new graph object
  /// </param>
  public Graph(
    Metadata? metadata = null)
  {
    _nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
    Metadata = new Metadata();
    if(metadata != null)
    {
      Metadata.Import(metadata);
    }
  }

  /// <summary>
  /// Try to derive a new file name from a given *.graph.json file
  /// </summary>
  /// <param name="graphJsonFile">
  /// The template *.graph.json file
  /// </param>
  /// <param name="newExtension">
  /// The new file extension (or multi-extension) to replace ".graph.json"
  /// </param>
  /// <param name="newName">
  /// The resulting new file name
  /// </param>
  /// <returns>
  /// True if successful, false if <paramref name="graphJsonFile"/> did not end with ".graph.json"
  /// </returns>
  /// <exception cref="ArgumentException">
  /// Thrown if <paramref name="newExtension"/> does not begin with "."
  /// </exception>
  public static bool TryDeriveName(string graphJsonFile, string newExtension, [NotNullWhen(true)] out string? newName)
  {
    var expectedExtension = ".graph.json";
    if(!newExtension.StartsWith('.'))
    {
      throw new ArgumentException(
        "Expecting new extension to start with a '.'", nameof(newExtension));
    }
    if(!graphJsonFile.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
    {
      newName = null;
      return false;
    }
    var prefix = graphJsonFile[..^expectedExtension.Length];
    newName = prefix + newExtension;
    return true;
  }

  /// <summary>
  /// Derive an output file name from the name of a *.graph.json file for the case where an
  /// output name was missing
  /// </summary>
  /// <param name="graphJsonFile">
  /// The template *.graph.json file
  /// </param>
  /// <param name="newExtension">
  /// The new file extension (or multi-extension) to replace ".graph.json"
  /// </param>
  /// <param name="predefinedName">
  /// The output name override. If not null (the user specified an explicit output name), this
  /// is the string that is returned (and <paramref name="graphJsonFile"/> and <paramref name="newExtension"/>
  /// are ignored)
  /// </param>
  /// <returns>
  /// <paramref name="predefinedName"/> if it is not null, or <paramref name="graphJsonFile"/> with
  /// ".graph.json" replaced by <paramref name="newExtension"/>, or null if <paramref name="graphJsonFile"/>
  /// did not end with ".graph.json".
  /// </returns>
  public static string? DeriveMissingName(string graphJsonFile, string newExtension, string? predefinedName = null)
  {
    if(!String.IsNullOrEmpty(predefinedName))
    {
      return predefinedName;
    }
    return TryDeriveName(graphJsonFile, newExtension, out var newName) ? newName : null;
  }

  /// <inheritdoc/>
  public Metadata Metadata { get; }

  /// <summary>
  /// The collection of nodes in this graph
  /// </summary>
  public IReadOnlyDictionary<string, GraphNode> Nodes => _nodes;

  /// <summary>
  /// The number of nodes in this graph
  /// </summary>
  public int NodeCount => _nodes.Count;

  /// <summary>
  /// The number of edges in this graph
  /// </summary>
  public int EdgeCount => _nodes.Values.Sum(n => n.Targets.Count);

  /// <summary>
  /// Enumerates all edges in all nodes
  /// </summary>
  public IEnumerable<GraphEdge> Edges => _nodes.Values.SelectMany(n => n.Targets.Values);

  /// <summary>
  /// Enumerate the seed nodes (calculated on the fly)
  /// </summary>
  public IEnumerable<GraphNode> SeedNodes => Nodes.Values.Where(n => n.Sources.Count == 0);

  /// <summary>
  /// Enumerate the sink nodes (calculated on the fly)
  /// </summary>
  public IEnumerable<GraphNode> SinkNodes => Nodes.Values.Where(n => n.Targets.Count == 0);

  /// <summary>
  /// The number of seed nodes (calculated on the fly)
  /// </summary>
  public int SeedCount => Nodes.Values.Count(n => n.Sources.Count == 0);

  /// <summary>
  /// The number of sink nodes (calculated on the fly)
  /// </summary>
  public int SinkCount => Nodes.Values.Count(n => n.Targets.Count == 0);

  /// <summary>
  /// Classify all nodes using <paramref name="classifier"/>, and group them based on the classification
  /// </summary>
  public Dictionary<K, List<GraphNode>> ClassifyNodes<K>(Func<GraphNode, K> classifier, IEqualityComparer<K>? comparer = null)
    where K : notnull
  {
    var result = new Dictionary<K, List<GraphNode>>(comparer);
    foreach(var node in Nodes.Values)
    {
      var classification = classifier(node);
      if(!result.TryGetValue(classification, out var list))
      {
        list = new List<GraphNode>();
        result[classification] = list;
      }
      list.Add(node);
    }
    return result;
  }

  /// <summary>
  /// Classify all nodes using their value for the given <paramref name="propertyName"/>, and group them
  /// based on the classification. Nodes where the property is missing or empty are skipped
  /// </summary>
  public Dictionary<string, List<GraphNode>> ClassifyNodes(string propertyName, IEqualityComparer<string>? comparer = null)
  {
    var result = new Dictionary<string, List<GraphNode>>(comparer);
    foreach(var node in Nodes.Values)
    {
      if(node.Metadata.Properties.TryGetValue(propertyName, out var classification)
        && !String.IsNullOrEmpty(classification))
      {
        if(!result.TryGetValue(classification, out var list))
        {
          list = new List<GraphNode>();
          result[classification] = list;
        }
        list.Add(node);
      }
    }
    return result;
  }

  /// <summary>
  /// Create a snapshot of source-node-id to target-node-id mappings for all edges
  /// </summary>
  /// <returns></returns>
  public KeySetMapView EdgesSnapShot()
  {
    var ksm = new KeySetMap();
    foreach(var node in Nodes.Values)
    {
      var targets = new KeySet(node.Targets.Keys);
      ksm.Add(node.Key, targets);
    }
    return new KeySetMapView(ksm);
  }

  /// <summary>
  /// Construct the supergraph of this graph based on the given <paramref name="classifier"/>.
  /// </summary>
  /// <param name="classifier"></param>
  /// <param name="addNodes">
  /// If true add "node" tags
  /// </param>
  /// <returns></returns>
  public Graph SuperGraph(INodeClassifier classifier, bool addNodes = true)
  {
    var result = new Graph(Metadata);
    var targetEdges = EdgesSnapShot();
    // classificationMap maps classifications to their nodes
    var classificationMap = classifier.ClassifyAll(Nodes.Keys);
    // first create nodes, before adding any edges
    foreach(var kvp in classificationMap)
    {
      var classification = kvp.Key;
      var superNode = result.AddNode(classification);
      var nodeTagCount = 0;
      // add tags to link back to the original nodes
      foreach(var nodeKey in kvp.Value)
      {
        if(addNodes)
        {
          superNode.Metadata.AddTag("node", nodeKey);
        }
        nodeTagCount++;
      }
      superNode.Metadata.Properties["sublabel"] =
        $"({nodeTagCount} nodes)";
    }
    // Then add edges
    foreach(var kvp in classificationMap)
    {
      var classification = kvp.Key;
      var superNode = result.Nodes[classification];
      foreach(var sourceKey in kvp.Value)
      {
        var edges = targetEdges[sourceKey];
        foreach(var targetKey in edges)
        {
          if(classifier.TryClassifyNode(targetKey, out var targetClassification) // skip unclassified targets
            && !superNode.Targets.ContainsKey(targetClassification) // skip duplicates
            && targetClassification != classification) // skip self-edges
          {
            var targetNode = result.Nodes[targetClassification];
            superNode.Connect(targetNode);
          }
        }
      }
    }
    return result;
  }

  /// <summary>
  /// Add a new node
  /// </summary>
  /// <param name="key">
  /// The key for the new node
  /// </param>
  /// <param name="metadata">
  /// Optional: the metadata to copy into the newly created node
  /// </param>
  /// <returns>
  /// The newly created node
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if a node with key <paramref name="key"/> already exists
  /// </exception>
  public GraphNode AddNode(string key, Metadata? metadata = null)
  {
    if(_nodes.ContainsKey(key))
    {
      throw new InvalidOperationException(
        $"Duplicate node '{key}'");
    }
    var node = new GraphNode(key, metadata);
    _nodes.Add(key, node);
    return node;
  }

  /// <summary>
  /// Add an edge between two existing nodes that are not connected yet. For the use case
  /// where an edge may exist already, use <see cref="ConnectOrMergeEdge(string, string, Metadata?)"/>
  /// instead.
  /// </summary>
  /// <param name="source">
  /// The key of the source node
  /// </param>
  /// <param name="target">
  /// The key of the target node
  /// </param>
  /// <param name="metadata">
  /// Optional metadata for the edge
  /// </param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public GraphEdge Connect(string source, string target, Metadata? metadata = null)
  {
    if(!_nodes.TryGetValue(source, out var sourceNode))
    {
      throw new InvalidOperationException(
        $"Missing source node: '{source}'");
    }
    if(!_nodes.TryGetValue(target, out var targetNode))
    {
      throw new InvalidOperationException(
        $"Missing target node: '{target}'");
    }
    return sourceNode.Connect(targetNode, metadata);
  }

  /// <summary>
  /// If there is an edge between <paramref name="source"/> and <paramref name="target"/>
  /// then return it. Otherwise return null
  /// </summary>
  /// <param name="source"></param>
  /// <param name="target"></param>
  /// <returns></returns>
  public GraphEdge? FindEdge(string source, string target)
  {
    if(!_nodes.TryGetValue(source, out var sourceNode))
    {
      throw new InvalidOperationException(
        $"Missing source node: '{source}'");
    }
    if(!_nodes.TryGetValue(target, out var targetNode))
    {
      throw new InvalidOperationException(
        $"Missing target node: '{target}'");
    }
    if(sourceNode.Targets.TryGetValue(target, out var edge))
    {
      return edge;
    }
    else
    {
      return null;
    }
  }

  /// <summary>
  /// Add an edge between <paramref name="source"/> and <paramref name="target"/> if
  /// it does not exist yet (using the specified edge <paramref name="metadata"/>).
  /// Otherwise import <paramref name="metadata"/> into the existing edge metadata.
  /// For the use case where it is an error if the edge already exists, use
  /// <see cref="Connect(string, string, Metadata?)"/> instead.
  /// </summary>
  /// <param name="source"></param>
  /// <param name="target"></param>
  /// <param name="metadata"></param>
  /// <returns></returns>
  public GraphEdge ConnectOrMergeEdge(string source, string target, Metadata? metadata = null)
  {
    var edge = FindEdge(source, target);
    if(edge != null)
    {
      if(metadata != null)
      {
        edge.Metadata.Import(metadata);
      }
      return edge;
    }
    else
    {
      return Connect(source, target, metadata);
    }
  }

  /// <summary>
  /// Create or merge multiple edges. Each key in <paramref name="edges"/> is a source key,
  /// while the values for that entry are the targets for the edges to create or merge.
  /// Returns each created or processed edge
  /// </summary>
  /// <param name="edges">
  /// A mapping from source keys to sets of target keys, defining the edges.
  /// </param>
  /// <param name="metadata">
  /// If not null, new edges will initially use this metadata, while existing edges will
  /// have this metadata merged into their metadata.
  /// </param>
  /// <returns></returns>
  public IEnumerable<GraphEdge> ConnectMany(IReadOnlyDictionary<string, KeySet> edges, Metadata? metadata = null)
  {
    foreach(var kvp in edges)
    {
      var source = kvp.Key;
      foreach(var target in kvp.Value)
      {
        var edge = ConnectOrMergeEdge(source, target, Metadata);
        yield return edge;
      }
    }
  }

  /// <summary>
  /// Disconnect two nodes (if they were connected and existed)
  /// </summary>
  /// <param name="source">
  /// The key of the source node of the edge to remove. If this node does not exist this method returns null.
  /// </param>
  /// <param name="target">
  /// The key of the target node of the edge to remove. If this node does not exist this method returns null.
  /// </param>
  /// <returns>
  /// Returns the edge that was removed or null if either endpoint node did not exist at all or there was no edge
  /// between them.
  /// </returns>
  public GraphEdge? Disconnect(string source, string target)
  {
    if(!_nodes.TryGetValue(source, out var sourceNode))
    {
      return null;
    }
    return sourceNode.DisconnectTarget(target);
  }

  /// <summary>
  /// Disconnect all source edges from a target node
  /// </summary>
  /// <param name="target"></param>
  /// <returns></returns>
  public IReadOnlyCollection<GraphEdge> DisconnectAllSources(string target)
  {
    var result = new List<GraphEdge>();
    if(_nodes.TryGetValue(target, out var targetNode))
    {
      result.AddRange(targetNode.Sources.Values);
      foreach(var edge in result)
      {
        edge.Disconnect();
      }
    }
    return result;
  }

  /// <summary>
  /// Disconnect all target edges from a source node
  /// </summary>
  /// <param name="source"></param>
  /// <returns></returns>
  public IReadOnlyCollection<GraphEdge> DisconnectAllTargets(string source)
  {
    var result = new List<GraphEdge>();
    if(_nodes.TryGetValue(source, out var sourceNode))
    {
      result.AddRange(sourceNode.Targets.Values);
      foreach(var edge in result)
      {
        edge.Disconnect();
      }
    }
    return result;
  }

  /// <summary>
  /// Remove nodes in the given key set, as well as any edges to or from those nodes
  /// in the remaining nodes
  /// </summary>
  /// <param name="nodeKeys"></param>
  public void RemoveNodes(IEnumerable<string> nodeKeys)
  {
    foreach(var key in nodeKeys)
    {
      _nodes.Remove(key);
    }
    foreach(var node in _nodes.Values)
    {
      node.RemoveEdges(nodeKeys);
    }
  }

  /// <summary>
  /// Remove all nodes except the ones in the given key set (as well as any edges to
  /// or from the removed nodes)
  /// </summary>
  /// <param name="nodeKeysToKeep"></param>
  public void RemoveOtherNodes(IEnumerable<string> nodeKeysToKeep)
  {
    var keysToRemove = KeySet.CreateDifference(_nodes.Keys, nodeKeysToKeep);
    RemoveNodes(keysToRemove);
  }

  /// <summary>
  /// For each source node key in <paramref name="targetEdgeMap"/> disconnect
  /// all edges from that source except the ones to those targets.
  /// If the source node is missing in <paramref name="targetEdgeMap"/> the behaviour
  /// depends on <paramref name="disconnectMissing"/>.
  /// </summary>
  /// <param name="targetEdgeMap">
  /// The mapping of source nodes to sets of target nodes for edges to keep.
  /// </param>
  /// <param name="disconnectMissing">
  /// If a source node is missing in <paramref name="targetEdgeMap"/>: If this
  /// is true then disconnect all targets. If false then leave the node untouched.
  /// </param>
  public void DisconnectTargetsExcept(
    IReadOnlyDictionary<string, IReadOnlySet<string>> targetEdgeMap,
    bool disconnectMissing)
  {
    foreach(var node in _nodes.Values)
    {
      if(targetEdgeMap.TryGetValue(node.Key, out var targetsToKeep))
      {
        node.DisconnectAllExcept(targetsToKeep);
      }
      else
      {
        if(disconnectMissing)
        {
          node.DisconnectAllExcept([]); // disconnect all
        }
      }
    }
  }

  /// <summary>
  /// Return nodes that have the specified <paramref name="tag"/>
  /// (or keyed tag <paramref name="tagkey"/>::<paramref name="tag"/>)
  /// </summary>
  /// <param name="tag">
  /// The tag to find
  /// </param>
  /// <param name="tagkey">
  /// The key of the keyed tag to look for (the default "" is the key for
  /// 'unkeyed' tags)
  /// </param>
  /// <returns></returns>
  public IEnumerable<GraphNode> FindTaggedNodes(string tag, string tagkey = "")
  {
    return Nodes.Values.Where(node => node.HasTag(tagkey, tag));
  }

  /// <summary>
  /// Return nodes that have the specified <paramref name="tags"/>
  /// </summary>
  /// <param name="tags">
  /// The tags to find
  /// </param>
  /// <param name="tagkey">
  /// The key of the keyed tags to look for (the default "" is the key for
  /// 'unkeyed' tags)
  /// </param>
  /// <returns></returns>
  public IEnumerable<GraphNode> FindTaggedNodes(IEnumerable<string> tags, string tagkey = "")
  {
    return Nodes.Values.Where(node => node.HasAnyTag(tagkey, tags));
  }

  /// <summary>
  /// Serialize the information in this graph into JSON form
  /// </summary>
  /// <returns></returns>
  public JObject Serialize()
  {
    var g = new JObject();
    var nodes = new JObject();
    foreach(var node in _nodes.Values.OrderBy(n => n.Key))
    {
      var nodeObject = node.Serialize();
      var key = node.Key;
      nodeObject.Remove("key");
      nodes[key] = nodeObject;
    }
    g["nodes"] = nodes;
    Metadata.AddToObject(g);
    return g;
  }

  /// <summary>
  /// Serialize this graph to a file
  /// </summary>
  /// <param name="fileName">
  /// The file name to save to
  /// </param>
  public void Serialize(string fileName)
  {
    var o = Serialize();
    var json = JsonConvert.SerializeObject(o, Formatting.Indented);
    File.WriteAllText(fileName, json + Environment.NewLine);
  }

  /// <summary>
  /// Create a new <see cref="Graph"/> by parsing a JSON object of the same
  /// shape as produced by <see cref="Serialize()"/>
  /// </summary>
  /// <param name="o">
  /// The JSON to parse, as a <see cref="JObject"/>
  /// </param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static Graph Deserialize(JObject o)
  {
    var g = new Graph();
    g.Metadata.FillFromObject(o, ["nodes"]);
    var nodesToken = o["nodes"];
    if(nodesToken is JObject nodes)
    {
      // first pass: add actual nodes (not yet edges)
      foreach(var prop in nodes.Properties())
      {
        if(prop.Value is JObject nodeObject)
        {
          var node = g.AddNode(prop.Name);
          // Note that "key" is not actually used, but reserved for potential future use
          node.Metadata.FillFromObject(nodeObject, ["key", "targets"]);
        }
      }
      // second pass: create and connect edges
      foreach(var prop in nodes.Properties())
      {
        if(prop.Value is JObject nodeObject)
        {
          var node = g.Nodes[prop.Name];
          var targetsToken = nodeObject["targets"];
          if(targetsToken is JObject targets)
          {
            foreach(var targetProp in targets.Properties())
            {
              if(targetProp.Value is JObject edgeObject)
              {
                if(!g.Nodes.TryGetValue(targetProp.Name, out var targetNode))
                {
                  throw new InvalidOperationException(
                    $"Missing target node for edge from '{node.Key}' to '{targetProp.Name}'");
                }
                var edge = node.Connect(targetNode);
                edge.Metadata.FillFromObject(edgeObject, []);
              }
            }
          }
        }
      }
    }
    return g;
  }

  /// <summary>
  /// Create a new <see cref="Graph"/> by parsing a JSON object of the same
  /// shape as produced by <see cref="Serialize()"/>
  /// </summary>
  /// <param name="json">
  /// The JSON to parse, as a string
  /// </param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static Graph DeserializeJson(string json)
  {
    var o =
      JsonConvert.DeserializeObject<JObject>(json)
      ?? throw new InvalidOperationException("Invalid JSON");
    return Deserialize(o);
  }

  /// <summary>
  /// Create a new <see cref="Graph"/> by parsing a JSON object of the same
  /// shape as produced by <see cref="Serialize()"/>
  /// </summary>
  /// <param name="file">
  /// The name of the file containing the JSON text to parse
  /// </param>
  /// <returns></returns>
  public static Graph DeserializeFile(string file)
  {
    return DeserializeJson(File.ReadAllText(file));
  }
}
