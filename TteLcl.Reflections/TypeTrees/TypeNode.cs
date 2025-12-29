/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using TteLcl.Reflections.TypesModel;

namespace TteLcl.Reflections.TypeTrees;

/// <summary>
/// Wraps a <see cref="Type"/> and related information
/// </summary>
public class TypeNode
{
  /// <summary>
  /// Create a new TypeNode. Called through the indexer on <see cref="TypeNodeMap"/>.
  /// </summary>
  internal TypeNode(Type targetType, TypeNodeMap owner)
  {
    TargetTypes = new HashSet<Type> { targetType };
    TargetType = targetType;
    Owner = owner;
    State = LoadState.Initial;
    Identity = new TypeNodeReference(TargetType, owner);
    Interfaces = [];
    TypeKind = TypeModel.Categorize(TargetType);
    GenericKind = TypeModel.CategorizeGeneric(TargetType);
    GenericArguments = [];
    IsAbstract = TargetType.IsAbstract;
    IsSealed = TargetType.IsSealed;
    ImplementationTypes = [];
  }

  /// <summary>
  /// The primary type this node describes
  /// </summary>
  public Type TargetType { get; }

  /// <summary>
  /// All types having the same key as <see cref="TargetType"/>. It is uncommon
  /// for there to be more than one, but may happen with equivalent open generic types
  /// </summary>
  public HashSet<Type> TargetTypes { get; }

  /// <summary>
  /// The <see cref="TypeNodeMap"/> owning this node
  /// </summary>
  public TypeNodeMap Owner { get; }

  /// <summary>
  /// The <see cref="TypeNodeReference"/> that will be used to uniquely identify this node
  /// </summary>
  public TypeNodeReference Identity { get; }

  /// <summary>
  /// A unique identifier for this node. Normally formed from the name of the type and assembly,
  /// but using a fallback if those are not both available.
  /// </summary>
  public string Key => Identity.Key;

  /// <summary>
  /// A numerical ID for the node
  /// </summary>
  public long Id => Identity.Id;

  /// <summary>
  /// Indicates the progress in the loading process
  /// </summary>
  public LoadState State { get; private set; }

  /// <summary>
  /// Load this node if not already loaded
  /// </summary>
  public void Load()
  {
    // Allow but ignore calls in the wrong phase
    if(State == LoadState.Initial)
    {
      try
      {
        State = LoadState.Loading;
        LoadInternal();
      }
      finally
      {
        State = LoadState.Loaded;
      }
    }
  }

  /// <summary>
  /// Convert this to a serializable model
  /// </summary>
  /// <returns></returns>
  public TypeNodeModel ToModel()
  {
    return new TypeNodeModel(this);
  }

  /// <summary>
  /// The base type
  /// </summary>
  public TypeNode? BaseNode { get; private set; }

  /// <summary>
  /// Interfaces implemented by this type
  /// </summary>
  public List<TypeNode> Interfaces { get; }

  /// <summary>
  /// The typenode for the declaring type, if any
  /// </summary>
  public TypeNode? DeclaringNode { get; private set; }

  /// <summary>
  /// A <see cref="TypeArgumentInfo"/> wrapping the the array (or similar) element type, if any.
  /// </summary>
  public TypeArgumentInfo? ElementType { get; private set; }

  /// <summary>
  /// The kind of the type
  /// </summary>
  public string TypeKind { get; }

  /// <summary>
  /// True if this type is abstract
  /// </summary>
  public bool IsAbstract { get; }

  /// <summary>
  /// True if this type is sealed
  /// </summary>
  public bool IsSealed { get; }

  /// <summary>
  /// Indicates how this type fits in the generic type system - if at all
  /// </summary>
  public string? GenericKind { get; }

  /// <summary>
  /// The type node containing the generic type definition, if any.
  /// This may be a self-link!
  /// </summary>
  public TypeNode? GenericDefinitionNode { get; private set; }

  /// <summary>
  /// The generic type info descriptors (empty for non-generic types)
  /// </summary>
  public List<TypeArgumentInfo> GenericArguments { get; }

  /// <summary>
  /// Types used in properties, fields, method arguments and return types
  /// and events, as selected by <see cref="TypeNodeMap.AnalysisRelations"/>.
  /// </summary>
  public HashSet<TypeNode> ImplementationTypes { get; }

  /// <summary>
  /// The decomposition of this type if it is generic
  /// </summary>
  public TypeTreeNode Tree { get; private set; } = null!;

  private void LoadInternal()
  {
    BaseNode = Owner.TryAddNode(TargetType.BaseType);
    foreach(var intf in TargetType.GetInterfaces())
    {
      Interfaces.Add(Owner.AddNode(intf));
    }
    DeclaringNode = Owner.TryAddNode(TargetType.DeclaringType);
    if(TargetType.IsGenericType)
    {
      var index = 0;
      foreach(var ta in TargetType.GetGenericArguments())
      {
        GenericArguments.Add(new TypeArgumentInfo(Owner, ta, index++));
      }
      var definition = TargetType.GetGenericTypeDefinition();
      GenericDefinitionNode = Owner.TryAddNode(definition);
    }
    ElementType = TypeArgumentInfo.FromType(Owner, TargetType.GetElementType());
    Tree = new TypeTreeNode(Owner, TargetType);
    if(Owner.AnalysisRelations.HasFlag(TypeEdgeKind.Properties))
    {
      var properties = TargetType.GetProperties(
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      foreach(var prop in properties)
      {
        AddLinkedType(prop.PropertyType);
      }
    }
    if(Owner.AnalysisRelations.HasFlag(TypeEdgeKind.Fields))
    {
      var fields = TargetType.GetFields(
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      foreach(var f in fields)
      {
        AddLinkedType(f.FieldType);
      }
    }
    if(Owner.AnalysisRelations.HasFlag(TypeEdgeKind.Methods))
    {
      var methods = TargetType.GetMethods(
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      foreach(var method in methods)
      {
        AddLinkedType(method.ReturnType);
        foreach(var genericArgument in method.GetGenericArguments())
        {
          AddLinkedType(genericArgument);
        }
        foreach(var parameter in method.GetParameters())
        {
          AddLinkedType(parameter.ParameterType);
        }
      }
    }
    if(Owner.AnalysisRelations.HasFlag(TypeEdgeKind.Constructors))
    {
      var constructors = TargetType.GetConstructors(
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      foreach(var constructor in constructors)
      {
        foreach(var parameter in constructor.GetParameters())
        {
          AddLinkedType(parameter.ParameterType);
        }
      }
    }
    if(Owner.AnalysisRelations.HasFlag(TypeEdgeKind.Events))
    {
      var events = TargetType.GetEvents(
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      foreach(var evt in events)
      {
        var type = evt.EventHandlerType;
        if(type != null)
        {
          AddLinkedType(type);
        }
      }
    }
  }

  private void AddLinkedType(Type type)
  {
    // Skip generic parameters and arrays of generic parameters
    if(type.IsGenericParameter)
    {
      return;
    }
    if(type.IsArray || type.IsByRef || type.IsPointer || type.IsByRefLike)
    {
      var elementType = type.GetElementType();
      // Time to read the fine print on GetElementType(): it returns null in several cases,
      // *including the generic type parameter case* in addition the obvious "it is not an array
      // or something else that has an 'element type'"
      if(elementType == null || elementType.IsGenericParameter)
      {
        return;
      }
    }
    var node = Owner.AddNode(type);
    ImplementationTypes.Add(node);
  }

  /// <summary>
  /// Loading state of <see cref="TypeNode"/>s
  /// </summary>
  public enum LoadState
  {
    /// <summary>
    /// Loading has not started yet
    /// </summary>
    Initial,

    /// <summary>
    /// Loading is in progress
    /// </summary>
    Loading,

    /// <summary>
    /// Loading has completed
    /// </summary>
    Loaded,
  }
}
