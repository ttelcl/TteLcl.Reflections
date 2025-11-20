using System.Collections.Generic;

using Newtonsoft.Json;

namespace TteLcl.Reflections.ConfigurationFiles;

/// <summary>
/// Partial model for the content of a *.runtimeconfig.json file
/// </summary>
public class RuntimeConfig
{
  /// <summary>
  /// JSON deserialization constructor for <see cref="RuntimeConfig"/>
  /// </summary>
  /// <param name="runtimeOptions"></param>
  [JsonConstructor]
  public RuntimeConfig(
    RuntimeOptions runtimeOptions)
  {
    RuntimeOptions = runtimeOptions;
  }

  /// <summary>
  /// Container for runtime options (the object named "runtimeoptions" in JSON)
  /// </summary>
  [JsonProperty("runtimeOptions")]
  public RuntimeOptions RuntimeOptions { get; }
}
