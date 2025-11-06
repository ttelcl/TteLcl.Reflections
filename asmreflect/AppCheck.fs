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
}

let private runCheck o =
  let afc = new AssemblyFileCollection()
  for a in o.Assemblies do
    let tag = Path.GetFileNameWithoutExtension(a)
    cp $"Adding \fg{a}\f0..."
    let count = afc.Seed(a, tag)
    cp $"  Added \fb{count}\f0 candidate assemblies"
  if o.Check then
    let byTag = afc.GetAssembliesByTag()
    cp "Prepared assemblies by tag:"
    for kvp in byTag do
      let tag = kvp.Key
      let count = kvp.Value.Count
      cp $"  \fb{count,5}\f0  {tag}"
  if o.Check then
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
  let assemblyDiagnostic (asm: Assembly) =
    let asmName = asm.GetName()
    let ok, afi = asm |> afc.TryFindByAssembly
    if ok then
      cp $"  \fc{asmName}\f0 is loaded from \fg{afi.FileName}\f0."
    else
      cp $"  \fo{asmName}\f0 is loaded from an \frunregistered location\f0."
  let assemblyDiagnostics assemblies =
    for asm in assemblies do
      asm |> assemblyDiagnostic
  // cp "Initializing assembly loader"
  use mlc = afc.OpenLoadContext()
  let initialAssemblies = mlc.GetAssemblies() |> Seq.toArray
  // cp $"Initial assembly count: \fb{initialAssemblies.Length}\f0."
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
  if o.Dependencies |> String.IsNullOrEmpty |> not then
    cp "Initializing dependency graph"
    let builder = new AssemblyGraphLoader(afc, mlc, null)
    for asm in initialAssemblies do
      let added, node = builder.AddAssembly(asm)
      ()
    let pendingQueue = builder.SeedAssemblies(seedAssemblies);
    cp $"Pending: \fb{pendingQueue.Count}\f0."
    if pendingQueue.Count > 0 then
      let pendingBefore = pendingQueue.Count
      let name = pendingQueue.Peek().ShortName
      let added = builder.ConnectNext(pendingQueue)
      cp $"\fk{pendingBefore,3}\f0 -> \fb{pendingQueue.Count}\f0  \fc+{added}\f0  \fg{name}\f0."
      ()
    let fileName = $"{o.Dependencies}.graph.json"
    do
      let graph = builder.Graph
      use w = fileName |> startFile
      let json = JsonConvert.SerializeObject(graph, Formatting.Indented)
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
    | [] ->
      if o.Assemblies |> List.isEmpty then
        cp "\foNo assembly arguments (\fg-a\fo) given\f0."
        None
      else
        {o with Assemblies = o.Assemblies |> List.rev} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fy{x}\f0'"
      None 
  let oo = args |> parseMore {
    Assemblies = []
    Check = false
    Dependencies = null
  }
  match oo with
  | Some(o) ->
    o |> runCheck
  | None ->
    cp ""
    Usage.usage "info"
    1
