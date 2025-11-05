module AppCheck

open System
open System.IO

open TteLcl.Reflections

open ColorPrint
open CommonTools

type private Options = {
  Assemblies: string list
}

let private runCheck o =
  let afc = new AssemblyFileCollection()
  for a in o.Assemblies do
    let tag = Path.GetFileNameWithoutExtension(a)
    cp $"Adding \fg{a}\f0..."
    let count = afc.Seed(a, tag)
    cp $"  Added \fb{count}\f0 candidate assemblies"
  let byTag = afc.GetAssembliesByTag()
  cp "Prepared assemblies by tag:"
  for kvp in byTag do
    let tag = kvp.Key
    let count = kvp.Value.Count
    cp $"  \fb{count,5}\f0  {tag}"
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
    o |> runCheck
  | None ->
    cp ""
    Usage.usage "info"
    1
