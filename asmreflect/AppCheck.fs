module AppCheck

open System
open System.IO
open System.Reflection

open Newtonsoft.Json

open TteLcl.Reflections
open TteLcl.Reflections.Graph
open TteLcl.Reflections.TypesModel

open ColorPrint
open CommonTools

type Dictionary<'k,'v> = System.Collections.Generic.Dictionary<'k,'v>

type private Options = {
  Assemblies: string list
  Check: bool
  Dependencies: string
  TypeAssemblies: string list
  TypeOutFile: string
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

let private loadDependencyGraph o loadState =
  let afc = loadState.Afc
  let mlc = loadState.Mlc
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
  let usedAssemblies =
    mlc.GetAssemblies()
    |> Seq.filter (fun asm -> (asm.Location |> String.IsNullOrEmpty |> not) && (asm.Location |> File.Exists)) // skip ghosts
    |> Seq.toList
  cp $"\fb{usedAssemblies.Length}\f0 of \fc{afc.AssembliesByName.Count}\f0 candidates in use"
  let afcUsage =
    afc.ReportRegistrationUse(usedAssemblies)
    |> Seq.toArray
    |> Array.sortBy (fun afu -> (afu.Module, afu.IsUsed, afu.AssemblyTag.ToLowerInvariant(), afu.AssemblyVersion, afu.FileName))
  let afuByModuleAndUse =
    afcUsage
    |> Array.groupBy (fun afu -> afu.Module)
    |> Array.map (fun (m,afus) -> (m, afus |> Array.groupBy (fun afu -> if afu.IsUsed then "used" else "unused")))
  // Convert afuByModuleAndUse to something serializable
  let afuMap = new Dictionary<string,Dictionary<string,AssemblyFileUsage array>>()
  for (m, mg) in afuByModuleAndUse do
    let nest = new Dictionary<string, AssemblyFileUsage array>()
    afuMap.Add(m, nest)
    for (u, ug) in mg do
      nest.Add(u, ug)
  let afcUsageFileName = $"{o.Dependencies}.registration-usage.json"
  do
    use w = afcUsageFileName |> startFile
    let json = JsonConvert.SerializeObject(afuMap, Formatting.Indented)
    w.WriteLine(json)
  afcUsageFileName |> finishFile

let typeColor (t:Type) =
  if t.IsNestedPublic then
    "\fw"
  elif t.IsPublic then
    if t.IsInterface then
      "\fo"
    elif t.IsAbstract then
      if t.IsValueType then
        "\fC"
      else
        "\fy"
    elif t.IsValueType then
      "\fb"
    elif t.IsClass then
      "\fg"
    else
      "\fc"
  else
    "\fk"

let private scanTypes o loadState =
  let afc = loadState.Afc
  let mlc = loadState.Mlc
  let typeAssemblies = o.TypeAssemblies
  let outName =
    if o.TypeOutFile.EndsWith(".types.json", StringComparison.OrdinalIgnoreCase) |> not then
      o.TypeOutFile + ".types.json"
    else
      o.TypeOutFile
  let typeMap = AssemblyTypeMap.CreateNew()
  for tan in typeAssemblies do
    if verbose then
      cp $"Loading \fy{tan}\f0."
    let a = mlc.LoadFromAssemblyName(tan)
    if verbose then
      cp $"    Loaded \fg{a.Location}\f0."
    let types = a.GetTypes()
    for t in types do
      if verbose then
        let color = t |> typeColor
        cp $"  {color}{t.FullName}\f0 ({t.Name})."
      let baseType = t.BaseType
      if verbose && baseType <> null then
        let baseColor = baseType |> typeColor
        let baseName = if baseType.FullName |> String.IsNullOrEmpty then baseType.Name+" \fr!!!" else baseType.FullName
        cp $"    : {baseColor}{baseName}\f0 ({baseType.Assembly.FullName})"
    //let typeList = AssemblyTypeList.FromAssembly(a)
    typeMap.AddAssembly(a)
    cp $"Added \fb{types.Length}\f0 types from \fy{a.GetName()}\f0."
    //let fileName = $"{a.GetName().Name}.types.json"
    //cp $"Saving \fg{fileName}\f0."
    //let json = JsonConvert.SerializeObject(typeList, Formatting.Indented)
    //File.WriteAllText(fileName+".tmp", json)
    //fileName |> finishFile
  let typeCount = typeMap.TypesByAssembly.Values |> Seq.sumBy (fun list -> list.Count)
  cp $"Saving \fb{typeCount}\f0 types to \fg{outName}\f0."
  typeMap.SaveInnerToJson(outName + ".tmp", true)
  outName |> finishFile
  //if verbose then
  //  cp "Assemblies after type loading:"
  //  mlc.GetAssemblies() |> assemblyDiagnostics afc

let private runCheck o =
  let afc = o |> buildFileCollection
  if o.Check then afc |> countAssembliesByModule
  if o.Check then afc |> checkAmbiguousRegistrations
  use mlc = afc.OpenLoadContext()
  let loadState = initLoadState o afc mlc
  if o.TypeAssemblies |> List.isEmpty |> not then
    // Beware: this mutates the content of the objects in loadState!
    scanTypes o loadState
  if o.Dependencies |> String.IsNullOrEmpty |> not then
    // Beware: this mutates the content of the objects in loadState!
    loadDependencyGraph o loadState
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
    | "-typo" :: file :: rest ->
      rest |> parseMore {o with TypeOutFile = file}
    | [] ->
      if o.Assemblies |> List.isEmpty then
        cp "\foNo assembly arguments (\fg-a\fo) given\f0."
        None
      elif (o.TypeAssemblies |> List.isEmpty |> not) && (o.TypeOutFile |> String.IsNullOrEmpty) then
        cp "\frMissing \fg-typo\fr argument. \f0(\forequired when any \fy-types\fo is present\f0)"
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
    TypeOutFile = null
  }
  match oo with
  | Some(o) ->
    o |> runCheck
  | None ->
    cp ""
    Usage.usage "info"
    1
