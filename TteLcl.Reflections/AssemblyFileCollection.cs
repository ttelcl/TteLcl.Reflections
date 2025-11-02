/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections;

/// <summary>
/// Builds a collection of assembly file paths, for passing on to the
/// constructor of <see cref="PathAssemblyResolver"/>
/// </summary>
public class AssemblyFileCollection
{
  private readonly HashSet<string> _assemblyFiles;

  /// <summary>
  /// Create a new <see cref="AssemblyFileCollection"/>
  /// </summary>
  public AssemblyFileCollection()
  {
    _assemblyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Add the specified file to this set (duplicates are ignored)
  /// </summary>
  /// <param name="fileName"></param>
  public void AddFile(string fileName)
  {
    _assemblyFiles.Add(Path.GetFullPath(fileName));
  }

  /// <summary>
  /// Add all matching files in the folder to this set
  /// </summary>
  /// <param name="folderName">
  /// The name of the folder containing the assembly files to add
  /// </param>
  /// <param name="subFolders">
  /// If true also include assembly files in subfolders
  /// </param>
  /// <param name="pattern">
  /// The file pattern for assembly files. Defaults to <c>*.dll</c>.
  /// </param>
  /// <returns>
  /// The number of files added (excluding duplicates)
  /// </returns>
  public int AddFolder(string folderName, bool subFolders, string pattern = "*.dll")
  {
    folderName = Path.GetFullPath(folderName);
    var count = 0;
    if(Directory.Exists(folderName))
    {
      foreach(var fileName in Directory.EnumerateFiles(
        folderName, pattern, subFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
      {
        if(_assemblyFiles.Add(fileName))
        {
          count++;
        }
      }
    }
    return count;
  }

  /// <summary>
  /// Add assemblies in the .NET Framework GAC. 
  /// <b>This should only be used when targeting ye olde .NET Framework,
  /// not when targeting .NET Core or .NET 5+</b>.
  /// </summary>
  /// <param name="msil">
  /// Include "Any CPU" assemblies (default true)
  /// </param>
  /// <param name="x64">
  /// Include 64 bit assemblies (default true)
  /// </param>
  /// <param name="x32">
  /// Include 32 bit assemblies (default false)
  /// </param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if both <paramref name="x64"/> and <paramref name="x32"/> are given
  /// </exception>
  /// <returns>
  /// The number of assemblies added
  /// </returns>
  public int AddGac(bool msil = true, bool x64 = true, bool x32 = false)
  {
    if(x64 && x32)
    {
      throw new InvalidOperationException(
        "x64 and x32 are mutually exclusive");
    }
    var basePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.Windows),
      "Microsoft.NET",
      "assembly");
    var count = 0;
    if(msil)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_MSIL"), true);
    }
    if(x64)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_64"), true);
    }
    if(x32)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_32"), true);
    }
    return count;
  }

}
