module AppScc

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

let private runScc o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  let nodeCountBefore = graph.NodeCount
  let edgeCountBefore = graph.EdgeCount
  cp $"  (\fb{nodeCountBefore}\f0 nodes, \fc{edgeCountBefore}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let analyzer = new GraphAnalyzer(graph)
  let components = analyzer.StronglyConnectedComponents()
  let minSize = components |> Seq.map (fun ks -> ks.Count) |> Seq.min
  let maxSize = components |> Seq.map (fun ks -> ks.Count) |> Seq.max
  cp $"Found \fb{components.Count}\f0 components, varying in size from \fb{minSize}\f0 to \fb{maxSize}\f0."
  let namedComponents =
    components
    |> Seq.mapi (fun i ks -> (ks, $"SCC-{i:D3}"))
  for (ks, name) in namedComponents do
    for key in ks do
      let node = graph.Nodes[key]
      node.Metadata.SetProperty("scc", name)
  cp $"Saving '\fg{o.OutputFile}\f0'"
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
      else
        let missingOutputName = o.OutputFile |> String.IsNullOrEmpty
        let o = {o with OutputFile = Graph.DeriveMissingName(o.InputFile, ".scc.graph.json", o.OutputFile)}
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
    o |> runScc
  | None ->
    cp ""
    Usage.usage "scc"
    1


