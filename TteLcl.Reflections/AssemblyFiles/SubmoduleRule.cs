using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.AssemblyFiles;

/// <summary>
/// A rule for automatically modifying a suggested module name to a submodule name
/// </summary>
public class SubmoduleRule
{
  /// <summary>
  /// Create a new <see cref="SubmoduleRule"/>. Consider using
  /// <see cref="SubmoduleRules.AddRule(string, string, string?)"/> instead.
  /// </summary>
  /// <param name="mainModule">
  /// The name of the module that this rule affects
  /// </param>
  /// <param name="submoduleSuffix">
  /// The suffix appended to <paramref name="mainModule"/> if this rule matches.
  /// If this starts with a letter or digit, a '/' is inserted as separator
  /// (to control the separator, include it explicitly)
  /// </param>
  /// <param name="triggerPrefix">
  /// The prefix of the assembly name that triggers this rule. If null or empty,
  /// this is derived from <paramref name="submoduleSuffix"/>.
  /// </param>
  public SubmoduleRule(
    string mainModule,
    string submoduleSuffix,
    string? triggerPrefix = null)
  {
    var derivedSuffix =
      (submoduleSuffix.StartsWith('.') || submoduleSuffix.StartsWith('/'))
      ? submoduleSuffix[1..]
      : submoduleSuffix;
    if(String.IsNullOrEmpty(submoduleSuffix))
    {
      throw new ArgumentException(
        $"{nameof(submoduleSuffix)} cannot be empty");
    }
    if(Char.IsLetterOrDigit(submoduleSuffix[0]))
    {
      submoduleSuffix = "/" + submoduleSuffix;
    }
    if(String.IsNullOrEmpty(triggerPrefix))
    {
      triggerPrefix = derivedSuffix + ".";
    }
    MainModule = mainModule;
    SubmoduleSuffix = submoduleSuffix;
    TriggerPrefix = triggerPrefix;
  }

  /// <summary>
  /// The main module. This rule only applies if the incoming module name equals this
  /// (case insensitively)
  /// </summary>
  public string MainModule { get; }

  /// <summary>
  /// The prefix for the assembly name. The rule only triggers if the assembly name
  /// starts with this prefix
  /// (case insensitively)
  /// </summary>
  public string TriggerPrefix { get; }

  /// <summary>
  /// The suffix to append to the main module name if the rule matches
  /// </summary>
  public string SubmoduleSuffix { get; }

  /// <summary>
  /// Tests if <paramref name="moduleName"/> matches <see cref="MainModule"/> and
  /// <paramref name="assemblyName"/> starts with <see cref="TriggerPrefix"/>. If true,
  /// this returns the combination of <paramref name="moduleName"/> and <see cref="SubmoduleSuffix"/>;
  /// otherwise <paramref name="moduleName"/> is returned unmodified.
  /// </summary>
  /// <param name="moduleName"></param>
  /// <param name="assemblyName"></param>
  /// <returns></returns>
  public string ApplyIfmatch(string moduleName, string assemblyName)
  {
    if(MainModule.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
      && assemblyName.StartsWith(TriggerPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return moduleName + SubmoduleSuffix;
    }
    return moduleName;
  }

  /// <summary>
  /// Try to apply the rule.
  /// Tests if <paramref name="moduleName"/> matches <see cref="MainModule"/> and
  /// <paramref name="assemblyName"/> starts with <see cref="TriggerPrefix"/>. If true,
  /// this returns the combination of <paramref name="moduleName"/> and <see cref="SubmoduleSuffix"/>
  /// in <paramref name="result"/> and returns true;
  /// otherwise <paramref name="result"/> is set to <paramref name="moduleName"/> and
  /// false is returned.
  /// </summary>
  /// <param name="moduleName"></param>
  /// <param name="assemblyName"></param>
  /// <param name="result"></param>
  /// <returns></returns>
  public bool TryApply(string moduleName, string assemblyName, out string result)
  {
    if(MainModule.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
      && assemblyName.StartsWith(TriggerPrefix, StringComparison.OrdinalIgnoreCase))
    {
      result = moduleName + SubmoduleSuffix;
      return true;
    }
    result = moduleName;
    return false;
  }

}
