module AppInfo

open System
open System.IO
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.PortableExecutable 

open TteLcl.Reflections

open ColorPrint
open CommonTools
open System.Runtime.InteropServices

type private InfoOptions = {
  Assemblies: string list
  NonPublic: bool
}

let private runInfo o =
  let analyze file =
    cp $"Analyzing \fg{file}\f0:"
    use pestream =
      let stream = File.OpenRead(file)
      new PEReader(stream)
    if pestream.HasMetadata then
      cp $"\fg{file}\f0 has .net metadata"
      let mr = pestream.GetMetadataReader()
      if mr.IsAssembly then
        let asmdef = mr.GetAssemblyDefinition()
        let myasm = asmdef.GetAssemblyName()
        cp $"This is assembly \fg{myasm}\f0."
        cp "\foTypes\f0:"
        for tdefh in mr.TypeDefinitions do
          let tdef = tdefh |> mr.GetTypeDefinition
          let ns = tdef.Namespace |> mr.GetString
          let name = tdef.Name |> mr.GetString
          let attr = tdef.Attributes
          let visibility = attr &&& TypeAttributes.VisibilityMask
          let isInterface = attr.HasFlag(TypeAttributes.Interface)
          let isAbstract = attr.HasFlag(TypeAttributes.Abstract)
          let isPublic = visibility = TypeAttributes.Public || visibility = TypeAttributes.NestedPublic
          if isPublic || o.NonPublic then
            let color =
              if isInterface then
                "\fo"
              elif isPublic then
                if isAbstract then "\fG" else "\fg"
              else
                "\fk"
            cp $"  \fc{ns}\f0.{color}{name}\f0. ({attr})"
          ()
        cp "\foAssemblyFiles\f0:"
        for afh in mr.AssemblyFiles do
          let af = afh |> mr.GetAssemblyFile
          if af.ContainsMetadata then
            cp $"  \fg{af.Name}\f0 (has metadata)"
          else
            cp $"  \fk{af.Name}\f0 (no metadata)"
          ()
        cp "\foAssemblyReferences\f0:"
        let assemblyNames =
          mr.AssemblyReferences
          |> Seq.map (fun arh -> arh |> mr.GetAssemblyReference)
          |> Seq.map (fun ar -> ar.GetAssemblyName())
          |> Seq.sortBy (fun an -> an.Name)
          |> Seq.toArray
        for an in assemblyNames do
          cp $"  \fg{an.Name}\f0 \fc{an.Version}\f0 ({an.CultureInfo})"
          ()
        ()
      else
        cp "  \fyNot an assembly\f0."
    else
      cp $"\fC{file}\fk does not have .net metadata\f0."
  //let rtd = RuntimeEnvironment.GetRuntimeDirectory()
  //cp $"Current Runtime Directory = {rtd}"
  for assembly in o.Assemblies do
    assembly |> analyze
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
        let assembly = assembly |> Path.GetFullPath
        rest |> parseMore {o with Assemblies = assembly :: o.Assemblies}
    | "-np" :: rest ->
      rest |> parseMore {o with NonPublic = true}
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
    NonPublic = false
  }
  match oo with
  | Some(o) ->
    o |> runInfo
  | None ->
    cp ""
    Usage.usage "info"
    1
  
