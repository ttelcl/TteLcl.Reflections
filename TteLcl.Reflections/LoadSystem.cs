using System;

namespace TteLcl.Reflections;

/// <summary>
/// The set of assumptions and rules for assemblies
/// </summary>
public enum LoadSystem
{
  /// <summary>
  /// Not yet locked in
  /// </summary>
  Undefined = 0,

  /// <summary>
  /// .NET Framework mode (up to 4.8).
  /// Standard libraries come from the GAC, configuration data is in a "*.exe.config" XML file.
  /// </summary>
  NetFramework = 1,

  /// <summary>
  /// .NET Core mode (including .net5+)
  /// Standard libraries come from shared frameworks, configuration data is in
  /// "*.runtimeconfig.json" JSON file.
  /// </summary>
  NetCore = 2,

}
