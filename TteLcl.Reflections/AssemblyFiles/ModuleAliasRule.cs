using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.AssemblyFiles;

/// <summary>
/// A rule for replacing module tags with an alternative name.
/// Used to shorten module tags.
/// <see cref="ModuleAliasRule"/>s apply after <see cref="SubmoduleRule"/>s.
/// </summary>
public class ModuleAliasRule
{
  /// <summary>
  /// Create a new <see cref="ModuleAliasRule"/>.
  /// </summary>
  /// <param name="original"></param>
  /// <param name="alias"></param>
  public ModuleAliasRule(string original, string alias)
  {
    Original = original;
    Alias = alias;
  }

  /// <summary>
  /// The original module tag to be replaced
  /// </summary>
  public string Original { get; }

  /// <summary>
  /// The new module tag
  /// </summary>
  public string Alias { get; }
}
