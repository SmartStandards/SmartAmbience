﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DistributedDataFlow {

  /// <summary>
  /// Enabled BACKWARD-DataFlow (for RESPONSEs) an declares,
  /// which FlowingContract should be used when transporting contextual data with the reply of a procedure call.
  /// </summary>
  [AttributeUsage(validOn: AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class HasDataFlowBackChannelAttribute : Attribute {

    /// <summary></summary>
    /// <param name="flowingContractName">
    /// Name of the FlowingContract that should be used when transporting contextual data with the reply of a procedure call.
    /// </param>
    public HasDataFlowBackChannelAttribute(string flowingContractName) {
      _FlowingContractName = flowingContractName;
    }

    private string _FlowingContractName;
    public string FlowingContractName { get { return _FlowingContractName; } }

    public static bool TryReadFrom(Type t, out string flowingContractName) {
      var attrib = t.GetCustomAttributes<HasDataFlowBackChannelAttribute>(true).SingleOrDefault();
      if (attrib == null) {
        flowingContractName = null;
        return false;
      }
      flowingContractName = attrib.FlowingContractName;
      return true;
    }

  }

}
