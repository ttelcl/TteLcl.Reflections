using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TteLcl.Reflections;

/// <summary>
/// A model for the runtime configuration file for "modern" applications (.NET Core and newer)
/// This contains the relevant bits of the application's *.runtimconfig.json file
/// </summary>
public class ModernConfigFile
{

  public ModernConfigFile(string fileName)
  {
  }

  /// <summary>
  /// Models a "framework" record in the config file
  /// </summary>
  public class Framework
  {
    /// <summary>
    /// JSON deserialization constructor
    /// </summary>
    [JsonConstructor]
    public Framework(
      string name,
      string version)
    {
      Name = name;
      Version = Version.Parse(version);
      VersionText = version;
    }

    /// <summary>
    /// The framework name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; }

    /// <summary>
    /// The framework version (in text form)
    /// </summary>
    [JsonProperty("version")]
    public string VersionText { get; }

    /// <summary>
    /// <see cref="VersionText"/> parsed to a <see cref="System.Version"/> instance
    /// </summary>
    [JsonIgnore]
    public Version Version { get; }
  }

  /// <summary>
  /// The runtime option container inside the configuration file
  /// </summary>
  public class RuntimeOptions
  {
    /// <summary>
    /// JSON deserialialization constructor
    /// </summary>
    [JsonConstructor]
    public RuntimeOptions(
      string tfm,
      IEnumerable<Framework> frameworks)
    {
      Tfm = tfm;
      Frameworks = frameworks.ToList();
    }

    /// <summary>
    /// The TFM
    /// </summary>
    [JsonProperty("tfm")]
    public string Tfm { get; init; }

    /// <summary>
    /// The list of frameworks used
    /// </summary>
    [JsonProperty("frameworks")]
    public IReadOnlyList<Framework> Frameworks { get; }
  }

  public class RuntimeConfig
  {
    public RuntimeConfig(RuntimeOptions runtimeoptions)
    {
      // WORK IN PROGRESS
    }
  }
}
