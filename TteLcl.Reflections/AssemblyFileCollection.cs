/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections;

/// <summary>
/// Builds a collection of assembly file paths, for passing on to the
/// constructor of <see cref="PathAssemblyResolver"/>
/// </summary>
public class AssemblyFileCollection
{
  // Maps assumed assembly names to a set of AssemblyFileInfo objects, each of which
  // is mapped to a set of tags. That "set of AssemblyFileInfo objects" are the keys
  // of the inner dictionaries
  private readonly Dictionary<string, HashSet<AssemblyFileInfo>> _assemblyFiles;
  private readonly Dictionary<string, IReadOnlySet<AssemblyFileInfo>> _assemblyFilesView;
  private readonly Dictionary<AssemblyFileInfo, HashSet<string>> _assemblyTags;
  private readonly Dictionary<AssemblyFileInfo, IReadOnlySet<string>> _assemblyTagsView;

  /// <summary>
  /// Create a new <see cref="AssemblyFileCollection"/>
  /// </summary>
  public AssemblyFileCollection(LoadSystem loadSystem = LoadSystem.Undefined)
  {
    _assemblyFiles = new Dictionary<string, HashSet<AssemblyFileInfo>>(StringComparer.OrdinalIgnoreCase);
    _assemblyFilesView = new Dictionary<string, IReadOnlySet<AssemblyFileInfo>>(StringComparer.OrdinalIgnoreCase);
    _assemblyTags = new Dictionary<AssemblyFileInfo, HashSet<string>>();
    _assemblyTagsView = new Dictionary<AssemblyFileInfo, IReadOnlySet<string>>();
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
  public IReadOnlyCollection<string> AssemblyKeys => _assemblyFiles.Keys;

  /// <summary>
  /// A view on the assemblies in this collection, grouped by assumed assembly name
  /// </summary>
  public IReadOnlyDictionary<string, IReadOnlySet<AssemblyFileInfo>> AssembliesByName => _assemblyFilesView;

  /// <summary>
  /// Mapping from <see cref="AssemblyFileInfo"/> to the set of tag strings.
  /// </summary>
  public IReadOnlyDictionary<AssemblyFileInfo, IReadOnlySet<string>> AssemblyTags => _assemblyTagsView;

  /// <summary>
  /// Add the specified file to this set (duplicates are ignored)
  /// </summary>
  /// <param name="fileName"></param>
  /// <param name="tags"></param>
  /// <returns>
  /// True if the file was added, false if it was already present
  /// </returns>
  public bool AddFile(string fileName, IEnumerable<string> tags)
  {
    var assumedName = Path.GetFileNameWithoutExtension(fileName);
    if(!_assemblyFiles.TryGetValue(assumedName, out var files))
    {
      files = new HashSet<AssemblyFileInfo>();
      _assemblyFiles[assumedName] = files;
      _assemblyFilesView[assumedName] = files;
    }
    // The constructor ensures the full path is stored
    var afi = new AssemblyFileInfo(fileName);
    var added = files.Add(afi);
    if(!_assemblyTags.TryGetValue(afi, out var tagset))
    {
      tagset = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      _assemblyTags[afi] = tagset;
      _assemblyTagsView[afi] = tagset;
    }
    foreach(var tag in tags)
    {
      tagset.Add(tag);
    }
    return added;

  }

  /// <summary>
  /// Return the assemblies grouped by tags
  /// </summary>
  /// <returns></returns>
  public IReadOnlyDictionary<string, IReadOnlySet<AssemblyFileInfo>> GetAssembliesByTag()
  {
    var assemblyTagPairs =
      _assemblyTagsView
      .SelectMany(kvp => kvp.Value.Select(tag => (kvp.Key, tag)));
    var groupedByTag =
      assemblyTagPairs
      .GroupBy(pair => pair.tag, StringComparer.OrdinalIgnoreCase);
    var assembliesByTag =
      groupedByTag
      .ToDictionary(
        g => g.Key,
        g => g.Select(pair => pair.Key).ToHashSet() as IReadOnlySet<AssemblyFileInfo>,
        StringComparer.OrdinalIgnoreCase);
    return assembliesByTag;
  }

  /// <summary>
  /// Return the assemblies that have the given tag
  /// </summary>
  public IReadOnlySet<AssemblyFileInfo> GetAssembliesByTag(string tag)
  {
    var result =
      _assemblyTagsView
      .Where(kvp => kvp.Value.Contains(tag))
      .Select(kvp => kvp.Key)
      .ToHashSet();
    return result;
  }

  /// <summary>
  /// Get the assemblies in this collection that have the assumed assembly name
  /// '<paramref name="assemblyName"/>' (case insensitively)
  /// </summary>
  /// <param name="assemblyName">
  /// The name to search for
  /// </param>
  /// <param name="assemblyFileInfos">
  /// The set of <see cref="AssemblyFileInfo"/>s found (or null if not found)
  /// </param>
  /// <returns>
  /// True if found (and <paramref name="assemblyFileInfos"/> is not null), false
  /// otherwise
  /// </returns>
  public bool TryGetAssemblies(
    string assemblyName,
    [NotNullWhen(true)] out IReadOnlySet<AssemblyFileInfo>? assemblyFileInfos)
  {
    return _assemblyFilesView.TryGetValue(assemblyName, out assemblyFileInfos);
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
  /// <param name="tags">
  /// Tags to add to added files
  /// </param>
  /// <returns>
  /// The number of files added (excluding duplicates)
  /// </returns>
  public int AddFolder(string folderName, bool subFolders, IEnumerable<string> tags, string pattern = "*.dll")
  {
    folderName = Path.GetFullPath(folderName);
    var count = 0;
    tags ??= [];
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
        if(AddFile(fileName, tags))
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
    if(bits64 && BitMode == BitMode.X86)
    {
      throw new InvalidOperationException(
        "Cannot include 64 bit .NET Core: already committed to 32 bit");
    }
    if(!bits64 && BitMode == BitMode.X64)
    {
      throw new InvalidOperationException(
        "Cannot include 32 bit .NET Core: already committed to 64 bit");
    }
    BitMode = bits64 ? BitMode.X64 : BitMode.X86;
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
    var fwFolderInfo = versionedDirectories.MaxBy(di => Version.Parse(di.Name));
    var bitnessTag = bits64 ? "x64" : "x86";
    var fwFolder = fwFolderInfo?.FullName ?? throw new InvalidOperationException("Unexpected: no maximum for FW version?");
    var fwFolderTag = fwFolderInfo!.Name;
    var fwTag = $"net-{fwFolderTag}-{bitnessTag}";
    List<string> tags = [
      $"{frameworkName} ({fwTag})",
    ];
    return AddFolder(fwFolder, false, tags);
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
  /// Try to find an already registered <see cref="AssemblyFileInfo"/> given its path
  /// </summary>
  /// <param name="file">
  /// The path to the already registered assembly file
  /// </param>
  /// <param name="info"></param>
  /// <returns></returns>
  public bool TryFindByFile(
    string file,
    [NotNullWhen(true)] out AssemblyFileInfo? info)
  {
    info = null;
    file = Path.GetFullPath(file);
    var key = Path.GetFileNameWithoutExtension(file);
    if(_assemblyFiles.TryGetValue(key, out var files))
    {
      info = files.FirstOrDefault(
        f => f.FileName.Equals(file, StringComparison.OrdinalIgnoreCase));
      return info != null;
    }
    return false;
  }

  /// <summary>
  /// Try to find an already registered <see cref="AssemblyFileInfo"/> given its already
  /// loaded Assembly
  /// </summary>
  /// <param name="assembly"></param>
  /// <param name="info"></param>
  /// <returns></returns>
  public bool TryFindByAssembly(
    Assembly assembly,
    [NotNullWhen(true)] out AssemblyFileInfo? info)
  {
    var file = assembly.Location;
    if(String.IsNullOrEmpty(file))
    {
      info = null;
      return false;
    }
    return TryFindByFile(file, out info);
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
  public int AddDotnetFramework(bool bits64 = true, string runtimeprefix = "v4.0")
  {
    if(LoadSystem == LoadSystem.NetCore)
    {
      throw new InvalidOperationException(
        "Cannot include .NET Framework: already comitted to .NET Core and its descendants");
    }
    if(bits64 && BitMode == BitMode.X86)
    {
      throw new InvalidOperationException(
        "Cannot include 64 bit .NET Framework: already committed to 32 bit");
    }
    if(!bits64 && BitMode == BitMode.X64)
    {
      throw new InvalidOperationException(
        "Cannot include 32 bit .NET Framework: already committed to 64 bit");
    }
    BitMode = bits64 ? BitMode.X64 : BitMode.X86;
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
    var bitnessTag = bits64 ? "x64" : "x86";
    var fwFolderTag = Path.GetFileName(frameworkFolder);
    var fwTag = $"FW-{fwFolderTag}-{bitnessTag}";
    List<string> tags = [
      fwTag,
    ];
    return AddFolder(frameworkFolder, true, tags);
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
  public int AddGac(BitMode bitMode = BitMode.X64)
  {
    if(LoadSystem == LoadSystem.NetCore)
    {
      throw new InvalidOperationException(
        "Cannot include GAC: already comitted to .NET Core (not .NET Framework)");
    }
    if(bitMode == BitMode.X86 && BitMode == BitMode.X64)
    {
      throw new InvalidOperationException(
        "Cannot include 32 bit GAC: already committed to 64 bit GAC");
    }
    if(bitMode == BitMode.X64 && BitMode == BitMode.X86)
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
    count += AddFolder(Path.Combine(basePath, "GAC_MSIL"), true, ["GAC_MSIL"]);
    if(bitMode == BitMode.X64)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_64"), true, ["GAC_64"]);
    }
    if(bitMode == BitMode.X86)
    {
      count += AddFolder(Path.Combine(basePath, "GAC_32"), true, ["GAC_32"]);
    }
    return count;
  }

  /// <summary>
  /// Add the given seed assembly, other assemblies in the same directory, and the framework files
  /// associated with the seed. The seed assembly should have a run time configuration file alongside
  /// it, either a *.config file (.NET Framework) or a *.runtimeconfiguration.json (modern .NET)
  /// </summary>
  /// <param name="seedAssembly"></param>
  /// <param name="seedTag"></param>
  /// <returns></returns>
  public int Seed(string seedAssembly, string seedTag = "application")
  {
    if(!File.Exists(seedAssembly))
    {
      throw new ArgumentException(
        $"Expecting an existing file, but got {seedAssembly}");
    }
    var classicProbe = seedAssembly + ".config";
    var modernProbe = Path.ChangeExtension(seedAssembly, ".runtimeconfig.json");
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
        return SeedClassic(seedAssembly, classicProbe, seedTag);
      }
    }
    else
    {
      if(modernExists)
      {
        return SeedModern(seedAssembly, modernProbe, seedTag);
      }
      else
      {
        throw new InvalidOperationException(
          $"No configuration found. Neither '{modernProbe}' nor '{classicProbe}' exist");
      }
    }
  }

  /// <summary>
  /// Open a new <see cref="MetadataLoadContext"/> using the assemblies registered
  /// in this collection. Don't forget to dispose it when done!
  /// </summary>
  /// <returns></returns>
  public MetadataLoadContext OpenLoadContext()
  {
    var coreAssembly =
      LoadSystem switch {
        LoadSystem.NetCore => "System.Runtime",
        LoadSystem.NetFramework => "mscorlib",
        _ => null, // let the constructor figure it out
      };
    var resolver = new PathAssemblyResolver(
      AssemblyFileInfos.Select(afi => afi.FileName));
    return new MetadataLoadContext(resolver, coreAssembly);
  }

  private int SeedClassic(string seedAssembly, string configFile, string seedTag)
  {
    var ccf = new ClassicConfigFile(configFile);
    var count = 0;
    count += AddDotnetFramework(bits64: true, runtimeprefix: ccf.SupportedRuntime);
    count += AddFile(seedAssembly, [seedTag]) ? 1 : 0;
    count += AddFolder(ccf.BasePath, false, [seedTag]);
    foreach(var ppe in ccf.PrivatePathEntries)
    {
      var ppf = Path.Combine(ccf.BasePath, ppe);
      count += AddFolder(ppf, false, [$"{seedTag}/{ppe}"]);
    }
    return count;
  }

  private int SeedModern(string seedAssembly, string configFile, string seedTag)
  {
    // It is quite likely that seedAssembly is not an assembly at all but
    // a shim executable. Verify that case first and use the *.dll instead
    if(seedAssembly.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
      && !AsmReflection.TryGetAssemblyName(seedAssembly, out _))
    {
      var dll = Path.ChangeExtension(seedAssembly, ".dll");
      if(File.Exists(dll))
      {
        seedAssembly = dll;
      }
      else
      {
        throw new InvalidOperationException(
          $"The given file is not an assembly, and the expected related DLL file does not exist: {dll}");
      }
    }
    throw new NotImplementedException(
      $"NYI: SeedModern({seedAssembly}, {configFile})");
  }
}
