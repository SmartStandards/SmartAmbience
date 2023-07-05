using System.Collections.Concurrent;

namespace System.Threading {

  public class AmbienceToAppdomainAdapter : IAmbienceToSomeContextAdapter {

    private static ConcurrentDictionary<string, string> _RootValuesByKey = new ConcurrentDictionary<string, string>();

    public event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;

    public void SetCurrentValue(string key, string value) {
      _RootValuesByKey[key] = value;
    }

    public string TryGetCurrentValue(string key) {
      string value = null;
      _RootValuesByKey.TryGetValue(key, out value);
      return value;
    }

  }

}
