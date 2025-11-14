module AppTags

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
}

let private runTags o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let seeds = String.Join("\f0,\fy ", graph.SeedNodes |> Seq.map (fun n -> n.Key))
  cp $"  Seed nodes: \fy{seeds}\f0."
  let sinks = String.Join("\f0,\fo ", graph.SinkNodes |> Seq.map (fun n -> n.Key))
  cp $"  Sink nodes: \fo{sinks}\f0."
  let nodeTags =
    graph.Nodes.Values
    |> Seq.collect (fun n -> n.Metadata.Tags)
    |> Seq.countBy id
    |> Seq.sortBy (fun (tag, _) -> tag)
    |> Seq.toArray
  cp $"Found \fb{nodeTags.Length}\f0 distinct unkeyed node tags:"
  for (tag, count) in nodeTags do
    cp $"\fb{count,4}  \f0'\fg{tag}\f0'"
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
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parseMore {
    InputFile = null
  }
  match oo with
  | Some(o) ->
    o |> runTags
  | None ->
    cp ""
    Usage.usage "tags"
    1

