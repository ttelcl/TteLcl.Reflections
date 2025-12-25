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
  cp "\fg-v \f0        \fx            Verbose mode"



