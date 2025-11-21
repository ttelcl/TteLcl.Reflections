// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\foasmreflect \f0[\fkcheck\f0] {\fg-a \fcseed.[dll|exe]\f0} [\fg-check\f0] [\fg-deps \fctag\f0]"
  cp "   Analyze .NET assemblies and their dependencies"
  cp "\fg-a \fcfile\f0          An assembly file to analyze (\fc*.exe\f0, \fc*.dll\f0)"
  cp "\fg-check\f0\fx           Check and report in greater detail"
  cp "\fg-deps \fctag\f0        Analyze dependencies, use \fctag\f0 to construct output file names"
  cp "\fg-types \fcassembly\f0  Export type information the specified assembly"
  cp "\fg-v \f0        \fx      Verbose mode"



