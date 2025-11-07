using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
  /// <param name="metdata">
  /// If provided: the metadata to copy into this new graph object
  /// </param>
  public Graph(
    Metadata? metdata = null)
  {
    _nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
    Metadata = new Metadata();
    if(metdata != null)
    {
      Metadata.Import(metdata);
    }
  }

  /// <inheritdoc/>
  public Metadata Metadata { get; }

  /// <summary>
  /// The collection of nodes in this graph
  /// </summary>
  public IReadOnlyDictionary<string, GraphNode> Nodes => _nodes;

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
  /// Create a new <see cref="Graph"/> by parsing a JSON object of the same
  /// shape as produced by <see cref="Serialize"/>
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
  /// shape as produced by <see cref="Serialize"/>
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
  /// shape as produced by <see cref="Serialize"/>
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
