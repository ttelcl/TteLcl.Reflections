using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections;

/// <summary>
/// Assembly bitness mode
/// </summary>
public enum BitMode
{
  /// <summary>
  /// Bit mode neutral or undecided
  /// </summary>
  AnyCpu = 0,

  /// <summary>
  /// Expect 32 bit assemblies, reject 64 bit assemblies
  /// </summary>
  X86 = 1,

  /// <summary>
  /// Expect 64 bit assemblies, reject 32 bit assemblies
  /// </summary>
  X64 = 2,
}
