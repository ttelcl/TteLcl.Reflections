module AppSuper

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Graphs
open TteLcl.Graphs.Analysis

open ColorPrint
open CommonTools

type private SuperKey =
  | Propname of string

type private Options = {
  InputFile: string
  OutputFile: string
  Key: SuperKey
}

let private runSuper o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let groupMap =
    match o.Key with
    | SuperKey.Propname(property) ->
      cp $"Grouping nodes by the value of their property '\fg{property}\f0'"
      graph.ClassifyNodes(property, StringComparer.OrdinalIgnoreCase)
  if groupMap.Count = 0 then
    cp $"\frError: No nodes found\f0."
    1
  elif groupMap.Count = 1 then
    let value = groupMap.Keys |> Seq.head
    cp $"\frError: All nodes have the same value\f0: '\fy{value}\f0'"
    1
  else
    cp "Found the following groups:"
    for kvp in groupMap |> Seq.sortBy (fun kvp -> kvp.Key) do
      cp $"\fb{kvp.Value.Count,4}\f0 '{kvp.Key}\f0'"
    let classifier = NodeMapClassifier.FromNodeClassificationMap(groupMap)
    let superGraph = graph.SuperGraph(classifier)
    do
      cp $"Saving \fg{o.OutputFile}\f0."
      cp $"  (\fb{superGraph.NodeCount}\f0 nodes, \fc{superGraph.EdgeCount}\f0 edges, \fy{superGraph.SeedCount}\f0 seeds, \fo{superGraph.SinkCount}\f0 sinks)"
      o.OutputFile + ".tmp" |> superGraph.Serialize
    o.OutputFile |> finishFile
    0

let run args =
  let getDefaultExtension o =
    match o.Key with
    | SuperKey.Propname(prop) ->
      $".property--{prop}.graph.json"
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
    | "-prop" :: name :: rest ->
      rest |> parseMore {o with Key = SuperKey.Propname(name)}
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      else
        let missingOutputName = o.OutputFile |> String.IsNullOrEmpty
        let defaultExtension = o |> getDefaultExtension
        let o = {o with OutputFile = Graph.DeriveMissingName(o.InputFile, defaultExtension, o.OutputFile)}
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
    Key = SuperKey.Propname("category")
  }
  match oo with
  | Some(o) ->
    o |> runSuper
  | None ->
    cp ""
    Usage.usage "supergraph"
    1


