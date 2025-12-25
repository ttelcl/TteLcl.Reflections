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
  PartitionInOutProp: string
}

let private emitEdgesFile o (graph:Graph) =
  let fileName = Graph.DeriveMissingName(o.InputFile, ".edges.csv")
  do
    let allPropertyNames = graph.Edges.AllPropertyNames() |> Seq.toList
    let nodePropNames = o.NodePropertyColumns
    let builder = new CsvWriteRowBuilder()
    let fromCell = builder.AddCell("from")
    let toCell = builder.AddCell("to")
    let edgePropCells =
      allPropertyNames
      |> List.map (fun propName -> builder.AddCell(propName))
    if allPropertyNames |> List.isEmpty |> not then
      let epropNamesText =
        "\fo|\fy" + String.Join("\fo|\fy", allPropertyNames) + "\fo|\f0"
      cp $"Including edge property columns:      {epropNamesText}"
    let nodePropInfos =
      nodePropNames
      |> List.map (fun propName -> (propName, $"{propName}(from)" |> builder.AddCell, $"{propName}(to)" |> builder.AddCell))
    let edgeNodePropNames =
      nodePropInfos
      |> List.map (fun (_, fpc, tpc) -> [fpc.Name; tpc.Name])
      |> List.concat
    if edgeNodePropNames |> List.isEmpty |> not then
      let enpropNamesText =
        "\fo|\fc" + String.Join("\fo|\fc", edgeNodePropNames) + "\fo|\f0"
      cp $"Including edge-node property columns: {enpropNamesText}"
    let rowBuffer = builder.Build()
    cp $"Saving \fg{fileName}\f0."
    use cw = new CsvRawWriter(fileName+".tmp")
    rowBuffer |> cw.WriteHeader
    for edge in graph.Edges do
      let metadata = edge.Metadata
      edge.Source.Key |> fromCell.Set
      edge.Target.Key |> toCell.Set
      for propCell in edgePropCells do
        propCell.Name |> metadata.GetPropertyOrDefault |> propCell.Set
      for propName, fromPropCell, toPropCell in nodePropInfos do
        propName |> edge.Source.Metadata.GetPropertyOrDefault |> fromPropCell.Set
        propName |> edge.Target.Metadata.GetPropertyOrDefault |> toPropCell.Set
      rowBuffer |> cw.WriteRow
  fileName |> finishFile

let private emitNodeTagsFile o (graph:Graph) =
  let fileName = Graph.DeriveMissingName(o.InputFile, ".node.tags.csv")
  do
    let builder = new CsvWriteRowBuilder()
    let nodeCell = builder.AddCell("node")
    let categoryCell = builder.AddCell("category")
    let tagCell = builder.AddCell("tag")
    cp $"Saving \fg{fileName}\f0."
    use cw = new CsvRawWriter(fileName+".tmp")
    let rowBuffer = builder.Build()
    rowBuffer |> cw.WriteHeader
    for node in graph.Nodes.Values do
      let metadata = node.Metadata
      for category in metadata.TagKeys do
        let ok, tags = category |> metadata.TryGetTags
        if ok then
          for tag in tags do
            node.Key |> nodeCell.Set
            category |> categoryCell.Set
            tag |> tagCell.Set
            rowBuffer |> cw.WriteRow
  fileName |> finishFile

type private CategoryTagNode = {
  Category: string
  Tag: string
  Node: string
}

let private emitNodeTaglistFile o (graph:Graph) =
  let fileName = Graph.DeriveMissingName(o.InputFile, ".node.taglist.csv")
  do
    let builder = new CsvWriteRowBuilder()
    let tagCell = builder.AddCell("tag")
    let categoryCell = builder.AddCell("category")
    let countCell = builder.AddCell("nodecount")
    cp $"Saving \fg{fileName}\f0."
    use cw = new CsvRawWriter(fileName+".tmp")
    let rowBuffer = builder.Build()
    rowBuffer |> cw.WriteHeader
    let ctnsGrouped =
      seq {
        for node in graph.Nodes.Values do
          let metadata = node.Metadata
          for category in metadata.TagKeys do
            let ok, tags = category |> metadata.TryGetTags
            if ok then
              for tag in tags do
                yield {
                  Category = category
                  Tag = tag
                  Node = node.Key
                }
      }
      |> Seq.groupBy (fun ctn -> (ctn.Category, ctn.Tag))
    for ((cat, tag), ctns) in ctnsGrouped do
      let ctna = ctns |> Seq.toArray
      cat |> categoryCell.Set
      tag |> tagCell.Set
      ctna.Length |> string |> countCell.Set
      rowBuffer |> cw.WriteRow
  fileName |> finishFile

let private emitNodesFile o (graph:Graph) =
  let nodesFileName = Graph.DeriveMissingName(o.InputFile, ".nodes.csv")
  do
    let doPio = o.PartitionInOutProp |> String.IsNullOrEmpty |> not
    let allPropertyNames = graph.Nodes.Values.AllPropertyNames()
    let allTagKeys = graph.Nodes.Values.AllTagKeys()
    let nodePropertyNames =
      if o.NodePropertyAuto then
        if o.NodePropertyColumns.Length > 0 then
          cp "\foWarning: merging of \fg-np *\fo and explicit \fg-np\f0 options is NYI\f0!"
        allPropertyNames |> Seq.toList
      else
        o.NodePropertyColumns
    let builder = new CsvWriteRowBuilder()
    let nameCell = builder.AddCell("node")
    let kindCell = builder.AddCell("kind")
    let targetCountCell = builder.AddCell("#tgt")
    let sourceCountCell = builder.AddCell("#src")
    let inTgtCell = if doPio then builder.AddCell("#inTgt") else null
    let inSrcCell = if doPio then builder.AddCell("#inSrc") else null
    let exTgtCell = if doPio then builder.AddCell("#exTgt") else null
    let exSrcCell = if doPio then builder.AddCell("#exSrc") else null
    let propCells =
      nodePropertyNames
      |> List.map (fun propName -> builder.AddCell(propName))
    if nodePropertyNames |> List.isEmpty |> not then
      let npropNamesText =
        "\fo|\fc" + String.Join("\fo|\fc", nodePropertyNames) + "\fo|\f0"
      cp $"Including node property columns:      {npropNamesText}"
    let tagKeyInfos =
      allTagKeys
      |> Seq.toList
      |> List.map (fun tagKey -> (tagKey, builder.AddCell(if tagKey = "" then "#tags" else $"#tags({tagKey})")))
    let tcNames =
      tagKeyInfos |> List.map(fun (_,cwc) -> cwc.Name)
    let tcText =
      "\fo|\fb" + String.Join("\fo|\fb", tcNames) + "\fo|\f0"
    cp $"Including node key tag count columns: {tcText}"
    let rowBuffer = builder.Build()
    cp $"Saving \fg{nodesFileName}\f0."
    use cw = new CsvRawWriter(nodesFileName+".tmp")
    rowBuffer |> cw.WriteHeader
    for node in graph.Nodes.Values do
      node.Key |> nameCell.Set
      node.Kind.ToString() |> kindCell.Set
      let tgtCount = node.Targets.Count
      let srcCount = node.Sources.Count
      if doPio then
        let nodeProp = node.Metadata.GetPropertyOrDefault(o.PartitionInOutProp)
        let internTargetCount =
          node.Targets.Values
          |> Seq.where (fun edge -> edge.Target.Metadata.GetPropertyOrDefault(o.PartitionInOutProp) = nodeProp)
          |> Seq.length
        let externTargetCount = tgtCount - internTargetCount
        let internSourceCount =
          node.Sources.Values
          |> Seq.where (fun edge -> edge.Source.Metadata.GetPropertyOrDefault(o.PartitionInOutProp) = nodeProp)
          |> Seq.length
        let externSourceCount = srcCount - internSourceCount
        internTargetCount.ToString() |> inTgtCell.Set
        externTargetCount.ToString() |> exTgtCell.Set
        internSourceCount.ToString() |> inSrcCell.Set
        externSourceCount.ToString() |> exSrcCell.Set
        // Add properties to edges: internal target count of the source node and internal source count of the target node
        for edge in node.Targets.Values do
          edge.Metadata.SetProperty("#inTgt(from)", internTargetCount.ToString())
        for edge in node.Sources.Values do
          edge.Metadata.SetProperty("#inSrc(to)", internSourceCount.ToString())
      tgtCount.ToString() |> targetCountCell.Set
      srcCount.ToString() |> sourceCountCell.Set
      let metadata = node.Metadata
      for propCell in propCells do
        propCell.Name |> metadata.GetPropertyOrDefault |> propCell.Set
      for tagKey, tagCountCell in tagKeyInfos do
        metadata.TagCount(tagKey).ToString() |> tagCountCell.Set
      rowBuffer |> cw.WriteRow
  nodesFileName |> finishFile

let private runCsvExport o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  graph |> emitNodesFile o
  graph |> emitNodeTagsFile o
  graph |> emitEdgesFile o
  graph |> emitNodeTaglistFile o
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
    | "-pio" :: propname :: rest ->
      rest |> parseMore {o with PartitionInOutProp = propname}
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
    PartitionInOutProp = null
  }
  match oo with
  | Some(o) ->
    o |> runCsvExport
  | None ->
    cp ""
    Usage.usage "csv"
    1


