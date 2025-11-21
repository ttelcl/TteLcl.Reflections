using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TteLcl.Reflections.TypesModel;

/// <summary>
/// A list of the types in an assembly
/// </summary>
public class AssemblyTypeList
{
  private readonly List<TypeModel> _types;

  /// <summary>
  /// Constructor (also for deserialization)
  /// </summary>
  /// <param name="assembly"></param>
  /// <param name="types"></param>
  /// <param name="filename"></param>
  [JsonConstructor]
  public AssemblyTypeList(
    string assembly,
    IEnumerable<TypeModel> types,
    string? filename = null)
  {
    _types = new List<TypeModel>(
      from t in types
      orderby t.VisibilityOrder, t.Kind, (t.Generic ?? ""), (t.Label ?? t.Name)
      select t);
    Types = _types;
    AssemblyName = assembly;
    if(String.IsNullOrEmpty(filename))
    {
      FileName = null;
    }
    else
    {
      FileName = Path.GetFileName(filename);
    }
  }

  /// <summary>
  /// Create a new <see cref="AssemblyTypeList"/> from an assembly, including all
  /// types
  /// </summary>
  /// <param name="asm"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  public static AssemblyTypeList FromAssembly(Assembly asm)
  {
    var name = asm.FullName ?? throw new ArgumentException("Nameless assembly");
    var filename = asm.Location;
    var models = asm.GetTypes().Select(t => new TypeModel(t));
    return new AssemblyTypeList(name, models, filename);
  }

  /// <summary>
  /// The full assembly name
  /// </summary>
  [JsonProperty("assembly")]
  public string AssemblyName { get; }

  /// <summary>
  /// The file name, if known
  /// </summary>
  [JsonProperty("filename")]
  public string? FileName { get; }

  /// <summary>
  /// The list of types
  /// </summary>
  [JsonProperty("types")]
  public IReadOnlyList<TypeModel> Types { get; }

}
