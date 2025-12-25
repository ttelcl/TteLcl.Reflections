using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TteLcl.Reflections.AssemblyFiles;

/// <summary>
/// Reports a summary of a <see cref="AssemblyFileInfo"/> and whether
/// it was used or not in a serializable form.
/// </summary>
public class AssemblyFileUsage
{
  /// <summary>
  /// Deserialization constructor
  /// </summary>
  /// <param name="used"></param>
  /// <param name="assembly"></param>
  /// <param name="module"></param>
  /// <param name="file"></param>
  /// <param name="version"></param>
  [JsonConstructor]
  public AssemblyFileUsage(
    bool used,
    string assembly,
    string module,
    string file,
    string? version)
  {
    IsUsed = used;
    AssemblyTag = assembly;
    Module = module;
    FileName = file;
    AssemblyVersion = version;
  }

  /// <summary>
  /// A flag indicating if this registration was actually used.
  /// Mutable.
  /// </summary>
  [JsonProperty("used")]
  public bool IsUsed { get; set; }

  /// <summary>
  /// The short assembly name
  /// </summary>
  [JsonProperty("assembly")]
  public string AssemblyTag { get; }

  /// <summary>
  /// The assembly version
  /// </summary>
  [JsonProperty("version")]
  public string? AssemblyVersion { get; }

  /// <summary>
  /// The primary tag (a.k.a. 'module'), grouping assembly files by file
  /// location or other grouping criterion
  /// </summary>
  [JsonProperty("module")]
  public string Module { get; }

  /// <summary>
  /// The name of the assembly file
  /// </summary>
  [JsonProperty("file")]
  public string FileName { get; }
}
