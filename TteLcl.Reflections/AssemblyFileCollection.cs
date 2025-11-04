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
  private readonly Dictionary<string, HashSet<AssemblyFileInfo>> _assemblyFiles;

  /// <summary>
  /// Create a new <see cref="AssemblyFileCollection"/>
  /// </summary>
  public AssemblyFileCollection(LoadSystem loadSystem = LoadSystem.Undefined)
  {
    _assemblyFiles = new Dictionary<string, HashSet<AssemblyFileInfo>>();
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
  /// <returns>
  /// True if the file was added, false if it was already present
  /// </returns>
  public bool AddFile(string fileName)
  {
    var assumedName = Path.GetFileNameWithoutExtension(fileName);
    if(!_assemblyFiles.TryGetValue(assumedName, out var files))
    {
      files = new HashSet<AssemblyFileInfo>();
      _assemblyFiles[assumedName] = files;
    }
    // The constructor ensures the full path is stored
    var afi = new AssemblyFileInfo(fileName);
    return files.Add(afi);
  }

  /// <summary>
  /// Enumerate the distinct <see cref="AssemblyFileInfo"/> objects cached in this
  /// collection. Equivalent to <c>AsseemblyTags.SelectMany(tag => FindAssemblyCandidates(tag))</c>
  /// (but implemented more efficiently)
  /// </summary>
  public IEnumerable<AssemblyFileInfo> AssemblyFileInfos =>
    _assemblyFiles.Values.SelectMany(afset => afset);

  /// <summary>
  /// Enumerate the list of distinct "assembly tags" in this collection. An assembly tag
  /// is the potential name of an assembly as derived from an assembly file. It is possible
  /// that this is not a valid assembly name at all. Additionally, casing may be incorrect.
  /// </summary>
  public IReadOnlyCollection<string> AssemblyTags => _assemblyFiles.Keys;

  /// <summary>
  /// Returns the set of <see cref="AssemblyFileInfo"/> objects in this collection associated
  /// with the given short assembly <paramref name="name"/>. Returns null if the name is not known
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  public IReadOnlySet<AssemblyFileInfo>? FindAssemblyCandidates(string name)
  {
    return _assemblyFiles.TryGetValue(name, out var candidates) ? candidates : null;
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
        if(AddFile(fileName))
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

  /// <summary>
  /// Add the given seed assembly, other assemblies in the same directory, and the framework files
  /// associated with the seed. The seed assembly should have a run time configuration file alongside
  /// it, either a *.config file (.NET Framework) or a *.runtimeconfiguration.json (modern .NET)
  /// </summary>
  /// <param name="seedAssembly"></param>
  /// <returns></returns>
  public int Seed(string seedAssembly)
  {
    if(!File.Exists(seedAssembly))
    {
      throw new ArgumentException(
        $"Expecting an existing file, but got {seedAssembly}");
    }
    var classicProbe = seedAssembly + ".config";
    var modernProbe = Path.ChangeExtension(seedAssembly, ".runtimeconfiguration.json");
    var classicExists = Path.Exists(classicProbe);
    var modernExists = Path.Exists(modernProbe);
    if(classicExists)
    {
      if(modernExists)
      {
        throw new InvalidOperationException(
          $"Ambiguous seed: both modern and classic configurations exist: '{modernProbe}' and '{classicProbe}'");
      }
      else
      {
        return SeedClassic(seedAssembly, classicProbe);
      }
    }
    else
    {
      if(modernExists)
      {
        return SeedModern(seedAssembly, modernProbe);
      }
      else
      {
        throw new InvalidOperationException(
          $"No configuration found. Neither '{modernProbe}' nor '{classicProbe}' exist");
      }
    }
  }

  private int SeedClassic(string seedAssembly, string configFile)
  {
    throw new NotImplementedException(); 
  }

  private int SeedModern(string seedAssembly, string configFile)
  {
    throw new NotImplementedException();
  }
}
