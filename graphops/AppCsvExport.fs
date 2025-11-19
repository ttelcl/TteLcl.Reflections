module AppCsvExport

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Csv
open TteLcl.Csv.Core

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
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let allPropertyNames = graph.Nodes.Values.AllPropertyNames()
  let nodePropertyNames =
    if o.NodePropertyAuto then
      if o.NodePropertyColumns.Length > 0 then
        cp "\foWarning: merging of \fg-np *\fo and explicit \fg-np\f0 options is NYI\f0!"
      allPropertyNames |> Seq.toList
    else
      o.NodePropertyColumns
  let nodesFileName = Graph.DeriveMissingName(o.InputFile, ".nodes.csv")
  do
    cp $"Saving \fg{nodesFileName}\f0."
    let builder = new CsvWriteRowBuilder()
    let nameCell = builder.AddCell("name")
    let kindCell = builder.AddCell("kind")
    let propCells = nodePropertyNames |> List.map (fun propName -> builder.AddCell(propName))
    let rowBuffer = builder.Build()
    use cw = new CsvRawWriter(nodesFileName+".tmp")
    rowBuffer |> cw.WriteHeader
    for node in graph.Nodes.Values do
      node.Key |> nameCell.Set
      node.Kind.ToString() |> kindCell.Set
      let metadata = node.Metadata
      for propCell in propCells do
        propCell.Name |> metadata.GetPropertyOrDefault |> propCell.Set
      rowBuffer |> cw.WriteRow
    ()
  nodesFileName |> finishFile
  
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


