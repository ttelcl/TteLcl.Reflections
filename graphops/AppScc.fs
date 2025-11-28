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
  Prefix: string
}

let private runScc o =
  cp $"Loading \fg{o.InputFile}\f0."
  let graph = o.InputFile |> Graph.DeserializeFile
  let nodeCountBefore = graph.NodeCount
  let edgeCountBefore = graph.EdgeCount
  cp $"  (\fb{nodeCountBefore}\f0 nodes, \fc{edgeCountBefore}\f0 edges, \fy{graph.SeedCount}\f0 seeds, \fo{graph.SinkCount}\f0 sinks)"
  let analyzer = new GraphAnalyzer(graph)
  let sccResult = analyzer.StronglyConnectedComponents(o.Prefix)
  let components = sccResult.Components
  let minSize = components |> Seq.map (fun c -> c.Nodes.Count) |> Seq.min
  let maxSize = components |> Seq.map (fun c -> c.Nodes.Count) |> Seq.max
  cp $"Found \fb{components.Count}\f0 components, varying in size from \fb{minSize}\f0 to \fb{maxSize}\f0."
  for c in components do
    for node in c.Nodes do
      let node = graph.Nodes[node]
      node.Metadata.SetProperty("scc", c.Name)
      node.Metadata.SetProperty("sccindex", c.Index.ToString())
  let componentsName = Graph.DeriveMissingName(o.InputFile, ".scc-components.json")
  do
    use w = componentsName |> startFile
    let json = JsonConvert.SerializeObject(components, Formatting.Indented)
    w.WriteLine(json)
  componentsName |> finishFile
  do
    let taggedName = Graph.DeriveMissingName(o.InputFile, ".scc-tagged.graph.json")
    cp $"Saving '\fg{taggedName}\f0'"
    graph.Serialize(taggedName + ".tmp")
    taggedName |> finishFile
  let sccGraph = sccResult.ComponentGraph(graph)
  do
    let graphName = Graph.DeriveMissingName(o.InputFile, ".scc-graph.graph.json")
    cp $"Saving '\fg{graphName}\f0'"
    sccGraph.Serialize(graphName + ".tmp")
    graphName |> finishFile
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
    | "-prefix" :: prefix :: rest ->
      rest |> parseMore {o with Prefix = prefix}
    | "-autoname" :: rest ->
      rest |> parseMore {o with Prefix = null}
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
    Prefix = "SCC-"
  }
  match oo with
  | Some(o) ->
    o |> runScc
  | None ->
    cp ""
    Usage.usage "scc"
    1


