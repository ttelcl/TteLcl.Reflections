module AppPurify

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Graphs
open TteLcl.Graphs.Analysis

open ColorPrint
open CommonTools

type private Options = {
  InputFile: string
  OutputFile: string
  BreakCircles: bool
  SccMode: bool
}

let private runPurifyScc o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let analyzer = new GraphAnalyzer(graph)
  let sccResult = analyzer.StronglyConnectedComponents()
  let components = sccResult.Components
  let minSize = components |> Seq.map (fun c -> c.Nodes.Count) |> Seq.min
  let maxSize = components |> Seq.map (fun c -> c.Nodes.Count) |> Seq.max
  cp $"SCC analysis found \fb{components.Count}\f0 components, varying in size from \fb{minSize}\f0 to \fb{maxSize}\f0."
  let sccGraph = sccResult.ComponentGraph(graph)
  cp $"  SCC graph initial:  \fb{sccGraph.NodeCount}\f0 nodes, \fc{sccGraph.EdgeCount}\f0 edges, \fy{sccGraph.SeedCount}\f0 seeds, \fo{sccGraph.SinkCount}\f0 sinks"
  let sccAnalyzer = new GraphAnalyzer(sccGraph)
  // No need to break circles, because the SCC graph is guaranteed to be acyclic, so ignore that setting
  let reachMap = sccAnalyzer.GetReachMap(null)
  let purified = reachMap.NotInSelfProjection(sccAnalyzer.TargetEdges)
  let purified = new KeySetMapView(purified)
  sccGraph.DisconnectTargetsExcept(purified, true);
  cp $"  SCC graph purified: \fb{sccGraph.NodeCount}\f0 nodes, \fc{sccGraph.EdgeCount}\f0 edges, \fy{sccGraph.SeedCount}\f0 seeds, \fo{sccGraph.SinkCount}\f0 sinks"
  // Now to construct the purified original
  let pureGraph = new Graph(graph.Metadata)
  // insert nodes in SCC order
  for scc in components do
    for node in scc.Nodes do
      let originalNode = graph.Nodes[node]
      pureGraph.AddNode(node, originalNode.Metadata) |> ignore
  // insert original edges that are still valid
  for scc in components do
    for node in scc.Nodes do
      let sourceNode = graph.Nodes[node]
      let sourceComponent = sccResult.ComponentForNode[node]
      for targetEdge in sourceNode.Targets.Values do
        let targetNode = targetEdge.Target
        let targetName = targetNode.Key
        let targetComponent = sccResult.ComponentForNode[targetName]
        let includeEdge =
          targetComponent.Name = sourceComponent.Name || // preserve ALL intra-component edges
          sccGraph.FindEdge(sourceComponent.Name, targetComponent.Name) <> null
        if includeEdge then
          pureGraph.Connect(node, targetName, targetEdge.Metadata) |> ignore
  cp $"Saving \fg{o.OutputFile}\f0."
  cp $"  (\fb{pureGraph.NodeCount}\f0 nodes, \fc{pureGraph.EdgeCount}\f0 edges, \fy{pureGraph.SeedCount}\f0 seeds, \fo{pureGraph.SinkCount}\f0 sinks)"
  pureGraph.Serialize(o.OutputFile + ".tmp")
  o.OutputFile |> finishFile
  0

let private runPurifyClassic o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let analyzer = new GraphAnalyzer(graph)
  let seeds = String.Join("\f0,\fy ", analyzer.Seeds)
  cp $"  Seed nodes: \fy{seeds}\f0."
  let sinks = String.Join("\f0,\fo ", analyzer.Sinks)
  cp $"  Sink nodes: \fo{sinks}\f0."
  let circles = if o.BreakCircles then new KeySetMap() else null
  let reachMap = analyzer.GetReachMap(circles)
  if circles <> null then
    let circleCount = circles.PairCount
    if circleCount = 0 then
      cp "Circular dependencies: \fg0\f0."
    else
      cp $"\fyCircular dependencies\f0: \fr{circleCount}\f0:"
      for kvp in circles do
        for target in kvp.Value do
          cp $"    \fo{kvp.Key}\f0 -> \fy{target}\f0."
  let purified = reachMap.NotInSelfProjection(analyzer.TargetEdges)
  let purified = new KeySetMapView(purified)
  graph.DisconnectTargetsExcept(purified, true);
  if circles <> null then
    cp "Patching back and tagging circular edges"
    let circleEdges = graph.ConnectMany(circles) |> Seq.toArray
    for circleEdge in circleEdges do
      let metadata = circleEdge.Metadata
      metadata.Tags.Add("cyclelink") |> ignore
      metadata.SetProperty("color", "#ff3333")
  cp $"Saving \fg{o.OutputFile}\f0."
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  graph.Serialize(o.OutputFile + ".tmp")
  o.OutputFile |> finishFile
  0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "--help" :: _
    | "-h" :: _ ->
      None
    | "-i" :: file :: rest ->
      rest |> parseMore {o with InputFile = file}
    | "-o" :: file :: rest ->
      rest |> parseMore {o with OutputFile = file}
    | "-breakcircles" :: rest ->
      rest |> parseMore {o with BreakCircles = true}
    | "-scc" :: rest ->
      rest |> parseMore {o with SccMode = true}
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      else
        let missingOutputName = o.OutputFile |> String.IsNullOrEmpty
        let o =
          if o.SccMode then
            {o with OutputFile = Graph.DeriveMissingName(o.InputFile, ".sccpure.graph.json", o.OutputFile)}
          else
            {o with OutputFile = Graph.DeriveMissingName(o.InputFile, ".pure.graph.json", o.OutputFile)}
        if o.OutputFile |> String.IsNullOrEmpty then
          let shortInput = Path.GetFileName(o.InputFile)
          cp $"\foNo output file (\fg-o\f0) given, and cannot derive the output name from \f0'{shortInput}\f0'"
          None
        else
          if missingOutputName then
            cp $"Using output name \fc{o.OutputFile}\f0."
          o |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parseMore {
    InputFile = null
    OutputFile = null
    BreakCircles = false
    SccMode = false
  }
  match oo with
  | Some(o) ->
    if o.SccMode then
      o |> runPurifyScc
    else
      o |> runPurifyClassic
  | None ->
    cp ""
    Usage.usage "purify"
    1
