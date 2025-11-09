/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections;

/// <summary>
/// Caches information about a potential assembly file
/// </summary>
public sealed class AssemblyFileInfo: IEquatable<AssemblyFileInfo>
{
  private AssemblyName? _assemblyName;
  private bool _checked;

  /// <summary>
  /// Create a new <see cref="AssemblyFileInfo"/>. Initially this merely stores the name
  /// of the file (as a full path). Upon calling <see cref="GetIsAssembly"/> or
  /// <see cref="GetAssemblyName"/> for the first time the file is inspected if it
  /// is an assembly file.
  /// </summary>
  public AssemblyFileInfo(
    string fileName)
  {
    FileName = Path.GetFullPath(fileName);
  }

  /// <summary>
  /// The full name of the file
  /// </summary>
  public string FileName { get; }

  /// <summary>
  /// True if this file is known to be an assembly. False if not an assembly,
  /// null if 'assemblyness' was never checked yet. Use <see cref="GetIsAssembly"/>
  /// or <see cref="GetAssemblyName"/> to "test assemblyness".
  /// </summary>
  public bool? IsAssembly => _checked ? _assemblyName != null : null;

  /// <summary>
  /// True if this file is known to be an assembly. If not done so already,
  /// this method performs the (somewhat expensive) test to check that, and
  /// caches the result.
  /// </summary>
  /// <returns></returns>
  public bool GetIsAssembly()
  {
    TestAssembly();
    return _assemblyName != null;
  }

  /// <summary>
  /// Retrieve the assembly name of the target file. Cached upon first call.
  /// Returns null if the file is not an assembly file (including the case that
  /// it does not exist at all)
  /// </summary>
  /// <returns></returns>
  public AssemblyName? GetAssemblyName()
  {
    TestAssembly(); // only actually does something on first call
    return _assemblyName;
  }

  private void TestAssembly()
  {
    if(!_checked)
    {
      _checked = true;
      AsmReflection.TryGetAssemblyName(FileName, out _assemblyName);
    }
  }

  /// <summary>
  /// Check if this and <paramref name="other"/> are equivalent. Two 
  /// <see cref="AssemblyFileInfo"/> objects are equivalent if their
  /// <see cref="FileName"/> properties are case insensitively equal.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public bool Equals(AssemblyFileInfo? other)
  {
    if(other == null)
    {
      return false;
    }
    return StringComparer.OrdinalIgnoreCase.Equals(FileName, other.FileName);
  }

  /// <summary>
  /// Check if <paramref name="obj"/> is a <see cref="AssemblyFileInfo"/> and equivalent to this.
  /// Two <see cref="AssemblyFileInfo"/> objects are equivalent if their
  /// <see cref="FileName"/> properties are case insensitively equal.
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public override bool Equals(object? obj)
  {
    if(obj is AssemblyFileInfo afi)
    {
      return StringComparer.OrdinalIgnoreCase.Equals(FileName, afi.FileName);
    }
    return false;
  }

  /// <summary>
  /// Get the hash code for this object, consistent with the equality definition
  /// </summary>
  /// <returns></returns>
  public override int GetHashCode()
  {
    return StringComparer.Ordinal.GetHashCode(FileName);
  }
}
