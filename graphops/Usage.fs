// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\foUtility for editing, transforming and otherwise using *.graph.json files\f0"
  cp ""
  cp "\fographops tags \fg-i \fcfile.graph.json\f0 "
  cp "  List node tags in the graph"
  cp ""
  cp "\fographops purify \fg-i \fcinputfile \f0[\fg-o \fcoutputfile\f0]"
  cp "  Remove superfluous edges"
  cp ""
  cp "\fographops filter \fg-i \fcinputfile\f0 [\fg-include\f0|\fg-exclude\f0] {\fg-n \fctag\f0} [\fg-o \fcoutputfile\f0]"
  cp "  Create a subgraph keeping (\fg-include\f0) or removing (\fg-exclude\f0) the nodes with the specified tags"
  cp ""
  cp "\fographops dot \fg-i \fcinputfile \f0[\fg-o \fcoutputfile\f0] [\fg-prop \fcproperty\f0|\fg-cluster\f0|]"
  cp "  Export a GraphViz *.dot file describing the graph"
  cp "  \fg-prop \fcproperty\f0      Use the named property to cluster nodes"
  cp "  \fg-cluster \fx\f0           Create clusters for seeds, sinks, and others "
  cp ""
  cp "\fographops supergraph \fg-i \fcinputfile\f0 [\fg-prop \fcproperty\f0] [\fg-o \fcoutputfile\f0] [\fg-prefix \fcseparator \fCmaxcount\f0]"
  cp "  Calculate a supergraph, merging all 'equivalent' nodes into one"
  cp "  \fg-prop \fcproperty\f0      If given, 'equivalent' means 'same value for the given node \fcproperty\f0' (default 'category')"
  cp "  \fg-prefix \fcseparator \fCmaxcount\f0    Synthesize a property 'prefix' containing the common prefix of contained node names"
  cp "  \fx        \fx          \fx        \fx    Prefixes are based on up to \fCmaxcount\f0 segments when splitting names with \fcseparator\f0."
  cp ""
  cp "Common options:"
  cp "\fg-h               \f0Show help"
  cp "\fg-v               \f0Verbose mode"



