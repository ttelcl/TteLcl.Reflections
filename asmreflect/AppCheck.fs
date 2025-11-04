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
    cp $"Adding \fg{a}\f0."
    let count = afc.Seed(a)
    cp "  Added \fb{count}\f0 entries"
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
