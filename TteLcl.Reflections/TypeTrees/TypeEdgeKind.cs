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
/// The kinds of type relations to include
/// </summary>
[Flags]
public enum TypeEdgeKind
{
  /// <summary>
  /// No relations beyond the basics
  /// </summary>
  None = 0,

  /// <summary>
  /// Include types of properties
  /// </summary>
  Properties = 1,

  /// <summary>
  /// Include types of fields
  /// </summary>
  Fields = 2,

  /// <summary>
  /// Include method types: return values, arguments, and generic method parameter types
  /// </summary>
  Methods = 4,

  /// <summary>
  /// Include constructor arguments
  /// </summary>
  Constructors = 8,

  /// <summary>
  /// Include event delegate types
  /// </summary>
  Events = 16,

  /// <summary>
  /// Include all type relations
  /// </summary>
  All = Properties | Fields | Methods | Constructors | Events ,
}
