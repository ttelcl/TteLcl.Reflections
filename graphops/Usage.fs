// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\foUtility for editing, transforming and otherwise using *.graph.json files\f0"
  cp ""
  cp "\fographops tags \fg-i \fcfile.graph.json\f0 "
  cp "   List node tags in the graph"
  cp ""
  cp "\fographops purify \fg-i \fcinputfile \f0[\fg-o \fcoutputfile\f0]"
  cp "   Remove superfluous edges"
  cp ""
  cp "\fographops filter \fg-i \fcinputfile\f0 [\fg-include\f0|\fg-exclude\f0] {\fg-n \fc tag\f0} [\fg-o \fcoutputfile\f0]"
  cp "   Create a subgraph keeping (\fg-include\f0) or removing (\fg-exclude\f0) the nodes with the specified tags"
  cp ""
  cp "Common options:"
  cp "\fg-h               \f0Show help"
  cp "\fg-v               \f0Verbose mode"



