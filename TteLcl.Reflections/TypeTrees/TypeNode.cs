/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    TargetType = targetType;
    Owner = owner;
    State = LoadState.Initial;
  }

  /// <summary>
  /// The type this node describes
  /// </summary>
  public Type TargetType { get; }

  /// <summary>
  /// The <see cref="TypeNodeMap"/> owning this node
  /// </summary>
  public TypeNodeMap Owner { get; }

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


  
  private void LoadInternal()
  {
    BaseNode = Owner.TryAddNode(TargetType.BaseType);

    // more to be added
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
