module AppTypegraph

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Csv
open TteLcl.Csv.Core

open TteLcl.Reflections.AssemblyFiles
open TteLcl.Reflections.Graph
open TteLcl.Reflections.TypesModel
open TteLcl.Reflections.TypeTrees

open ColorPrint
open CommonTools

type Dictionary<'k,'v> = System.Collections.Generic.Dictionary<'k,'v>
type HashSet<'k> = System.Collections.Generic.HashSet<'k>

type private Options = {
  SeedAssemblies: string list
  FullAssemblies: string list
  Outputfile: string
  Rules: SubmoduleRules
  Relations: TypeEdgeKind
}

let private buildFileCollection o =
  let afc = new AssemblyFileCollection(o.Rules)
  for a in o.SeedAssemblies do
    let tag = Path.GetFileNameWithoutExtension(a)
    cp $"Adding \fg{a}\f0..."
    let count = afc.Seed(a, tag)
    cp $"  Added \fb{count}\f0 candidate assemblies"
  afc

let private runTypegraph o =
  let afc = o |> buildFileCollection
  use mlc = afc.OpenLoadContext()
  let loadSeedAssembly (a: string) =
    let anm = Path.GetFileNameWithoutExtension(a)
    cp $"Loading \fg{anm}\f0."
    mlc.LoadFromAssemblyName(anm)
  let seedAssemblies =
    o.SeedAssemblies
    |> Seq.map loadSeedAssembly
    |> Seq.toArray
  let typenodes = new TypeNodeMap(o.Relations)
  // first add seed assemblies
  for assembly in seedAssemblies do
    assembly |> typenodes.AddAssembly
  cp $"After pre-loading \fg-a\f0 assemblies: \fb{typenodes.NodeCount}\f0 types"
  // next add the other full-load assemblies (aborting if unknown)
  for a in o.FullAssemblies do
    let assembly = mlc.LoadFromAssemblyName(a)
    assembly |> typenodes.AddAssembly
  cp $"After pre-loading \fg-p\f0 assemblies: \fb{typenodes.NodeCount}\f0 types"
  let mutable nextFeedback = DateTime.UtcNow
  for loadedNode in typenodes.LoadNodes() do
    let now = DateTime.UtcNow
    if now >= nextFeedback then
      nextFeedback <- now.AddMilliseconds(250)
      cpx $"\r \fc{typenodes.PendingNodeCount,5}\fo / \fb{typenodes.NodeCount, 5}\f0  ..."
  cp $"\r \fc{typenodes.PendingNodeCount,5}\fo / \fb{typenodes.NodeCount, 5}\f0  Done."
  let modelsByAssembly = typenodes.ToAssemblyGroupedModel()
  do
    use w = o.Outputfile |> startFile
    let json = JsonConvert.SerializeObject(modelsByAssembly, Formatting.Indented)
    w.WriteLine(json)
  o.Outputfile |> finishFile
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
    | "-a" :: assembly :: rest ->
      if assembly |> File.Exists |> not then
        cp $"\foFile not found: \fy{assembly}"
        None
      else
        let shortname = assembly |> Path.GetFileNameWithoutExtension
        rest 
        |> parseMore 
           { o with SeedAssemblies = assembly :: o.SeedAssemblies; FullAssemblies = shortname :: o.FullAssemblies }
    | "-p" :: assembly :: rest ->
      rest |> parseMore {o with FullAssemblies = assembly :: o.FullAssemblies}
    | "-o" :: file :: rest ->
      rest |> parseMore {o with Outputfile = file}
    | "-rule" :: m :: prefix :: rest ->
      o.Rules.AddRule(m, prefix) |> ignore
      rest |> parseMore o
    | "-props" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Properties}
    | "-fields" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Fields}
    | "-returns" :: rest | "-return" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Returns}
    | "-args" :: rest | "-arguments" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Arguments}
    | "-methods" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Methods}
    | "-events" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Events}
    | "-ctor" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.Constructors}
    | "-all" :: rest ->
      rest |> parseMore {o with Relations = o.Relations ||| TypeEdgeKind.All}
    | [] ->
      if o.SeedAssemblies |> List.isEmpty then
        cp "\foNo seed assembly arguments (\fg-a\fo) given\f0."
        None
      elif (o.Outputfile |> String.IsNullOrEmpty) && o.SeedAssemblies.Length > 1 then
        cp "\foNo output (\fg-o\fo) given \f0(required when giving multiple \fg-a\f0 options)"
        None
      else
        let outname =
          if o.Outputfile |> String.IsNullOrEmpty then
            (o.SeedAssemblies.Head |> Path.GetFileNameWithoutExtension) + ".out.types.json"
          else
            o.Outputfile
        {o with SeedAssemblies = o.SeedAssemblies |> List.rev; FullAssemblies = o.FullAssemblies |> List.rev; Outputfile = outname}
        |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None 
  let oo = args |> parseMore {
    SeedAssemblies = []
    FullAssemblies = []
    Outputfile = null
    Rules = new SubmoduleRules()
    Relations = TypeEdgeKind.None
  }
  match oo with
  | Some(o) ->
    o |> runTypegraph
  | None ->
    cp ""
    Usage.usage "typegraph"
    1
