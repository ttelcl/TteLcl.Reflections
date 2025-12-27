// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\foasmreflect \f0[\fkcheck\f0] {\fg-a \fcseed.[dll|exe]\f0} [\fg-check\f0] [\fg-deps \fctag\f0]"
  cp "   Analyze .NET assemblies and their dependencies"
  cp "\fg-a \fcfile\f0                An assembly file to analyze (\fc*.exe\f0, \fc*.dll\f0)"
  cp "\fg-check\f0\fx                 Check and report in greater detail"
  cp "\fg-deps \fctag\f0              Analyze dependencies, use \fctag\f0 to construct output file names"
  cp "\fg-types \fcassembly\f0        Export type information the specified assemblies"
  cp "\fg-types \fo@ \fcfile.csv\f0 \fbcolumn\f0   Export type information in the assemblies named in the \fbcolumn\f0 of the CSV \fcfile\f0."
  cp "\fg-typo \fcfile.types.json\f0  The output file collecting all \fg-types\f0 output"
  cp "\fg-rule \fcmodule \fbsuffix\f0    Add a submodule name generating rule extending \fcmodule\f0 if the assembly starts with the prefix derived from the module \fbsuffix\f0."
  cp ""
  cp "\foasmreflect typegraph\f0 {\fg-a \fcseed.[dll|exe]\f0} {\fg-p \fcassembly\f0} [\fg-o \fcoutputfile\f0] <flags>"
  cp "  Analyze types in .NET assemblies by tracking references between types"
  cp "\fg-a \fcfile\f0                An assembly file to analyze and use as seed for determining valid assemblies (\fc*.exe\f0, \fc*.dll\f0)"
  cp "\fx\fx\fx                       (all types in these assemblies are inluded, not just public types: '\fg-p\f0' is implied)"
  cp "\fg-p \fcassembly\f0            Additional assemblies reachable from the \fg-a\f0 assemblies for which to include ALL types."
  cp "\fg-o \fcoutputfile\f0          The output file in either plain \fc*.json\f0 or \fc*.mjson\f0 (multi-json) format."
  cp "\fg-props\fx\fx                 Follow property type edges"
  cp "\fg-fields\fx\fx                Follow field type edges"
  cp "\fg-methods\fx\fx               Follow method related type edges: return types, parameter types, generic type arguments"
  cp "\fg-events\fx\fx                Follow event type edges"
  cp "\fg-ctors\fx\fx                 Follow constructor argument edges"
  cp "\fg-all\fx\fx                   Follow all type relation edges"
  cp ""
  cp "\fg-v \f0        \fx            Verbose mode"



