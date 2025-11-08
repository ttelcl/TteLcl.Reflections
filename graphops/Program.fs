// (c) 2025  ttelcl / ttelcl

open System

open ColorPrint
open CommonTools
open ExceptionTool

let rec run arglist =
  // For subcommand based apps, split based on subcommand here
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-h" :: _
  | [] ->
    Usage.usage ""
    0  // program return status code to the operating system; 0 == "OK"
  | "tags" :: rest ->
    rest |> AppTags.run
  | "purify" :: rest ->
    cp "\fypurify \fo Not yet implemented\f0."
    1
  | "filter" :: rest ->
    rest |> AppFilter.run
  | x :: _ ->
    cp $"\frUnrecognized command: \fo{x}\f0."
    cp ""
    Usage.usage ""
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



