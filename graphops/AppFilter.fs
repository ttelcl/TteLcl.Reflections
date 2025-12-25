module AppFilter

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Csv

open TteLcl.Graphs
open TteLcl.Graphs.Analysis

open ColorPrint
open CommonTools

type private FilterMode =
  | Undefined
  | Include
  | Exclude

type private Options = {
  InputFile: string
  OutputFile: string
  Mode: FilterMode
  Tags: string list
}

let private runFilter o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let taggedNodes = graph.FindTaggedNodes(o.Tags) |> Seq.toArray
  if taggedNodes.Length = 0 then
    cp "\foThe tags did not match any nodes. Aborting\f0."
    1
  else
    match o.Mode with
    | Exclude ->
      cp $"Found \fb{taggedNodes.Length}\f0 matching nodes to \fgexclude\f0."
      taggedNodes |> Seq.map (fun n -> n.Key) |> graph.RemoveNodes
      do
        cp $"Saving \fg{o.OutputFile}\f0."
        cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
        o.OutputFile + ".tmp" |> graph.Serialize
      o.OutputFile |> finishFile
      0
    | Include ->
      cp $"Found \fb{taggedNodes.Length}\f0 matching nodes to \fginclude\f0."
      taggedNodes |> Seq.map (fun n -> n.Key) |> graph.RemoveOtherNodes
      do
        cp $"Saving \fg{o.OutputFile}\f0."
        cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
        o.OutputFile + ".tmp" |> graph.Serialize
      o.OutputFile |> finishFile
      0
    | Undefined ->
      cp "\frError\fo: Missing \fg-exclude\fo or \fg-include\f0."
      1

let private loadTags (tagfile:string) =
  use reader = new CsvReader(tagfile, false, true)
  let tagCell = reader.GetColumn("tag")
  let categoryCell = reader.GetColumn("category")
  let firstCell = reader.GetColumn(0)
  // The loaded row is still the header
  if tagCell.Get() <> "tag" then
    failwith $"Expecting '{tagfile}' to contain a 'tag' column"
  // the category is optional, so don't check it
  let tags =
    seq {
      while reader.Next() do
        let tag = tagCell.Get()
        let category = categoryCell.Get()
        let tag =
          if category |> String.IsNullOrEmpty then
            tag
          else
            $"{category}::{tag}"
        let firstColumn = firstCell.Get()
        if firstColumn.StartsWith('#') |> not then
          yield tag
        else
          if verbose then
            cp $"Ignoring (excluding) commented out tag \fy{tag}\f0."
    }
  tags |> Seq.toList

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
    | "-include" :: rest ->
      rest |> parseMore {o with Mode = Include}
    | "-exclude" :: rest ->
      rest |> parseMore {o with Mode = Exclude}
    | "-n" :: tag :: rest ->
      rest |> parseMore {o with Tags = tag :: o.Tags}
    | "-nf" :: tagfile :: rest ->
      let tags = tagfile |> loadTags
      let tags = tags @ o.Tags
      rest |> parseMore {o with Tags = tags}
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      elif o.OutputFile |> String.IsNullOrEmpty then
        cp "\foNo output file (\fg-o\fo) given\f0."
        None
      elif o.Mode = Undefined then
        cp "\foNo operation mode (\fg-include\fo or \fg-exclude\fo) given\f0."
        None
      elif o.Tags |> List.isEmpty then
        cp "\foNo ytags (\fg-n\fo) given\f0."
        None
      else
        {o with Tags = o.Tags |> List.rev} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parseMore {
    InputFile = null
    OutputFile = null
    Mode = Undefined
    Tags = []
  }
  match oo with
  | Some(o) ->
    o |> runFilter
  | None ->
    cp ""
    Usage.usage "filter"
    1

