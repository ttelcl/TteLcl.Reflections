// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\foasmreflect \fycheck \f0{\fg-a \fcseed.[dll|exe]\f0}"
  cp "   Check suitability of the given assemblies for use with this app"
  cp "\fg-a \fcfile\f0          An assembly file to analyze (\fc*.exe\f0, \fc*.dll\f0)"
  cp "\fg-v \f0        \fx      Verbose mode"



