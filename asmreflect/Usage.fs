// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\foasmreflect \f0[\fkcheck\f0] \f0{\fg-a \fcseed.[dll|exe]\f0}"
  cp "   Analyze .NET assemblies and their dependencies"
  cp "\fg-a \fcfile\f0          An assembly file to analyze (\fc*.exe\f0, \fc*.dll\f0)"
  cp "\fg-v \f0        \fx      Verbose mode"



