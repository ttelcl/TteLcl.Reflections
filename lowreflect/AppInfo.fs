module AppInfo

open System
open System.IO
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.PortableExecutable 

open ColorPrint
open CommonTools

type private InfoOptions = {
  Assemblies: string list
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
      for tdefh in mr.TypeDefinitions do
        let tdef = tdefh |> mr.GetTypeDefinition
        let ns = tdef.Namespace |> mr.GetString
        let name = tdef.Name |> mr.GetString
        let attr = tdef.Attributes
        let isInterface = attr.HasFlag(TypeAttributes.Interface)
        let isAbstract = attr.HasFlag(TypeAttributes.Abstract)
        let isPublic = (attr &&& TypeAttributes.VisibilityMask) = TypeAttributes.Public
        let color =
          if isInterface then
            "\fo"
          elif isPublic then
            if isAbstract then "\fG" else "\fg"
          else
            "\fk"
        cp $"  \fc{ns}\f0.{color}{name}\f0. ({attr})"
        ()
      ()
    else
      cp $"\fC{file}\fk does not have .net metadata\f0."
    cp "  \frNYI\f0."
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
  }
  match oo with
  | Some(o) ->
    o |> runInfo
  | None ->
    cp ""
    Usage.usage "info"
    1
  
