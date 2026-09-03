module ParameterFile

open System

open TteLcl.Csv
open TteLcl.Csv.Core

type Token =
  | Eof
  | Eoln
  | Field of string

let private convertToken (t:CsvToken) =
  match t.TokenType with
  | CsvTokenType.EndOfFile -> Eof
  | CsvTokenType.EndOfLine -> Eoln
  | CsvTokenType.Field -> t.FieldValue |> Field
  | _ -> failwith "Unexpected token type"

type ParamFileState =
  | Start // Start of a normal line
  | CommentLine // Current line is a comment line
  | Value of String // A value on a non-comment line
  | Done // EOF

let private stateMachineStep state token =
  match state, token with
  | Done, _ -> Done
  | _, Eof -> Done
  | _, Eoln -> Start
  | Start, Field value ->
    if value.StartsWith('#') then CommentLine else Value(value)
  | CommentLine, Field _ -> CommentLine
  | Value _, Field value -> value |> Value

let readParametersFrom (tokenSource: CsvStreamParser) =
  tokenSource.EnumerateAsTokenStream()
  |> Seq.map convertToken
  |> Seq.scan stateMachineStep ParamFileState.Start
  |> Seq.choose (fun state -> match state with Value(v) -> Some(v) | _ -> None)

let readParameters fileName =
  use tokenSource = new CsvStreamParser(fileName, ' ')
  tokenSource |> readParametersFrom |> Seq.toList
  
