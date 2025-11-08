module AppFilter
open System
open System.IO
open System.Reflection

open Newtonsoft.Json

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
  let taggedNodes = graph.FindTaggedNodes(o.Tags, "") |> Seq.toArray
  if taggedNodes.Length = 0 then
    cp "\foThe tags did not match any nodes. Aborting\f0."
    1
  else
    cp $"Found \fb{taggedNodes.Length}\f0 matching nodes."
    taggedNodes |> Seq.map (fun n -> n.Key) |> graph.RemoveNodes
    do
      cp $"Saving \fg{o.OutputFile}\f0."
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
    | "-include" :: rest ->
      rest |> parseMore {o with Mode = Include}
    | "-exclude" :: rest ->
      rest |> parseMore {o with Mode = Exclude}
    | "-n" :: tag :: rest ->
      rest |> parseMore {o with Tags = tag :: o.Tags}
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

