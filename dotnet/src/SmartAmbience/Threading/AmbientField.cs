using System.Collections.Generic;

namespace System.Threading {

  /// <summary>
  ///    Contains a named string value that is stored AsyncLocal and in an external short living context ("Operation", "Request", ...) as fallback.
  /// </summary>
  public partial class AmbientField {
    private static IAmbienceToSomeContextAdapter _ContextAdapter;

    private static Dictionary<string, AmbientField> _ExposedInstances = new Dictionary<string, AmbientField>();

    private bool _ContextAdapterEventHandlersAdded;

    private string _LongLivingValue;

    private AsyncLocal<string> _InnerAsyncLocal = new AsyncLocal<string>();

    /// <summary>
    ///   Creates a new instance of an AmbientField. 
    ///   Optionally, registers it to the ExposedInstances catalog.
    /// </summary>
    /// <param name="name">
    ///   The name of the field (best practice is to use the same name here and for the field that is holding the AmbientField).
    ///   The name will be used as (part of the) key when storing the value in the external fallback context.
    ///   The name might also be used when exposed (in flowing scenarios) - in this case it should be globally unique.
    /// </param>
    /// <param name="exposedInstance">
    ///   Tags the AmbientField as "exposed". It will be registered in the ExposedInstances catalog.
    ///   The intended purpose is flowing: Exposed AmbientFields can be collected by a flowing engine (to be flowed accross web service hops).
    /// </param>
    public AmbientField(string name, bool exposedInstance = false) {
      Name = name;

      if ((!exposedInstance))
        Key = name + "'" + this.GetHashCode().ToString();
      else {
        Key = name;
        _ExposedInstances.Add(name, this);

        // "Pre-Staging": Der Wert des AmbientField kann zeitlich vor dessen Konstruktor bereits hinterlegt worden sein.
        // (macht z.B. der AmbienceHub beim Restore der Flowed Ambient States).
        // Der Konstruktor wird ggf. vom Owner des AmbientField (z.B. ProfileStateManager) lazy aufgerufen - und somit viel später.

        // In dieser Konstellation, muss der hinterlegte Wert ins InnerAsyncLocal synchronisiert werden:

        if ((_ContextAdapter != null && _ContextAdapter.IsUsable)) {
          string preStagedValue = AmbientField.ContextAdapter.TryGetCurrentValue(Key);

          if ((preStagedValue != null))
            _InnerAsyncLocal.Value = preStagedValue;
        }
      }
    }

    /// <summary>
    ///   Throws an exception if the context adapter currently cannot be used to store/fetch the value.
    /// </summary>
    /// <remarks>
    ///   This can be called before getting/setting values to ensure that the value does not accidentally go into the
    ///   inner long living value store. This is recommended in code that is executed in a request lifetime scope.
    /// </remarks>
    public void AssertLongLivingValueIsNotUsed() {
      this.AssertContextAdapterIsSet();

      if ((!_ContextAdapter.IsUsable))
        throw new Exception($"{nameof(ContextAdapter)} {_ContextAdapter.GetType().Name} is currently not usable!");
    }

    private void AssertContextAdapterIsSet() {
      if ((_ContextAdapter == null))
        throw new Exception($"{nameof(ContextAdapter)} must be initialized before using {Name}!");

      if ((!_ContextAdapterEventHandlersAdded)) {
        _ContextAdapter.CurrentContextIsTerminating += this.ContextAdapter_IsTerminating;
        _ContextAdapterEventHandlersAdded = true;
      }
    }

    public static void InjectPreStagedValue(string name, string value) {
      if ((_ContextAdapter == null))
        throw new Exception($"{nameof(ContextAdapter)} must be initialized before using {nameof(AmbientField)}!");

      if ((!_ContextAdapter.IsUsable))
        throw new InvalidOperationException("Pre-staging failed, because ContextAdapter is currently not usable!");

      _ContextAdapter.SetCurrentValue(name, value);
    }

    public void InvokeUnderTemporaryBranch(string overridingValue, Action actionToInvoke) {
      string rescuedCurrentValue = _InnerAsyncLocal.Value;

      try {
        _InnerAsyncLocal.Value = overridingValue;
        actionToInvoke.Invoke();
      }
      finally {
        _InnerAsyncLocal.Value = rescuedCurrentValue;
      }
    }

    public void SealContextValue() {
      this.AssertLongLivingValueIsNotUsed();

      string contextValue = _ContextAdapter.TryGetCurrentValue(Key);

      if ((contextValue == null))
        throw new InvalidOperationException($"Context value for \"{Key}\" cannot be sealed, because it's null!");

      _ContextAdapter.SetCurrentValue(Key + ".IsSealed", "True");
    }

    /// <summary>
    ///  The adapter for storing the value in an external fallback context depending upon the hosting technology.
    /// </summary>
    /// <remarks>
    ///   this must be initialized before any AmbientField can be used.
    /// </remarks>
    public static IAmbienceToSomeContextAdapter ContextAdapter {
      get {
        return _ContextAdapter;
      }
      set {
        if ((_ContextAdapter == null)) {
          _ContextAdapter = value;
          return;
        }
        if ((value == null)) {
          throw new InvalidOperationException($"{nameof(ContextAdapter)} cannot be set to back null!");
        }
        if ((value.GetType() != _ContextAdapter.GetType())) {
          throw new InvalidOperationException($"{nameof(ContextAdapter)} has already been set to \"{_ContextAdapter.GetType().FullName}\" and cannot be changed to \"{value.GetType().FullName}\"!");
        }
      }
    }

    /// <returns>
    ///  (ContextAdapterIsNull)
    ///  (ContextAdapterIsNotUsable)
    ///  (NotPresent) - if the ....IsSealed key/value is not present in the _ContextAdapter (= SealContextValue() was never called for that current context)
    ///  True - It's sealed.
    /// </returns>
    public string ContextValueIsSealed {
      get {
        if ((_ContextAdapter == null))
          return "(ContextAdapterIsNull)";

        if ((!_ContextAdapter.IsUsable))
          return "(ContextAdapterIsNotUsable)";

        string value = _ContextAdapter.TryGetCurrentValue(Key + ".IsSealed");

        if ((value == null))
          return "(NotPresent)";

        return value;
      }
    }

    public static IDictionary<string, AmbientField> ExposedInstances {
      get {
        return _ExposedInstances;
      }
    }

    /// <summary>
    ///  Normally, this is a key string built from "{Name}'{HashNumber}", which is used as key for the external fallback context.
    ///  The HashNumber avoids key collisions to entries from 3rd party components, that may exist in the external fallback context.
    ///  For "ExposedInstances", the key ist just "{Name}" - otherwise it would be impossible to inject pre staged values 
    ///  (because this would happen before the hash number is even generated).
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///  Must be set via constructor parameter. See documentation there.
    /// </summary>
    public string Name { get; }

    public Action<string> OnTerminatingMethod { get; set; }

    /// <summary>
    ///  Gets or sets the ambient value.
    /// </summary>
    /// <returns>
    ///  Nothing, if the value was never set.
    ///  If you recieve nothing, although you surely set a value, this is a bug due to a not properly working fallback context.
    /// </returns>
    /// <remarks>
    ///  You cannot set null as value, because null is reserved for 'never set' 
    ///  (and also, we cannot assume that the external fallback context is supporting null as value).
    /// </remarks>
    public string Value {
      get {
        this.AssertContextAdapterIsSet();

        _DebugInfo.LongLivingValueDuringGet = _LongLivingValue;

        string contextValue = null;

        if ((_ContextAdapter.IsUsable)) {
          contextValue = _ContextAdapter.TryGetCurrentValue(Key);
          _DebugInfo.ContextWasUsableDuringGet = true;
        }
        else
          _DebugInfo.ContextWasUsableDuringGet = false;

        _DebugInfo.ContextValueDuringGet = contextValue;

        string asyncLocalValue = _InnerAsyncLocal.Value;

        _DebugInfo.AsyncLocalValueDuringGet = asyncLocalValue;

        if ((asyncLocalValue != null))
          return asyncLocalValue;
        else if ((_ContextAdapter.IsUsable)) {

          // Der AsyncLocal-Context ist zwischenzeitlich verloren gegangen, wir müssen ihn wiederherstellen...

          _InnerAsyncLocal.Value = contextValue;

          // ...damit zukünftige Sub-Threads funktionieren können
          // (wir erinnern uns: Bei Sub-Threads versagt oft der Value aus dem ContextAdapter - z.B. HttpContext.Current.Value liefert als Value null.
          // der Context.Current selbst ist dabei nicht null).

          return contextValue;
        }
        else {
          _InnerAsyncLocal.Value = _LongLivingValue;
          return _LongLivingValue;
        }
      }
      set {
        this.AssertContextAdapterIsSet();

        // Null cannot be used because AsnycLocal.Value returns null when it was lost
        // - so we cannot distinguish that accidental null from an intentional null

        if ((value == null))
          throw new ArgumentException($"Null can not be carried as value for \"{Name}\"!");

        if ((this.ContextValueIsSealed == "True")) {

          // Ensure the context value is only not changed.

          string contextValue = _ContextAdapter.TryGetCurrentValue(Key);

          if ((!string.IsNullOrEmpty(contextValue) && !contextValue.Equals(value)))
            throw new InvalidOperationException($"Sealed context value for \"{Key}\" has already been set to \"{contextValue}\" and cannot be changed to \"{value}\"!"
          );
        }

        // Actually store the value

        if ((_ContextAdapter.IsUsable))
          _ContextAdapter.SetCurrentValue(Key, value);
        else
          _LongLivingValue = value;

        _InnerAsyncLocal.Value = value; // Immer setzen
      }
    }

    private void ContextAdapter_IsTerminating() {
      AmbientField.ContextAdapter.CurrentContextIsTerminating -= this.ContextAdapter_IsTerminating;

      if ((OnTerminatingMethod != null)) {
        string dyingValue = AmbientField.ContextAdapter.TryGetCurrentValue(Name);
        OnTerminatingMethod.Invoke(dyingValue);
      }
    }

  }

}
