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
  let reachMap = analyzer.GetReachMap()
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
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      elif o.OutputFile |> String.IsNullOrEmpty then
        cp "\foNo output file (\fg-o\fo) given\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parseMore {
    InputFile = null
    OutputFile = null
  }
  match oo with
  | Some(o) ->
    o |> runPurify
  | None ->
    cp ""
    Usage.usage "purify"
    1
