module AppCheck

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Reflections
open TteLcl.Reflections.Graph

open ColorPrint
open CommonTools

type private Options = {
  Assemblies: string list
  Check: bool
  Dependencies: string
  TypeAssemblies: string list
}

type private LoadState = {
  Afc: AssemblyFileCollection
  Mlc: MetadataLoadContext
  InitialAssemblies: Assembly array
  SeedAssemblies: Assembly array
  PostLoadAssemblies: Assembly array
}

let private buildFileCollection o =
  let afc = new AssemblyFileCollection()
  for a in o.Assemblies do
    let tag = Path.GetFileNameWithoutExtension(a)
    cp $"Adding \fg{a}\f0..."
    let count = afc.Seed(a, tag)
    cp $"  Added \fb{count}\f0 candidate assemblies"
  afc

let private countAssembliesByModule (afc: AssemblyFileCollection) =
  let byTag = afc.GetAssembliesByTag()
  cp "Prepared assemblies by tag:"
  for kvp in byTag do
    let tag = kvp.Key
    let count = kvp.Value.Count
    cp $"  \fb{count,5}\f0  {tag}"

let private checkAmbiguousRegistrations (afc: AssemblyFileCollection) =
  cp "Checking for ambiguous registrations:"
  let ambiguousRegistrations =
    afc.AssembliesByName
    |> Seq.map (fun kvp -> (kvp.Key, kvp.Value))
    |> Seq.filter (fun (_, infos) -> infos.Count > 1)
    |> Seq.toArray
  if ambiguousRegistrations.Length = 0 then
    cp "\fgNo ambiguities found\f0."
  else
    cp $"\fb{ambiguousRegistrations.Length}\fy ambiguous names found\f0:"
    for (name, infos) in ambiguousRegistrations do
      cp $"  \fo{name}\f0:"
      for info in infos do
        let fileName = info.FileName
        let an = info.GetAssemblyName()
        if an = null then
          cp $"    \fk{fileName}\f0 (\frnot an assembly\f0)"
        else
          cp $"    \fy{fileName}\f0 (\fc{an.Version}\f0)"

let private assemblyDiagnostics (afc: AssemblyFileCollection) assemblies =
  let assemblyDiagnostic (asm: Assembly) =
    let asmName = asm.GetName()
    let ok, afi = asm |> afc.TryFindByAssembly
    if ok then
      cp $"  \fc{asmName}\f0 is loaded from \fg{afi.FileName}\f0."
    else
      cp $"  \fo{asmName}\f0 is loaded from an \frunregistered location\f0."
  for asm in assemblies do
    asm |> assemblyDiagnostic

let private initLoadState o afc (mlc: MetadataLoadContext) =
  let assemblyDiagnostics = assemblyDiagnostics afc
  let initialAssemblies = mlc.GetAssemblies() |> Seq.toArray
  if o.Check then
    cp $"Initial assembly count: \fb{initialAssemblies.Length}\f0."
  let loadAssembly (a: string) =
    let anm = Path.GetFileNameWithoutExtension(a)
    cp $"Loading \fg{anm}\f0."
    mlc.LoadFromAssemblyName(anm)
  let seedAssemblies =
    o.Assemblies
    |> Seq.map loadAssembly
    |> Seq.toArray
  let postLoadAssemblies = mlc.GetAssemblies() |> Seq.toArray
  if o.Check then
    cp $"Post preload assembly count: \fb{postLoadAssemblies.Length}\f0."
    postLoadAssemblies |> assemblyDiagnostics
  {
    Afc = afc
    Mlc = mlc
    InitialAssemblies = initialAssemblies
    SeedAssemblies = seedAssemblies
    PostLoadAssemblies = postLoadAssemblies
  }

let private typesTest o loadState =
  cp "\frNot yet implemented: \fg-types\f0."
  ()

let private runCheck o =
  let afc = o |> buildFileCollection
  if o.Check then afc |> countAssembliesByModule
  if o.Check then afc |> checkAmbiguousRegistrations
  // cp "Initializing assembly loader"
  use mlc = afc.OpenLoadContext()
  let loadState = initLoadState o afc mlc
  if o.TypeAssemblies |> List.isEmpty |> not then
    typesTest o loadState
  if o.Dependencies |> String.IsNullOrEmpty |> not then
    cp "Initializing dependency graph"
    let builder = new AssemblyGraphLoader(afc, mlc, null)
    for asm in loadState.InitialAssemblies do
      let added, node = builder.AddAssembly(asm)
      ()
    let pendingQueue = builder.SeedAssemblies(loadState.SeedAssemblies);
    let eraser = "\r" + new String(' ', Console.WindowWidth - 1) + "\r"
    // cp $"Pending: \fb{pendingQueue.Count}\f0."
    while pendingQueue.Count > 0 do
      let pendingBefore = pendingQueue.Count
      let node = pendingQueue.Peek()
      let name = node.ShortName
      let added = builder.ConnectNext(pendingQueue)
      let message = $"\fk{pendingBefore,3}\f0 -> \fb{pendingQueue.Count,3}\f0  \fc+{added,3}\f0  \fg{name,-60} \fy{node.Module}\f0."
      if verbose then
        cp message
      else
        cpx $"{eraser}{message}  "
      ()
    let totalNodeCount = builder.Graph.Nodes.Count
    let missingNodeCount = builder.Graph.Nodes |> Seq.where (fun n -> n.FileName |> String.IsNullOrEmpty) |> Seq.sumBy (fun _ -> 1)
    let message = $"\fb{totalNodeCount}\f0 graph nodes: \fg{totalNodeCount - missingNodeCount}\f0 assemblies loaded, and \fr{missingNodeCount}\f0 missing assemblies"
    if verbose |> not then
      cp $"{eraser}{message}"
    else
      cp message
    let fileName = $"{o.Dependencies}.asm-graph.json"
    do
      let graph = builder.Graph
      use w = fileName |> startFile
      let json = JsonConvert.SerializeObject(graph, Formatting.Indented)
      w.WriteLine(json)
    fileName |> finishFile
    cp "Converting to generic graph model"
    let graph = builder.Graph.ExportAsGraph()
    let fileName = $"{o.Dependencies}.graph.json"
    do
      let jgraph = graph.Serialize()
      use w = fileName |> startFile
      let json = JsonConvert.SerializeObject(jgraph, Formatting.Indented)
      w.WriteLine(json)
    fileName |> finishFile
    ()
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
        // let assembly = assembly |> Path.GetFullPath
        rest |> parseMore {o with Assemblies = assembly :: o.Assemblies}
    | "-check" :: rest ->
      rest |> parseMore {o with Check = true}
    | "-deps" :: filetag :: rest
    | "-dependencies" :: filetag :: rest ->
      rest |> parseMore {o with Dependencies = filetag}
    | "-types" :: ta :: rest ->
      rest |> parseMore {o with TypeAssemblies = ta :: o.TypeAssemblies}
    | [] ->
      if o.Assemblies |> List.isEmpty then
        cp "\foNo assembly arguments (\fg-a\fo) given\f0."
        None
      else
        {o with Assemblies = o.Assemblies |> List.rev; TypeAssemblies = o.TypeAssemblies |> List.rev} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None 
  let oo = args |> parseMore {
    Assemblies = []
    Check = false
    Dependencies = null
    TypeAssemblies = []
  }
  match oo with
  | Some(o) ->
    o |> runCheck
  | None ->
    cp ""
    Usage.usage "info"
    1
