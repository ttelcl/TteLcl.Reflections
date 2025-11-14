module AppPrune

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Graphs
open TteLcl.Graphs.Analysis

open ColorPrint
open CommonTools

type private PruneTarget =
  | AllEdgesTo of string
  | AllEdgesFrom of string
  | EdgeBetween of (string * string)
  | Node of string

type private Options = {
  InputFile: string
  OutputFile: string
  Targets: PruneTarget list
}

let private runPrune o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  let nodeCountBefore = graph.NodeCount
  let edgeCountBefore = graph.EdgeCount
  cp $"  (\fb{nodeCountBefore}\f0 nodes, \fc{edgeCountBefore}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  for target in o.Targets do
    match target with
    | Node(nodeKey) ->
      graph.RemoveNodes([nodeKey])
    | EdgeBetween(node1, node2) ->
      graph.Disconnect(node1, node2) |> ignore
    | AllEdgesTo(node) ->
      graph.DisconnectAllSources(node) |> ignore
    | AllEdgesFrom(node) ->
      graph.DisconnectAllTargets(node) |> ignore
  let nodesRemoved = nodeCountBefore - graph.NodeCount
  let edgesRemoved = edgeCountBefore - graph.EdgeCount
  cp $"Removed \fb{edgesRemoved}\f0 edges and \fb{nodesRemoved}\f0 nodes."
  if edgesRemoved = 0 && nodesRemoved = 0 then
    cp "\foNothing removed - \fynot saving anything\f0."
    1
  else
    do
      cp $"Saving \fg{o.OutputFile}\f0."
      cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
      o.OutputFile + ".tmp" |> graph.Serialize
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
    | "-e" :: "*" :: node2 :: rest ->
      rest |> parseMore {o with Targets = AllEdgesTo(node2) :: o.Targets}
    | "-e" :: node1 :: "*" :: rest ->
      rest |> parseMore {o with Targets = AllEdgesFrom(node1) :: o.Targets}
    | "-e" :: node1 :: node2 :: rest ->
      rest |> parseMore {o with Targets = EdgeBetween(node1, node2) :: o.Targets}
    | "-n" :: node :: rest ->
      rest |> parseMore {o with Targets = Node(node) :: o.Targets}
    | [] ->
      let o = {o with Targets = o.Targets |> List.rev}
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      elif o.Targets |> List.isEmpty then
        cp "\foNo targets (\fg-e\fo or \fg-n\fo) given\f0."
        None
      else
        let missingOutputName = o.OutputFile |> String.IsNullOrEmpty
        let o = {o with OutputFile = Graph.DeriveMissingName(o.InputFile, ".pruned.graph.json", o.OutputFile)}
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
    Targets = []
  }
  match oo with
  | Some(o) ->
    o |> runPrune
  | None ->
    cp ""
    Usage.usage "prune"
    1

