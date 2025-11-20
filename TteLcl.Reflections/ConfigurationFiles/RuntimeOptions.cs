using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace TteLcl.Reflections.ConfigurationFiles;

/// <summary>
/// The runtime option container inside the configuration file
/// </summary>
public class RuntimeOptions
{
  /// <summary>
  /// JSON deserialialization constructor
  /// </summary>
  /// <param name="tfm"></param>
  /// <param name="frameworks"></param>
  /// <param name="additionalProbingPaths"></param>
  [JsonConstructor]
  public RuntimeOptions(
    string tfm,
    IEnumerable<Framework> frameworks,
    IEnumerable<string>? additionalProbingPaths = null)
  {
    Tfm = tfm;
    Frameworks = frameworks.ToList();
    AdditionalProbingPaths = new List<string>(additionalProbingPaths ?? []);
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

  /// <summary>
  /// The additional probing paths (strings). Behaviour is so ill-defined as to be useless :(
  /// </summary>
  [JsonProperty("additionalProbingPaths")]
  public IReadOnlyList<string> AdditionalProbingPaths { get; }

  /// <summary>
  /// Used by the JSON serializer to decide if <see cref="AdditionalProbingPaths"/> should
  /// be serialized. Returns false if that is empty
  /// </summary>
  /// <returns></returns>
  public bool ShouldSerializeAdditionalProbingPaths()
  {
    return AdditionalProbingPaths.Count > 0;
  }
}
