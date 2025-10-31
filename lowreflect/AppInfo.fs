module AppInfo

open System
open System.IO

open ColorPrint
open CommonTools

type private InfoOptions = {
  Assemblies: string list
}

let private runInfo o =
  let analyze file =
    cp $"Analyzing \fg{file}\f0:"
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
  
