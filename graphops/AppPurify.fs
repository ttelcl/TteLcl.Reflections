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
}

let private runPurify o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let analyzer = new GraphAnalyzer(graph)
  let seeds = String.Join("\f0,\fy ", analyzer.Seeds)
  cp $"  Seed nodes: \fy{seeds}\f0."
  let sinks = String.Join("\f0,\fo ", analyzer.Sinks)
  cp $"  Sink nodes: \fo{sinks}\f0."
  let reachMap = analyzer.GetReachMap(o.BreakCircles)
  let purified = reachMap.NotInSelfProjection(analyzer.TargetEdges)
  let purified = new KeySetMapView(purified)
  graph.DisconnectTargetsExcept(purified, true);
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
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      else
        let missingOutputName = o.OutputFile |> String.IsNullOrEmpty
        let o = {o with OutputFile = Graph.DeriveMissingName(o.InputFile, ".pure.graph.json", o.OutputFile)}
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
  }
  match oo with
  | Some(o) ->
    o |> runPurify
  | None ->
    cp ""
    Usage.usage "purify"
    1
