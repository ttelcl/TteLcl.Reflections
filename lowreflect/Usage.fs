// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\folowreflect \fyinfo \f0{\fg-a \fcfile.dll\f0} [\fg-np\f0]"
  cp "   Inspect the .net DLL (or EXE) at low level"
  cp "\fg-a \fcfile.dll\f0      An assembly file to analyze (\fc*.exe\f0, \fc*.dll\f0)"
  cp "\fg-np\f0\fx              Include non-public visibilities"
  cp "\fg-v \f0        \fx      Verbose mode"



