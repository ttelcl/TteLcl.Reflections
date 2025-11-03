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
  public AssemblyFileCollection(LoadSystem loadSystem = LoadSystem.Undefined)
  {
    _assemblyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    LoadSystem = loadSystem;
  }

  /// <summary>
  /// The load system
  /// </summary>
  public LoadSystem LoadSystem { get; private set; }

  /// <summary>
  /// The bitness, if locked in
  /// </summary>
  public BitMode BitMode { get; private set; }

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
        // Blacklist known false positives
        if(fileName.EndsWith(".resource.dll") || fileName.EndsWith(".ni.dll"))
        {
          // not comparing case insensitively is more or less on purpose
          continue;
        }
        if(_assemblyFiles.Add(fileName))
        {
          count++;
        }
      }
    }
    return count;
  }

  /// <summary>
  /// Add a .NET Core (or later) "framework" (who came up with that confusing name???)
  /// </summary>
  /// <param name="frameworkName">
  /// The name of the framework to add (for instance as found in a *.runtimeconfig.json file).
  /// Example: "<c>Microsoft.NETCore.App</c>".
  /// </param>
  /// <param name="version">
  /// The version of that framework, as a <see cref="Version"/>.
  /// Example: '<c>8.0.0</c>'. The selected version will be
  /// the highest installed one with the same major version number (e.g. <c>8.0.21</c>)
  /// </param>
  /// <param name="bits64">
  /// True to select the 64 bit version (default true)
  /// </param>
  /// <returns>
  /// The number of assemblies added
  /// </returns>
  public int AddCoreFramework(
    string frameworkName,
    Version version,
    bool bits64 = true)
  {
    if(LoadSystem == LoadSystem.NetFramework)
    {
      throw new InvalidOperationException(
        "Cannot include .NET Core frameworks: already comitted to .NET Framework, not .NET Core descendants");
    }
    if(bits64 && BitMode == BitMode.Bit32)
    {
      throw new InvalidOperationException(
        "Cannot include 64 bit .NET Core: already committed to 32 bit");
    }
    if(!bits64 && BitMode == BitMode.Bit64)
    {
      throw new InvalidOperationException(
        "Cannot include 32 bit .NET Core: already committed to 64 bit");
    }
    BitMode = bits64 ? BitMode.Bit64 : BitMode.Bit32;
    LoadSystem = LoadSystem.NetCore;
    var baseFolder =
      Path.Combine(
        Environment.GetFolderPath(
           bits64 ? Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.ProgramFilesX86),
        "dotnet",
        "shared");
    var frameworkFolder = Path.Combine(baseFolder, frameworkName);
    if(!Directory.Exists(frameworkFolder))
    {
      throw new DirectoryNotFoundException(
        $"The requested framework was not found (not installed?): {frameworkFolder}");
    }
    var frameworkDirectory = new DirectoryInfo(frameworkFolder);
    var versionedDirectories = 
      frameworkDirectory.EnumerateDirectories()
      .Where(d => Version.TryParse(d.Name, out var fdv) && fdv.Major == version.Major && fdv >= version)
      .ToList();
    if(versionedDirectories.Count == 0)
    {
      throw new InvalidOperationException(
        $"No framework matching version '{version}' found in framework folder {frameworkFolder}");
    }
    // in case there are multiple matches take the highest version
    var fwFolder =
      (versionedDirectories.MaxBy(di => Version.Parse(di.Name)))?.FullName
      ?? throw new InvalidOperationException("Unexpected: no maximum for FW version?");
    return AddFolder(fwFolder, false);
  }

  /// <summary>
  /// Add a .NET Core (or later) "framework" (who came up with that confusing name???)
  /// </summary>
  /// <param name="frameworkName">
  /// The name of the framework to add (for instance as found in a *.runtimeconfig.json file).
  /// Example: "<c>Microsoft.NETCore.App</c>".
  /// </param>
  /// <param name="version">
  /// The version of that framework, as a <see cref="string"/>.
  /// Example: <c>"8.0.0"</c>. The selected version will be
  /// the highest installed one with the same major version number (e.g. <c>8.0.21</c>)
  /// </param>
  /// <param name="bits64">
  /// True to select the 64 bit version (default true)
  /// </param>
  /// <returns>
  /// The number of assemblies added
  /// </returns>
  public int AddCoreFramework(
    string frameworkName,
    string version,
    bool bits64 = true)
  {
    if(!Version.TryParse(version, out var v))
    {
      throw new ArgumentOutOfRangeException(
        nameof(version),
        $"'{version}' is not a recognized version string");
    }
    return AddCoreFramework(frameworkName, v, bits64);
  }

  /// <summary>
  /// Add ye olde .net framework
  /// </summary>
  /// <param name="bits64">
  /// True to load the 64 bit version (default true)
  /// </param>
  /// <param name="runtimeprefix">
  /// The prefix for the runtime. "4.0" for the final version (.NET Framework
  /// 4.8 / 4.8.1 / 4.8.2 uses runtime 4.0; anything older than that is probably
  /// no longer relevant)
  /// </param>
  /// <returns>
  /// The number of DLLs added
  /// </returns>
  public int AddDotnetFramework(bool bits64 = true, string runtimeprefix = "4.0")
  {
    if(LoadSystem == LoadSystem.NetCore)
    {
      throw new InvalidOperationException(
        "Cannot include .NET Framework: already comitted to .NET Core and its descendants");
    }
    if(bits64 && BitMode == BitMode.Bit32)
    {
      throw new InvalidOperationException(
        "Cannot include 64 bit .NET Framework: already committed to 32 bit");
    }
    if(!bits64 && BitMode == BitMode.Bit64)
    {
      throw new InvalidOperationException(
        "Cannot include 32 bit .NET Framework: already committed to 64 bit");
    }
    BitMode = bits64 ? BitMode.Bit64 : BitMode.Bit32;
    LoadSystem = LoadSystem.NetFramework;
    var basePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.Windows),
      "Microsoft.NET",
      bits64 ? "Framework64" : "Framework");
    var root = new DirectoryInfo(basePath);
    var frameworks =
      root.EnumerateDirectories()
      .Where(d => d.Name.StartsWith(runtimeprefix))
      .ToList();
    string frameworkFolder;
    if(frameworks.Count == 0)
    {
      var pseudoPath = Path.Combine(basePath, runtimeprefix) + "*";
      throw new InvalidOperationException(
        $"Framework not found: {pseudoPath}");
    }
    else if(frameworks.Count > 1)
    {
      // Should not happen, but pick the alphabetically newest
      frameworkFolder =
        (from fw in frameworks orderby fw.Name descending select fw)
        .First().FullName;
    }
    else
    {
      frameworkFolder = frameworks[0].FullName;
    }
    return AddFolder(frameworkFolder, true);
  }

  /// <summary>
  /// Add assemblies in the .NET Framework GAC. 
  /// <b>This should only be used when targeting ye olde .NET Framework,
  /// not when targeting .NET Core or .NET 5+</b>. Even then it may be the wrong move.
  /// </summary>
  /// <param name="bitMode">
  /// Which GAC parts to include. MSIL is always included, but this optionally allows also
  /// 32 bit or 64 bit parts to be included
  /// </param>
  /// <returns>
  /// The number of assemblies added
  /// </returns>
  public int AddGac(BitMode bitMode = BitMode.Bit64)
  {
    if(LoadSystem == LoadSystem.NetCore)
    {
      throw new InvalidOperationException(
        "Cannot include GAC: already comitted to .NET Core (not .NET Framework)");
    }
    if(bitMode == BitMode.Bit32 && BitMode == BitMode.Bit64)
    {
      throw new InvalidOperationException(
        "Cannot include 32 bit GAC: already committed to 64 bit GAC");
    }
    if(bitMode == BitMode.Bit64 && BitMode == BitMode.Bit32)
    {
      throw new InvalidOperationException(
        "Cannot include 64 bit GAC: already committed to 32 bit GAC");
    }
    if(bitMode != BitMode.AnyCpu)
    {
      BitMode = bitMode;
    }
    LoadSystem = LoadSystem.NetFramework;
    var basePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.Windows),
      "Microsoft.NET",
      "assembly");
    var count = 0;
    // Add neutral GAC content
    count += AddFolder(Path.Combine(basePath, "GAC_MSIL"), true);
    if(bitMode == BitMode.Bit64)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_64"), true);
    }
    if(bitMode == BitMode.Bit32)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_32"), true);
    }
    return count;
  }

}
