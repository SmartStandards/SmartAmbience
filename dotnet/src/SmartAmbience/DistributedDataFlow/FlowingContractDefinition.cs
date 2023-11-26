using System;
using System.Collections.Generic;
using System.Text;

namespace DistributedDataFlow {

  public class FlowingContractDefinition {

    /// <summary>
    /// Includes all AmbientFields and all CustomEndpoints
    /// </summary>
    internal static FlowingContractDefinition DefaultDefinition = new FlowingContractDefinition(true);

    internal FlowingContractDefinition(bool allowAll) {
      if (allowAll) {
        _IncludeAllAmbientFields = true;
        _IncludeAnyAmbientFields = true;
        _IncludeAllEndpoints = true;
        _IncludeAnyEndpoints = true;
      }
    }

    //a little bit of redundancy for better performance...
    private bool _IncludeAllAmbientFields = false;
    private bool _IncludeAnyAmbientFields = false;
    private bool _IncludeAllEndpoints = false;
    private bool _IncludeAnyEndpoints = false;

    private List<string> _IncludedAmbientFieldNames = new List<string>();
    private List<string> _ExcludedAmbientFieldNames = new List<string>();
    private List<string> _IncludedCustomEndpointNames = new List<string>();
    private List<string> _ExcludedCustomEndpointNames = new List<string>();

    public void IncludeAllExposedAmbientFieldInstances() {
      this.ImmutableGuard();
      _IncludeAllAmbientFields = true;
      _IncludeAnyAmbientFields = true;
    }

    public void IncludeExposedAmbientFieldInstances(params string[] ambientFieldNames) {
      this.ImmutableGuard();
      _IncludeAnyAmbientFields = true;
      _IncludedAmbientFieldNames.AddRange(ambientFieldNames);
    }

    public void IncludeAllCustomEndpoints() {
      this.ImmutableGuard();
      _IncludeAllEndpoints = true;
      _IncludeAnyEndpoints = true;
    }

    public void IncludeCustomEndpoints(params string[] customEndpointName) {
      this.ImmutableGuard();
      _IncludeAnyEndpoints = true;
      _IncludedCustomEndpointNames.AddRange(customEndpointName);
    }

    public void ExcludeExposedAmbientFieldInstances(params string[] ambientFieldNames) {
      this.ImmutableGuard();
      _ExcludedAmbientFieldNames.AddRange(ambientFieldNames);
    }

    public void ExcludeCustomEndpoints(params string[] customEndpointName) {
      this.ImmutableGuard();
      _ExcludedCustomEndpointNames.AddRange(customEndpointName);
    }

    #region " Immutable "

    private bool _IsImmutable = false;

    internal void MakeImmutable() {
      _IsImmutable = true;
    }
    private void ImmutableGuard() {
      if (_IsImmutable) {
        throw new InvalidOperationException("This Definition is now Immutable! It can only be modified during the setup phase.");
      }
    }

    #endregion

    #region " Evaluation "

    internal bool IsEndpointIncluded(string customEndpointName) {
      if (customEndpointName == AmbienceHub.AmbientFieldEndpointName) {
        return _IncludeAnyAmbientFields;
      }
      if (_IncludeAllEndpoints) {
        return true;
      }
      if (!_IncludeAnyEndpoints) {
        return false;
      }
      if (!_IncludedCustomEndpointNames.Contains(customEndpointName)) {
        return false;
      }
      return !(_ExcludedCustomEndpointNames.Contains(customEndpointName));
    }

    internal bool IsAmbientFieldIncluded(string ambientFieldName) {
      if (!_IncludeAnyAmbientFields) {
        return false;
      }
      if (_IncludeAllAmbientFields) {
        return true;
      }
      if (!_IncludedAmbientFieldNames.Contains(ambientFieldName)) {
        return false;
      }
      return !(_ExcludedAmbientFieldNames.Contains(ambientFieldName));
    }

    #endregion

  }

}
