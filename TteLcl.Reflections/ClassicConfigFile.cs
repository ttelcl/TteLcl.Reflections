using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace TteLcl.Reflections;

/// <summary>
/// Model for a classic .NET configuration file (*.config)
/// </summary>
public class ClassicConfigFile
{
  /// <summary>
  /// Create a <see cref="ClassicConfigFile"/> and parse the relevant bits of the config file.
  /// </summary>
  /// <param name="fileName"></param>
  /// <exception cref="NotSupportedException">
  /// Thrown if no 'supported runtime' element was found
  /// </exception>
  public ClassicConfigFile(
    string fileName)
  {
    FileName = Path.GetFullPath(fileName);
    var doc = new XPathDocument(FileName);
    var nav = doc.CreateNavigator();
    var nsmgr = new XmlNamespaceManager(nav.NameTable);
    nsmgr.AddNamespace("a", "urn:schemas-microsoft-com:asm.v1");
    nav.MoveToRoot();
    var supportedRuntime = nav.Evaluate("string(/configuration/startup/supportedRuntime/@version)", nsmgr);
    if(supportedRuntime is string s)
    {
      SupportedRuntime = s;
    }
    else
    {
      throw new NotSupportedException(
        $"'supportedRuntime' element missing in configuration file {fileName}");
    }
    BasePath = Path.GetDirectoryName(FileName)!;
    var privatePath = nav.Evaluate("string(/configuration/runtime/a:assemblyBinding/a:probing/@privatePath)", nsmgr);
    if(privatePath is string pp && !String.IsNullOrEmpty(pp))
    {
      PrivatePath = pp;
      var parts = pp.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      PrivatePathEntries = parts.ToList().AsReadOnly();
      PrivatePathFolders =
        PrivatePathEntries.Select(p => Path.Combine(BasePath, p))
        .ToList()
        .AsReadOnly();
    }
    else
    {
      PrivatePath = String.Empty;
      PrivatePathFolders = [];
      PrivatePathEntries = [];
    }
  }

  /// <summary>
  /// The full path to the configuration file modeled in this object
  /// </summary>
  public string FileName { get; }

  /// <summary>
  /// The supported runtime (nowadays expected to be "4.0")
  /// </summary>
  public string SupportedRuntime { get; }

  /// <summary>
  /// The private assembly search path (may be empty)
  /// </summary>
  public string PrivatePath { get; }

  /// <summary>
  /// The folder containing the seed assembly and configuration.
  /// </summary>
  public string BasePath { get; }
  
  /// <summary>
  /// <see cref="PrivatePath"/> split into separate folders expanded to full paths
  /// </summary>
  public IReadOnlyList<string> PrivatePathFolders { get; }

  /// <summary>
  /// <see cref="PrivatePath"/> split into separate folders <i>relative to <see cref="BasePath"/></i>
  /// </summary>
  public IReadOnlyList<string> PrivatePathEntries { get; }
}
