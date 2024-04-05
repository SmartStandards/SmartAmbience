using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.SmartSingleton {

  /// <summary>
  /// An [Scoped-]Singleton container, which supports multiple co-exisiting instances
  /// (for multi-tenancy) and/or auto-disposal when idle.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  [DebuggerDisplay("{TypeName}-Singleton's ({ActiveInstances})")]
  public sealed class Singleton<T>{

    private Func<T> _Factory;
    private Func<string> _ScopeDiscriminatorGetter = ()=> "{global}";
    private int _AutoDisposeSeconds = 0;
    private Task _AutoDisposeTask = null;
    private Dictionary<string, DateTime> _LastAccessTimesPerScope = new Dictionary<string, DateTime>();

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private Dictionary<string, T> _InstancesPerScope = new Dictionary<string, T>();

    ////// CONSTRUCTORS //////

    public Singleton(Func<T> factory) {
      _Factory = factory;
    }

    public Singleton(Func<T> factory, Func<string> scopeDiscriminatorGetter) {
      _Factory = factory;
      _ScopeDiscriminatorGetter = scopeDiscriminatorGetter;
    }

    public Singleton(Func<T> factory, int autoDisposeSeconds) {
      _Factory = factory;
      _AutoDisposeSeconds = autoDisposeSeconds;
    }

    public Singleton(Func<T> factory, Func<string> scopeDiscriminatorGetter, int autoDisposeSeconds) {
      _Factory = factory;
      _ScopeDiscriminatorGetter = scopeDiscriminatorGetter;
      _AutoDisposeSeconds = autoDisposeSeconds;
    }

    ////// GETTER / INSTANCE CREATION //////

    public T GetCurrent() {
      string currentScopeDiscriminator = _ScopeDiscriminatorGetter.Invoke();
      T instance;
      lock (_InstancesPerScope) {
        if (!_InstancesPerScope.TryGetValue(currentScopeDiscriminator, out instance)) {
          instance = _Factory.Invoke();
          if (instance == null) {
            throw new Exception($"Factory-Method for Singleton<{this.TypeName}> returned null!");
          }
          _InstancesPerScope[currentScopeDiscriminator] = instance;
          this.StartAutoDisposeIfRequired();
        }
      }
      lock (_LastAccessTimesPerScope) {
        _LastAccessTimesPerScope[currentScopeDiscriminator] = DateTime.Now;
      }
      return instance;
    }

    ////// DISPOSAL //////

    /// <summary>
    /// Disposes the current instance (only for the current scope) and returns true, if it was active!
    /// </summary>
    /// <returns></returns>
    public bool DisposeCurrent() {
      string currentScopeDiscriminator = _ScopeDiscriminatorGetter.Invoke();
      return this.DisposeInstance(currentScopeDiscriminator);
    }

    /// <summary>
    /// Disposes all instances (also for foreign scopes) and returns the count of disposed instances.
    /// </summary>
    public int DisposeAll() {
      int count = _InstancesPerScope.Count;
      foreach (string scopeDiscriminator in this.ActiveScopes) {
        if(this.DisposeInstance(scopeDiscriminator)) {
          count++;
        }
      }
      return count;
    }

    private bool DisposeInstance(string scopeDiscriminator) {

      lock (_LastAccessTimesPerScope) {
        if (_LastAccessTimesPerScope.ContainsKey(scopeDiscriminator)) {
          _LastAccessTimesPerScope.Remove(scopeDiscriminator);
        }
      }

      lock (_InstancesPerScope) {
        if (_InstancesPerScope.ContainsKey(scopeDiscriminator)) {
          T instance = _InstancesPerScope[scopeDiscriminator];
          if (instance != null && instance is IDisposable) {
            ((IDisposable)instance).Dispose();
          }
          _InstancesPerScope.Remove(scopeDiscriminator);
          return true;
        }
      }

      return false;
    }

    private void StartAutoDisposeIfRequired() {

      if (_AutoDisposeSeconds <= 0 || _AutoDisposeTask != null) {
        return;
      }

      _AutoDisposeTask = Task.Run(
        () => {
          var scopesToDispose = new List<string>();
          while (true) {
            lock (_LastAccessTimesPerScope) {
              if (_LastAccessTimesPerScope.Count == 0) {
                _AutoDisposeTask = null;
                return;
              }
              DateTime disposalTime = DateTime.Now.Subtract(new TimeSpan(0, 0, _AutoDisposeSeconds));
              foreach (var entry in _LastAccessTimesPerScope) {
                if (entry.Value < disposalTime) {
                  scopesToDispose.Add(entry.Key);
                }
              }
            }
            foreach (string key in scopesToDispose) {
              this.DisposeInstance(key);
            }
            Thread.Sleep(500);
          }
        }
      );

    }

    ////// INFO-PROPS //////
    
    public string TypeName {
      get {
        return typeof(T).Name;
      }
    }

    public bool HasInstance {
      get {
        string currentScopeDiscriminator = _ScopeDiscriminatorGetter.Invoke();
        lock (_InstancesPerScope) {
          return _InstancesPerScope.TryGetValue(currentScopeDiscriminator, out T dummy);
        }
      }
    }

    public int ActiveInstances {
      get {
        lock (_InstancesPerScope) {
          return _InstancesPerScope.Count;
        }
      }
    }

    public string[] ActiveScopes {
      get {
        lock (_InstancesPerScope) {
          return _InstancesPerScope.Keys.ToArray();
        }
      }
    }

  }

}
