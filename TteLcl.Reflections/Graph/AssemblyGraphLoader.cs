/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using TteLcl.Graphs.Analysis;

namespace TteLcl.Reflections.Graph;

/// <summary>
/// Helper class to build an assembly graph. This class temporarily
/// holds the <see cref="AssemblyFileCollection"/> that maps assemblies
/// to tags and the disposable <see cref="MetadataLoadContext"/> caching
/// and loading assemblies
/// </summary>
public class AssemblyGraphLoader
{
  /// <summary>
  /// Create a new AssemblyGraphLoader
  /// </summary>
  /// <param name="registry">
  /// The <see cref="AssemblyFileCollection"/> storing candidate
  /// assembly file name and their tags
  /// </param>
  /// <param name="loadContext">
  /// The <see cref="MetadataLoadContext"/> caching the loaded assemblies.
  /// </param>
  /// <param name="graph">
  /// A graph to continue building, or null to start a new graph
  /// </param>
  public AssemblyGraphLoader(
    AssemblyFileCollection registry,
    MetadataLoadContext loadContext,
    AssemblyGraph? graph = null)
  {
    Graph = graph ?? new AssemblyGraph([], []);
    Registry = registry;
    LoadContext = loadContext;
  }

  /// <summary>
  /// The graph that is being built by this builder
  /// </summary>
  public AssemblyGraph Graph { get; }

  /// <summary>
  /// The registry of candidate assembly files that can be loaded
  /// </summary>
  public AssemblyFileCollection Registry { get; }

  /// <summary>
  /// The load context caching actually loaded assemblies
  /// </summary>
  public MetadataLoadContext LoadContext { get; }

  /// <summary>
  /// Add an assembly node to this graph, without connecting it to other
  /// assemblies, but making it available to be connected. If already present,
  /// this just looks up the existing <see cref="AssemblyNode"/> instance.
  /// </summary>
  /// <param name="assembly">
  /// The assembly to add or look up
  /// </param>
  /// <param name="node">
  /// The node that was created or looked up
  /// </param>
  /// <returns>
  /// True if a new node was created, false if it was already present
  /// </returns>
  public bool AddAssembly(Assembly assembly, out AssemblyNode node)
  {
    return Graph.AddNode(assembly, Registry, out node);
  }

  /// <summary>
  /// Create an initial pending assembly queue
  /// </summary>
  /// <param name="seeds">
  /// The initial seed assemblies (that are already loaded in <see cref="LoadContext"/>)
  /// </param>
  /// <returns></returns>
  public Queue<AssemblyNode> SeedAssemblies(IEnumerable<Assembly> seeds)
  {
    var queue = new Queue<AssemblyNode>();
    foreach(var assembly in seeds)
    {
      if(AddAssembly(assembly, out var node))
      {
        queue.Enqueue(node);
      }
    }
    return queue;
  }

  /// <summary>
  /// Connect the next node in <paramref name="pending"/>.
  /// </summary>
  /// <param name="pending">
  /// The queue of nodes remaining to be connected. One node is taken
  /// from it and zero or more are added to it. Connecting is done once
  /// this queue becomes empty.
  /// </param>
  /// <returns>
  /// The number of edges added, or 0 if the queue is empty. A return value
  /// of 0 does NOT necessarily mean that connecting is done.
  /// </returns>
  public int ConnectNext(Queue<AssemblyNode> pending)
  {
    if(pending.TryDequeue(out var node))
    {
      return ConnectNode(node, pending);
    }
    return 0;
  }

  /// <summary>
  /// Connect the <paramref name="node"/> to the assemblies it depends
  /// on, inserting missing nodes and adding assembly edges. Newly inserted
  /// nodes are added to <paramref name="pendingNodes"/>.
  /// </summary>
  /// <param name="node">
  /// The node to connect to its dependencies
  /// </param>
  /// <param name="pendingNodes">
  /// A queue of pending assembly nodes still waiting to be connected. Newly
  /// inserted nodes are inserted in this queue.
  /// </param>
  /// <returns>
  /// The number of edges added
  /// </returns>
  public int ConnectNode(
    AssemblyNode node, Queue<AssemblyNode> pendingNodes)
  {
    if(!node.Available)
    {
      // skip 'ghost' nodes
      return 0;
    }
    var nodeName = node.AssemblyName;
    var assembly = LoadContext.LoadFromAssemblyName(nodeName); // retrieve already loaded assembly
    var dependencyNames = assembly.GetReferencedAssemblies();
    var dependentName = node.FullName;
    var count = 0;
    // Trace.TraceInformation($"Connecting {dependentName} ... ");
    foreach(var dependencyName in dependencyNames)
    {
      if(nodeName == dependencyName || nodeName.Name == dependencyName.Name)
      {
        // Odd thing that may happen for core assemblies like mscorlib, IIRC. Or it did in the early days of .NET
        // Observed in System.Printing (of all places)
        Trace.TraceInformation(
          $"Bypassing assembly self reference in '{nodeName}'");
        continue;
      }
      if(String.IsNullOrEmpty(dependencyName.Name))
      {
        // another oddball case
        Trace.TraceInformation(
          $"Bypassing nameless assembly '{nodeName}'");
        continue;
      }
      Assembly dependency;
      try
      {
        dependency = LoadContext.LoadFromAssemblyName(dependencyName); // possibly a true load
      }
      catch(FileNotFoundException ex)
      {
        Trace.TraceInformation(
          $"Error loading dependency '{dependencyName}' of assembly '{nodeName}' ({assembly.Location}). ({ex.Message})");
        var missingNode = Graph.AddMissingNode(dependencyName);
        var ghostEdge = new AssemblyEdge(dependentName, missingNode.FullName, []);
        if(Graph.AddEdge(ghostEdge))
        {
          count++;
        }
        continue;
      }
      var isnew = AddAssembly(dependency, out var dependencyNode);
      // Trace.TraceInformation($" ({isnew})  -->  {dependencyName} ({dependency.Location}, {dependency.FullName})");
      if(isnew)
      {
        pendingNodes.Enqueue(dependencyNode);
      }
      // Initialize without tags
      var circular = IsKnownCircularDependency(nodeName, dependencyName);
      if(circular)
      {
        Trace.TraceInformation(
          $"Ignoring known circular dependency {nodeName} -> {dependencyName}");
        continue;
      }
      var edge = new AssemblyEdge(dependentName, dependencyNode.FullName, []);
      if(Graph.AddEdge(edge))
      {
        count++;
      }
      else
      {
        Trace.TraceInformation($"(ignoring aliased / duplicate edge {node.ShortName} -> {dependencyNode.ShortName})");
      }
    }
    return count;
  }

  private static readonly KeySetMap KnownCircularDependencies = new() {
    { "System", new() { "System.Configuration", "System.Xml" } },
    { "System.Xml", new() { "System.Configuration" } },
    { "System.Data.SqlXml", new() { "System.Xml" } },
    { "System.Deployment", new() { "System.Windows.Forms" } },
    { "PresentationFramework", new() { "ReachFramework" } },
    { "PresentationUI", new() { "PresentationFramework", "ReachFramework", "System.Printing" } },
    { "ReachFramework", new() { "System.Printing" } },
    { "System.Printing", new() { /*"System.Printing", self-reference!! */ "PresentationFramework", "ReachFramework" } },
    { "System.Data", new() { "System.EnterpriseServices", "System.Runtime.Caching" } },
    { "System.Transactions", new() { "System.EnterpriseServices" } },
    { "System.Web", new() { "System.Design", "System.EnterpriseServices", "System.Web.Services" } },
    { "System.ServiceModel", new() { "System.ServiceModel.Activation" } },
    { "Microsoft.Transactions.Bridge", new() { "System.ServiceModel" } },
    { "System.Data.Services.Design", new() { "System.Web.Extensions" } },
  };

  /// <summary>
  /// Check if the edge is a known circular dependency and if ignored would
  /// break that circle.
  /// </summary>
  /// <param name="dependent">
  /// The assembly that depends on <paramref name="dependency"/>
  /// </param>
  /// <param name="dependency">
  /// The assembly that <paramref name="dependent"/> depends on
  /// </param>
  /// <returns></returns>
  public static bool IsKnownCircularDependency(
    AssemblyName dependent, AssemblyName dependency)
  {
    var dependentName = dependent.Name!;
    var dependencyName = dependency.Name!;
    return
      KnownCircularDependencies.TryGetValue(dependentName, out var badDependencies)
      ? badDependencies.Contains(dependencyName)
      : false;
  }
}
