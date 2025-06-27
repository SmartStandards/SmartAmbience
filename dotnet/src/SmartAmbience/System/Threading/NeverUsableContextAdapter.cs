using System.Collections.Concurrent;

namespace System.Threading {

  public class NeverUsableContextAdapter : IAmbienceToSomeContextAdapter {

    public bool IsUsable {
      get {
        return false;
      }
    }

    public event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;

    public void SetCurrentValue(string key, string value) {
      throw new InvalidOperationException("This context adapter is never usable, so setting a value is not supported.");
    }

    public string TryGetCurrentValue(string key) {
      throw new InvalidOperationException("This context adapter is never usable, so getting values is not supported.");
    }

  }

}
