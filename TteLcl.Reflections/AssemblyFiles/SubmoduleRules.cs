using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TteLcl.Reflections.AssemblyFiles;

/// <summary>
/// A collection of submodule auto-naming rules
/// </summary>
public class SubmoduleRules
{
  private readonly Dictionary<string, List<SubmoduleRule>> _ruleMap;

  /// <summary>
  /// Create the collection of submodule rules
  /// </summary>
  public SubmoduleRules()
  {
    _ruleMap = new Dictionary<string, List<SubmoduleRule>>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Add a <see cref="SubmoduleRule"/> and sort the rules to ensure no rule gets shadowed
  /// </summary>
  /// <param name="rule"></param>
  public void AddRule(SubmoduleRule rule)
  {
    AddRule(rule, false);
  }

  /// <summary>
  /// Create and add a <see cref="SubmoduleRule"/>
  /// </summary>
  /// <param name="mainModule"></param>
  /// <param name="submoduleSuffix"></param>
  /// <param name="triggerPrefix"></param>
  /// <returns></returns>
  public SubmoduleRule AddRule(
    string mainModule,
    string submoduleSuffix,
    string? triggerPrefix = null)
  {
    var rule = new SubmoduleRule(mainModule, submoduleSuffix, triggerPrefix);
    AddRule(rule, false);
    return rule;
  }

  /// <summary>
  /// Tries to apply all rules associated with <paramref name="moduleName"/> until
  /// one matches, or returns <paramref name="moduleName"/> unmodified otherwise.
  /// </summary>
  /// <param name="moduleName"></param>
  /// <param name="assemblyName"></param>
  /// <returns></returns>
  public string ApplyIfmatch(string moduleName, string assemblyName)
  {
    if(_ruleMap.TryGetValue(moduleName, out var ruleList))
    {
      foreach(var rule in ruleList)
      {
        if(rule.TryApply(moduleName, assemblyName, out var result))
        {
          return result;
        }
      }
    }
    return moduleName;
  }

  /// <summary>
  /// Add a <see cref="SubmoduleRule"/> and optionally sort them to ensure no rule gets shadowed
  /// </summary>
  /// <param name="rule">
  /// The rule to add
  /// </param>
  /// <param name="skipSort">
  /// If true sorting is skipped. This is useful to speed up multiple rules
  /// </param>
  private void AddRule(SubmoduleRule rule, bool skipSort)
  {
    if(!_ruleMap.TryGetValue(rule.MainModule, out var ruleList))
    {
      ruleList = new List<SubmoduleRule>();
      _ruleMap.Add(rule.MainModule, ruleList);
    }
    ruleList.Add(rule);
    if(!skipSort)
    {
      // Sort descending by prefix, so that shorter prefixes appear later
      ruleList.Sort((s1, s2) => -StringComparer.OrdinalIgnoreCase.Compare(s1.TriggerPrefix, s2.TriggerPrefix));
    }
  }

}
