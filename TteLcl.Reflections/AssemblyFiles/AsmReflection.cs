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

  /// <summary>
  /// Find the assembly names of friend assemblies (InternalsVisibleTo attribute values).
  /// This method does so without instantiating those attributes.
  /// </summary>
  /// <param name="assembly"></param>
  /// <returns></returns>
  public static IEnumerable<string> FindFriendAssemblyNames(Assembly assembly)
  {
    // Note that we need to use the assembly without accidentally instancing anything it
    // (assuming we work  with metadata-only assemblies). So we cannot use the normal
    // attribute loading methods.
    var names =
      assembly.GetCustomAttributesData()
        .Where(cad => cad.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute")
        .Select(cad => cad.ConstructorArguments.FirstOrDefault().Value?.ToString())
        .Where(name => name != null)
        .Select(name => name!);
    return names;
  }

}
