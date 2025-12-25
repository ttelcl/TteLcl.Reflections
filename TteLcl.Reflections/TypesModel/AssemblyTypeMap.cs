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
/// Stores a list of types indexed by full assembly name
/// </summary>
public class AssemblyTypeMap
{
  private readonly Dictionary<string, List<TypeModel>> _typesByAssembly = new Dictionary<string, List<TypeModel>>();

  /// <summary>
  /// JSON deserialization constructor
  /// </summary>
  /// <param name="typesByAssembly"></param>
  [JsonConstructor]
  public AssemblyTypeMap(
    IDictionary<string, IEnumerable<TypeModel>> typesByAssembly)
  {
    TypesByAssembly = _typesByAssembly;
    foreach(var kvp in typesByAssembly)
    {
      var list = kvp.Value.ToList();
      _typesByAssembly.Add(kvp.Key, list);
    }
  }

  /// <summary>
  /// Create a new empty instance
  /// </summary>
  /// <returns></returns>
  public static AssemblyTypeMap CreateNew()
  {
    return new AssemblyTypeMap(new Dictionary<string, IEnumerable<TypeModel>>());
  }

  /// <summary>
  /// Save the <see cref="TypesByAssembly"/> field to a JSON file (without
  /// this outer wrapper)
  /// </summary>
  /// <param name="fileName">
  /// The file to save to
  /// </param>
  /// <param name="flat">
  /// If false: save <see cref="TypesByAssembly"/> itself, as a JSON object.
  /// If true: save all types in entries in <see cref="TypesByAssembly"/> as
  /// one big JSON array (without the index layer)
  /// </param>
  public void SaveInnerToJson(string fileName, bool flat)
  {
    string json;
    if(flat)
    {
      var list = TypesByAssembly.Values.SelectMany(x => x);
      json = JsonConvert.SerializeObject(list, Formatting.Indented);
    }
    else
    {
      json = JsonConvert.SerializeObject(TypesByAssembly, Formatting.Indented);
    }
    using var writer = File.CreateText(fileName);
    writer.WriteLine(json);
  }

  /// <summary>
  /// Add the types from the given assembly
  /// </summary>
  /// <param name="asm"></param>
  /// <exception cref="InvalidOperationException"></exception>
  public void AddAssembly(Assembly asm)
  {
    var list = asm.GetTypes().Select(t => new TypeModel(t)).ToList();
    _typesByAssembly[asm.FullName ?? throw new InvalidOperationException("assembly is nameless")] = list;
  }

  /// <summary>
  /// A read-only view on the list of types indexed by full assembly name
  /// </summary>
  [JsonProperty("typesByAssembly")]
  public IReadOnlyDictionary<string, List<TypeModel>> TypesByAssembly { get; }
}
