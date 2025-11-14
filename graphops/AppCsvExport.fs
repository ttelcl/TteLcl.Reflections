module AppCsvExport

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
  NodePropertyColumns: string list
  NodePropertyAuto: bool
}

let private runCsvExport o =
  cp "\frNot yet implemented"
  1

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
    | "-np" :: "*" :: rest ->
      rest |> parseMore {o with NodePropertyAuto = true}
    | "-np" :: propname :: rest ->
      rest |> parseMore {o with NodePropertyColumns = propname :: o.NodePropertyColumns}
    | [] ->
      if o.InputFile |> String.IsNullOrEmpty then
        cp "\foNo input file (\fg-i\fo) given\f0."
        None
      else
        {o with NodePropertyColumns = o.NodePropertyColumns |> List.rev} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parseMore {
    InputFile = null
    NodePropertyColumns = []
    NodePropertyAuto = false
  }
  match oo with
  | Some(o) ->
    o |> runCsvExport
  | None ->
    cp ""
    Usage.usage "csv"
    1


