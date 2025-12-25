// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  let showSection section =
    focus = "" || focus = "all" || focus = section
  let showDetails section =
    focus = "all" || focus = section
  if showSection "" || showSection "all" then
    cp "\fyUtility for editing, transforming and otherwise using *.graph.json files\f0"
    cp ""
  if showSection "tags" then
    cp "\fographops tags \fg-i \fcfile.graph.json\f0 "
    cp "  List node tags in the graph"
  if showDetails "tags" then
    cp ""
  if showSection "scc" then
    cp "\fographops scc \fg-i \fcinputfile \f0[\fg-prefix \fcprefix\f0|\fg-autoname\f0]"
    cp "  Find Strongly Connected Components. Adds an 'scc' property to each node with a numeric SCC index."
  if showDetails "scc" then
    cp "  \fg-prefix \fcprefix\f0      Use the prefix for naming components. Default '\foSCC-\f0'"
    cp "  \fg-autoname\f0\fx           Name components by picking a random node in them"
    cp ""
  if showSection "cycles" then
    cp "\fographops cycles \fg-i \fcinputfile \f0[\fg-o \fcoutputfile\f0] [\fg-tag\f0|\fg-split \fccyclefile\f0]"
    cp "  Find cycles in the graph and tag them or split the graph based on them"
  if showDetails "cycles" then
    cp "  \fg-tag\f0\fx                Tagging mode: tag edges where cycles are detected in the edge metadata. This is the default"
    cp "  \fg-split \fccyclefile\f0    Splitting mode: create two outputs: one with cycle key edges removed, and the other with just those"
    cp ""
  if showSection "purify" then
    cp "\fographops purify \fg-i \fcinputfile \f0[\fg-o \fcoutputfile\f0] [\fg-breakcircles\f0] [\fg-scc\f0]"
    cp "  Remove superfluous edges"
  if showDetails "purify" then
    cp "  \fg-breakcircles\f0\fx       Break circular dependencies (instead of giving an error). (\fycurrently flawed\f0)"
    cp "  \fg-scc\f0\fx                Prune the graph of strongly connected components (and project that back) instead of the whole graph"
    cp ""
  if showSection "filter" then
    cp "\fographops filter \fg-i \fcinputfile\f0 [\fg-include\f0|\fg-exclude\f0] {\fg-n \fctag\f0} {\fg-nf \fctagfile.csv\f0} [\fg-o \fcoutputfile\f0]"
    cp "  Create a subgraph keeping (\fg-include\f0) or removing (\fg-exclude\f0) the nodes with the specified \fctag\f0s"
  if showDetails "filter" then
    cp "  \fg-include\f0\fx            Keep the specified nodes, remove all others"
    cp "  \fg-exclude\f0\fx            Remove the specified nodes, keep all others"
    cp "  \fg-n \fctag\f0              The tags used to match node(s) to keep or remove. Any resulting dangling edges are removed."
    cp "  \fx\fx\fx                    Tags can use the \fckey\fo::\fctag\f0 format."
    cp "  \fx\fx\fx                    (to remove nodes by name instead of tag use \fographops prune\f0 instead)"
    cp "  \fg-nf \fctags.csv\f0        Load tags from a CSV file, using the required 'tag' and optional 'category' columns"
    cp "  \fx\fx\fx                    You can comment out lines in the CSV file by starting the line with '#'."
    cp ""
  if showSection "prune" then
    cp "\fographops prune \fg-i \fcinputfile\f0 {\fg-e \fcfrom\f0|\fo* \fCto\f0|\fo*\f0} {\fg-n \fcnode\f0} [\fg-o \fcoutputfile\f0]"
    cp "  Remove the specified edges from the graph"
  if showDetails "prune" then
    cp "  \fg-e \fcfrom \fCto\f0          Remove the edge between nodes \fcfrom\f0 and \fCto\f0."
    cp "  \fg-e \fo* \fCto\f0             Remove \foall\f0 edges to node \fCto\f0."
    cp "  \fg-e \fcfrom \fo*\f0           Remove \foall\f0 edges from node \fcfrom\f0."
    cp "  \fg-n \fcnode\f0\fx             Remove the node named '\fcnode\f0' and its edges."
    cp ""
  if showSection "dot" then
    cp "\fographops dot \fg-i \fcinputfile \f0[\fg-o \fcoutputfile\f0] [[\fg-ports\f0]|\fg-prop \fcproperty\f0|\fg-cluster\f0] [\fg-colorize \f0[\fonone\f0|\foports\f0]] [\fg-lr\f0]"
    cp "  Export a GraphViz *.dot file describing the graph"
  if showDetails "dot" then
    cp "  \fg-ports \fx\f0             (default) Create same-rank subgraphs (not clusters) for seeds and sinks"
    cp "  \fg-prop \fcproperty\f0      Use the named property to cluster nodes"
    cp "  \fg-cluster \fx\f0           Create clusters for seeds, sinks, and others"
    cp "  \fg-colorize \fonone\f0      (default) Do not colorize nodes"
    cp "  \fg-colorize \foports\f0     Colorize seed and sink nodes"
    cp "  \fg-lr\f0\fx                 Lay out the graph from left to right instead of top to bottom"
    cp ""
  if showSection "supergraph" then
    cp "\fographops supergraph \fg-i \fcinputfile\f0 [\fg-prop \fcproperty\f0] [\fg-o \fcoutputfile\f0] [\fg-prefix \fcseparator \fCmaxcount\f0] [\fg-nonodes\f0]"
    cp "  Calculate a supergraph, merging all 'equivalent' nodes into one"
  if showDetails "supergraph" then
    cp "  \fg-prop \fcproperty\f0      If given, 'equivalent' means 'same value for the given node \fcproperty\f0' (default 'category')"
    cp "  \fg-prefix \fcseparator \fCmaxcount\f0    Synthesize a property 'prefix' containing the common prefix of contained node names"
    cp "  \fx        \fx          \fx        \fx    Prefixes are based on up to \fCmaxcount\f0 segments when splitting names with \fcseparator\f0."
    cp "  \fg-nonodes\fo\fx            Skip emitting the 'node' keyed tags in the output"
    cp ""
  if showSection "csv" then
    cp "\fographops csv \f0[\foexport\f0] \fg-i \fcinputfile\f0 {\fg-np \fo*\f0|\fcproperty\f0} [\fg-pio \fcprop\f0]"
    cp "  Export a graph to a set of CSV files: one for nodes (and their properties), one for edges, one for node tags (including keyed node tags)."
  if showDetails "csv" then
    cp "  \fg-np \fo*\f0               Export all node properties as columns in the node list."
    cp "  \fg-np \fcproperty\f0        Export the named node property as a column in the node list and in the edge list for source and target nodes"
    cp "  \fg-pio \fcprop\f0           Include information on incoming, outgoing, and internal edges in the partition implied by node property \fcprop\f0."
    cp ""
  if true then
    cp "Common options:"
    cp "  \fg-h             \f0Show help"
    cp "  \fg-v             \f0Verbose mode"



