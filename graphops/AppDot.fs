module AppDot

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Graphs
open TteLcl.Graphs.Analysis

open ColorPrint
open CommonTools
open TteLcl.Graphs.Dot

type private Options = {
  InputFile: string
  OutputFile: string
}

let private runDot o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  let seedKeys = graph.SeedNodes |> Seq.map (fun n -> n.Key) |> Set.ofSeq
  let sinkKeys = graph.SinkNodes |> Seq.map (fun n -> n.Key) |> Set.ofSeq
  cp $"  (\fb{graph.NodeCount}\f0 nodes, \fc{graph.EdgeCount}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  do
    cp $"Writing \fg{o.OutputFile}\f0."
    use dw = new DotFileWriter(o.OutputFile + ".tmp", true, horizontal = false)
    // dw.WriteProperty("TBbalance", "min")
    let classification = graph.ClassifyNodes((fun n -> n.Kind.ToString()))
    for kvp in classification do
      let cls = kvp.Key
      use _ = dw.StartSubGraph("cluster_" + cls, null)
      for node in kvp.Value do
        let properties = node.GetProperties()
        use _ = dw.StartNode(node.Key, [ properties["module"] ], "box")
        if node.Key |> seedKeys.Contains then
          dw.WriteProperty("color", "#ccdd55")
        elif node.Key |> sinkKeys.Contains then
          dw.WriteProperty("color", "#cc55dd")
        ()
    for node in graph.Nodes.Values do
      for edge in node.Targets.Values do
        let src = edge.Source.Key
        let tgt = edge.Target.Key
        use _ = dw.StartEdge(src, tgt)
        ()
    ()
  o.OutputFile |> finishFile
  cp $"   Reminder on dot commands:"
  cp $"      \fGdot -Txdot -O {o.OutputFile}\f0 to generate laid out dot (xdot)"
  cp $"      \fGdot -Txdot_json -O {o.OutputFile}.xdot\f0 to convert that to json"

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
      else
        let missingOutputName = o.OutputFile |> String.IsNullOrEmpty
        let o = {o with OutputFile = Graph.DeriveMissingName(o.InputFile, ".dot", o.OutputFile)}
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
  }
  match oo with
  | Some(o) ->
    o |> runDot
  | None ->
    cp ""
    Usage.usage "dot"
    1

