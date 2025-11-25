/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.AssemblyFiles;

/// <summary>
/// Low level reflection utilities on .NET assemblies
/// </summary>
public static class AsmReflection
{
  /// <summary>
  /// Check if the given file exists and is a .NET assembly.
  /// </summary>
  /// <param name="fileName">
  /// The *.dll or *.exe file to check
  /// </param>
  /// <param name="assemblyName">
  /// Upon success: the <see cref="AssemblyName"/> for the assembly stored in
  /// the file. Null otherwise.
  /// </param>
  /// <returns>
  /// True if the file is an assembly. False if it is not, doesn't exist or
  /// could not be accessed.
  /// </returns>
  public static bool TryGetAssemblyName(
    string fileName,
    [NotNullWhen(true)] out AssemblyName? assemblyName)
  {
    try
    {
      assemblyName = MetadataReader.GetAssemblyName(fileName);
      return assemblyName != null;
    }
    catch
    {
      assemblyName = null;
    }
    return false;
  }

}
