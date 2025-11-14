/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Graphs.Dot;

/// <summary>
/// Utility to help write a GraphViz *.dot file.
/// This does not support everything, just what is needed in the scope
/// of this library.
/// </summary>
public class DotFileWriter: IDisposable
{
  private TextWriter? _writer;
  private readonly HashSet<string> _knownNodes;
  private string _indent;
  private int _indentLevel;

  /// <summary>
  /// Create a new DotFileWriter
  /// </summary>
  public DotFileWriter(
    string fileName,
    bool directed,
    bool horizontal = false,
    string? graphId = null)
  {
    _indentLevel = 0;
    _indent = string.Empty;
    _knownNodes = new HashSet<string>(StringComparer.Ordinal);
    _writer = File.CreateText(fileName);
    Directed = directed;
    Writer.Write(Directed ? "digraph" : "graph");
    if(graphId != null)
    {
      Writer.Write($" \"{graphId}\"");
    }
    Writer.WriteLine(" {");
    IncreaseIndent();
    WriteProperty("rankdir", horizontal ? "LR" : "TB");
  }

  /// <summary>
  /// Whether this is a directed or undirected graph
  /// </summary>
  public bool Directed { get; }

  /// <summary>
  /// Add a node. Shorthand for calling <see cref="StartNode"/> and immediately
  /// disposing the returned scope marker.
  /// </summary>
  public void AddNode(
    string label, // Must not include quotes.
    IEnumerable<string> sublabels, // written as italic lines below main label
    string? shape = "box",
    string? style = "filled",
    string? color = null, // background color, omitted if null
    string? id = null) // defaults to label
  {
    StartNode(label, sublabels, shape, style, color, id).Dispose();
  }

  /// <summary>
  /// Start a new node and add common properties. You can continue to add
  /// more properties to it using <see cref="WriteProperty(string, string)"/>,
  /// until you dispose the returned scope marker.
  /// </summary>
  /// <param name="label">
  /// The short label text (must not include quotes).
  /// </param>
  /// <param name="sublabels">
  /// Additional text written as separate lines in italics below the main label.
  /// </param>
  /// <param name="shape">
  /// The shape to use. Defaults to "box". Not written if null.
  /// </param>
  /// <param name="style">
  /// The style to use. Defaults to "filled". Not written if null
  /// </param>
  /// <param name="color">
  /// The fill color to use. Omitted if null
  /// </param>
  /// <param name="id">
  /// Node id. Defaults to <paramref name="label"/>
  /// </param>
  /// <returns>
  /// A scope marker. Disposing it finishes the edge.
  /// </returns>
  /// <exception cref="InvalidOperationException"></exception>
  public IDisposable StartNode(
    string label, // Must not include quotes.
    IEnumerable<string> sublabels, // written as italic lines below main label
    string? shape = "box",
    string? style = "filled",
    string? color = null, // background color, omitted if null
    string? id = null) // defaults to label
  {
    if(String.IsNullOrEmpty(label))
    {
      throw new InvalidOperationException(
        "Node labels must not be empty");
    }
    id ??= label;
    if(_knownNodes.Contains(id))
    {
      throw new InvalidOperationException(
        $"Duplicate node declaration '{id}'");
    }
    _knownNodes.Add(id);
    Writer.WriteLine($"{_indent}\"{id}\" ["); // Always quote, to err on the safe side
    var scope = new Scope(this, "]");
    if(!String.IsNullOrEmpty(shape))
    {
      WriteProperty("shape", shape);
    }
    var lb = new StringBuilder();
    lb.Append($"<{label}<BR ALIGN=\"LEFT\"/>");
    foreach(var sublabel in sublabels)
    {
      if(!String.IsNullOrEmpty(sublabel))
      {
        lb.Append($"<I>{sublabel}</I><BR ALIGN=\"LEFT\"/>");
      }
    }
    lb.Append('>');
    var fullLabel = lb.ToString();
    WriteProperty("label", fullLabel);
    if(!String.IsNullOrEmpty(style))
    {
      WriteProperty("style", style);
    }
    if(!String.IsNullOrEmpty(color))
    {
      WriteProperty("fillcolor", color);
    }
    return scope;
  }

  /// <summary>
  /// Add a fully completed edge between two previously added nodes.
  /// Shorthand for calling <see cref="StartEdge"/> and immediately
  /// disposing the returned scope marker.
  /// </summary>
  public void AddEdge(
    string nodeId1,
    string nodeId2,
    bool weightless = false,
    string? color = null)
  {
    StartEdge(nodeId1, nodeId2, weightless, color).Dispose();
  }

  /// <summary>
  /// Start writing a new edge between two previously added nodes. Use
  /// <see cref="WriteProperty(string, string)"/> to add additional properties.
  /// When completed, dispose the returned scope marker
  /// </summary>
  /// <param name="nodeId1">
  /// ID of the first node
  /// </param>
  /// <param name="nodeId2">
  /// ID of the second node
  /// </param>
  /// <param name="weightless">
  /// If true, an attribute 'weight=0' will be added
  /// </param>
  /// <param name="color">
  /// If not null or empty, a 'color={color}' attribute will be added
  /// </param>
  /// <returns>
  /// A scope marker. Disposing it finishes the edge.
  /// </returns>
  /// <exception cref="ArgumentException"></exception>
  public IDisposable StartEdge(
    string nodeId1,
    string nodeId2,
    bool weightless = false,
    string? color = null)
  {
    if(!_knownNodes.Contains(nodeId1))
    {
      throw new ArgumentException(
        $"Edge source was not added as node yet: {nodeId1}");
    }
    if(!_knownNodes.Contains(nodeId2))
    {
      throw new ArgumentException(
        $"Edge target was not added as node yet: {nodeId2}");
    }
    var edgeop = Directed ? "->" : "--";
    Writer.WriteLine($"{_indent}\"{nodeId1}\" {edgeop} \"{nodeId2}\" [");
    var scope = new Scope(this, "]");
    if(weightless)
    {
      WriteProperty("weight", "0");
    }
    if(!String.IsNullOrEmpty(color))
    {
      WriteProperty("color", color);
    }
    return scope;
  }

  /// <summary>
  /// Start a new subgraph or cluster. Subsequent nodes and edges are added to the
  /// subgraph, until it is closed by disposing the returned scope marker
  /// </summary>
  /// <param name="id">
  /// The ID of the subgraph. If null, an anonymous subgraph is started. 
  /// If this starts with the text "cluster", dot will treat this as a cluster,
  /// modifying its layout algorithm.
  /// </param>
  /// <param name="rank">
  /// Sets the 'rank' property if not null. See https://graphviz.org/docs/attrs/rank/
  /// for documentation (known values are "same", "min", "source", "max", "sink")
  /// </param>
  /// <returns>
  /// A scope marker. Disposing it finishes the edge.
  /// </returns>
  public IDisposable StartSubGraph(
    string? id = null,
    string? rank = null)
  {
    if(!String.IsNullOrEmpty(id))
    {
      Writer.WriteLine($"{_indent}subgraph \"{id}\" {{");
    }
    else
    {
      Writer.WriteLine($"{_indent}{{");
    }
    var scope = new Scope(this, "}");
    if(!String.IsNullOrEmpty(rank))
    {
      WriteProperty("rank", rank);
    }
    return scope;
  }

  /// <summary>
  /// Write a property to the current item
  /// </summary>
  /// <param name="key">
  /// The key. It is assumed there is no need to quote this
  /// </param>
  /// <param name="value">
  /// The value. This will be quoted, unless it starts with "&lt;" and ends with ">"
  /// </param>
  public void WriteProperty(string key, string value)
  {
    ObjectDisposedException.ThrowIf(_writer == null, nameof(Writer));
    if(value.StartsWith('<') &&  value.EndsWith('>'))
    {
      Writer.WriteLine($"{_indent}{key}={value}");
    }
    else
    {
      Writer.WriteLine($"{_indent}{key}=\"{value}\"");
    }
  }

  private void SetIndent(int level)
  {
    if(level < 0)
    {
      level = 0;
    }
    _indentLevel = level;
    _indent = new string(' ', 2 *  level);
  }

  private void IncreaseIndent()
  {
    SetIndent(_indentLevel + 1);
  }

  private void DecreaseIndent()
  {
    SetIndent(_indentLevel - 1);
  }

  /// <summary>
  /// The underlying text writer.
  /// </summary>
  public TextWriter Writer {
    get {
      return
        _writer == null
        ? throw new ObjectDisposedException(nameof(Writer))
        : _writer;
    }
  }

  /// <summary>
  /// Clean up
  /// </summary>
  public void Dispose()
  {
    if(_writer != null)
    {
      DecreaseIndent();
      Writer.WriteLine("}");
      _writer.Dispose();
      _writer = null;
    }
  }

  private sealed class Scope: IDisposable
  {
    private readonly DotFileWriter _owner;
    private bool _closed;

    public Scope(DotFileWriter owner, string terminal)
    {
      _owner = owner;
      _closed = false;
      Terminal = terminal;
      _owner.IncreaseIndent();
    }

    public string Terminal { get; }

    public void Dispose()
    {
      if(!_closed)
      {
        _closed = true;
        _owner.DecreaseIndent();
        if(!String.IsNullOrEmpty(Terminal))
        {
          _owner.Writer.WriteLine($"{_owner._indent}{Terminal}");
        }
      }
    }

    //.
  }
}
