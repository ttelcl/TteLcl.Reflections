using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TteLcl.Reflections.ConfigurationFiles;

/// <summary>
/// A model for the runtime configuration file for "modern" applications (.NET Core and newer)
/// This contains the relevant bits of the application's *.runtimconfig.json file
/// </summary>
public partial class ModernConfigFile
{

  /// <summary>
  /// Load a *.runtimeconfig.json file.
  /// </summary>
  /// <param name="fileName"></param>
  /// <exception cref="InvalidOperationException"></exception>
  public ModernConfigFile(string fileName)
  {
    FileName = Path.GetFullPath(fileName);
    BasePath = Path.GetDirectoryName(FileName)!;
    var json = File.ReadAllText(FileName);
    Configuration = JsonConvert.DeserializeObject<RuntimeConfig>(json) ??
      throw new InvalidOperationException(
        $"Deserialization error while loading {FileName}");
  }

  /// <summary>
  /// The full path to the configuration file modeled in this object
  /// </summary>
  public string FileName { get; }

  /// <summary>
  /// The folder containing the seed assembly and configuration.
  /// </summary>
  public string BasePath { get; }

  /// <summary>
  /// The content of the configuration file (using our partial model)
  /// </summary>
  public RuntimeConfig Configuration { get; }
}
