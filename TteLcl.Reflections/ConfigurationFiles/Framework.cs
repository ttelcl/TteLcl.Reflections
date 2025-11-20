using System;

using Newtonsoft.Json;

namespace TteLcl.Reflections.ConfigurationFiles;

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

