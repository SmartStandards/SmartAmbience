using System.Collections.Generic;

namespace System.Threading {

  /// <summary>
  ///    Contains a named string value that is stored AsyncLocal and in an external short living context ("Operation", "Request", ...) as fallback.
  /// </summary>
  public partial class AmbientField {

    private static IAmbienceToSomeContextAdapter _ContextAdapter;

    private bool _ContextAdapterEventHandlersAdded;

    private AmbientFieldDebugInfo _DebugInfo = new AmbientFieldDebugInfo();

    private static Dictionary<string, AmbientField> _ExposedInstances = new Dictionary<string, AmbientField>();

    private AsyncLocal<string> _InnerAsyncLocal = new AsyncLocal<string>();

    private bool _RootValueIsWriteOnce;

    private AmbientField() {
    }

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

      this.Name = name;

      if (!exposedInstance) {
        this.Key = name + "'" + this.GetHashCode().ToString();
      }

      else {
        this.Key = name;
        _ExposedInstances.Add(name, this);

        // "Pre-Staging": Der Wert des AmbientField kann zeitlich vor dessen Konstruktor bereits hinterlegt worden sein.
        // (macht z.B. der AmbienceHub beim Restore der Flowed Ambient States).
        // Der Konstruktor wird ggf. vom Owner des AmbientField (z.B. ProfileStateManager) lazy aufgerufen - und somit viel später.

        // In dieser Konstellation, muss der hinterlegte Wert ins InnerAsyncLocal synchronisiert werden:

        if (ContextAdapter != null) {

          string preStagedValue = ContextAdapter.TryGetCurrentValue(this.Key);

          if (preStagedValue != null) {
            _InnerAsyncLocal.Value = preStagedValue;
          }

        }

      }

    }

    private void AssertContextAdapter() {

      if (_ContextAdapter is null) {
        throw new Exception($"{nameof(ContextAdapter)} must be initialized before using {this.Name}!");
      }

      if (!_ContextAdapterEventHandlersAdded) {
        global::System.Threading.AmbientField._ContextAdapter.CurrentContextIsTerminating += this.ContextAdapter_IsTerminating;
        _ContextAdapterEventHandlersAdded = true;
      }

    }

    public static void InjectPreStagedValue(string name, string value) {

      if (_ContextAdapter is null) {
        throw new Exception($"{nameof(ContextAdapter)} must be initialized before using {nameof(AmbientField)}!");
      }

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

    /// <summary>
    ///   The adapter for storing the root item in an external fallback context depending upon the hosting technology.
    /// </summary>
    /// <remarks>
    ///   This must be initialized before any AmbientField can be used.
    /// </remarks>
    public static IAmbienceToSomeContextAdapter ContextAdapter {
      get {
        return _ContextAdapter;
      }
      set {
        if (_ContextAdapter != null) {
          throw new InvalidOperationException($"{nameof(ContextAdapter)} has already been set to \"{_ContextAdapter.GetType().Name}\" and cannot be changed to \"{value.GetType().Name}\"!");
        }
        _ContextAdapter = value;
      }
    }

    /// <summary>
    ///   Some states that have been rescued during get- and set-operations for later diagnostics.
    /// </summary>
    /// <remarks>
    ///   This is only for diagnostics.
    /// </remarks>
    public AmbientFieldDebugInfo DebugInfo {
      get {
        return _DebugInfo;
      }
    }

    public static IDictionary<string, AmbientField> ExposedInstances {
      get {
        return _ExposedInstances;
      }
    }

    /// <summary>
    ///    Protects the root value from accidental changes; subsequent changes would throw an exception.
    ///    Useful, when the root value is kind of a binding identifier for e.g. a lifetime scope container (like request, session, etc.)
    /// </summary>
    public bool RootValueIsWriteOnce {
      get {
        return _RootValueIsWriteOnce;
      }
      set {
        _RootValueIsWriteOnce = value;
      }
    }

    /// <summary>
    ///   Normally, this is a key string built from "{Name}'{HashNumber}", which is used as key for the external fallback context.
    ///   The HashNumber avoids key collisions to entries from 3rd party components, that may exist in the external fallback context.
    ///   For "ExposedInstances", the key ist just "{Name}" - otherwise it would be impossible to inject pre staged values 
    ///   (because this would happen before the hash number is even generated).
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    ///   Must be set via constructor parameter. See documentation there.
    /// </summary>
    public string Name { get; private set; }

    public Action<string> OnTerminatingMethod { get; set; }

    /// <summary>
    ///   Gets or sets the ambient value.
    /// </summary>
    /// <returns>
    ///   Nothing, if the value was never set.
    ///   If you recieve nothing, although you surely set a value, this is a bug due to a not properly working fallback context.
    /// </returns>
    /// <remarks>
    ///   You cannot set null as value, because null is reserved for 'never set' 
    ///   (and also, we cannot assume that the external fallback context is supporting null as value).
    /// </remarks>
    public string Value {
      get {

        this.AssertContextAdapter();

        string rootValue = ContextAdapter.TryGetCurrentValue(this.Key);
        string asyncLocalValue = _InnerAsyncLocal.Value;

        _DebugInfo.RootValueDuringGet = rootValue;
        _DebugInfo.AsyncLocalValueDuringGet = asyncLocalValue;

        if (asyncLocalValue != null) {
          return asyncLocalValue;
        }

        else {

          // Der AsyncLocal-Context ist zwischenzeitlich verloren gegangen, wir müssen ihn wiederherstellen...

          _InnerAsyncLocal.Value = rootValue;

          // ...damit zukünftige Sub-Threads funktionieren können
          // (wir erinnern uns: Bei Sub-Threads versagt oft der Value aus dem ContextAdapter - z.B. HttpContext.Current.Value liefert als Value null.
          // der Context.Current selbst ist dabei nicht null).

          return rootValue;
        }

      }
      set {

        this.AssertContextAdapter();

        // Null cannot be used because AsnycLocal.Value returns null when it was lost
        // - so we cannot distinguish that accidental null from an intentional null

        if (value is null) {
          throw new ArgumentException($"Null can not be carried as value for \"{this.Name}\"!");
        }

        if (_RootValueIsWriteOnce) {

          // Ensure the root (= fallback) value is only set once.
          // Subsequent value changes have to be treated as "overriding sessions".

          string currentRootValue = _ContextAdapter.TryGetCurrentValue(this.Key);

          if (!string.IsNullOrEmpty(currentRootValue) && !currentRootValue.Equals(value)) {
            throw new InvalidOperationException($"Root value of \"{this.Name}\" has already been set to \"{currentRootValue}\" and cannot be changed to \"{value}\"!");
          }

        }

        // Actually store the value

        _ContextAdapter.SetCurrentValue(this.Key, value);

        _InnerAsyncLocal.Value = value; // Immer setzen
      }
    }

    private void ContextAdapter_IsTerminating() {

      global::System.Threading.AmbientField.ContextAdapter.CurrentContextIsTerminating -= this.ContextAdapter_IsTerminating;

      if (this.OnTerminatingMethod != null) {
        string dyingValue = ContextAdapter.TryGetCurrentValue(this.Name);
        this.OnTerminatingMethod.Invoke(dyingValue);
      }

    }

    public partial class AmbientFieldDebugInfo {

      /// <remarks> ORDER DEPENDENCY! Fetch this value AFTER get_Value()! </remarks>
      public string RootValueDuringGet { get; set; }

      /// <summary>
      ///   The value that was present during the Get()-operation.
      ///   If is was null, it indicated that the AsyncLocal context died somewhen before and the fallback context was used to fetch the value.
      /// </summary>
      /// <remarks> ORDER DEPENDENCY! Fetch this value AFTER get_Value()! </remarks>
      public string AsyncLocalValueDuringGet { get; set; }

    }

  }

}
