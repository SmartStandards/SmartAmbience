using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore {

  public class AmbientFieldAdapterMiddleware {

    private class AmbientFieldToHttpContextAdapter : IAmbienceToSomeContextAdapter {
      public event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;

      public void SetCurrentValue(string key, string value) {
        var ctx = this.CurrentHttpContext;
        if (ctx == null) {
          return;
        }
        ctx.Items[key] = value;
      }

      public string TryGetCurrentValue(string key) {
        var ctx = this.CurrentHttpContext;
        if (ctx == null) {
          return null;
        }
        ctx.Items.TryGetValue(key, out var rawValue);
        string value = rawValue?.ToString();
        return value;
      }

      public void RaiseTerminate() {
        if (CurrentContextIsTerminating != null) {
          CurrentContextIsTerminating.Invoke();
        }
      }

      AsyncLocal<HttpContext> _CurrentHttpContext = new AsyncLocal<HttpContext>();
      public HttpContext CurrentHttpContext {
        get {
          return _CurrentHttpContext.Value;
        }
        set {
          _CurrentHttpContext.Value = value;
        }
      }

    }

    private static AmbientFieldToHttpContextAdapter _ContextAdapter = new AmbientFieldToHttpContextAdapter();
    public static IAmbienceToSomeContextAdapter ContextAdapter {
      get {
        return _ContextAdapter;
      }
    }

    private readonly RequestDelegate _next;

    public AmbientFieldAdapterMiddleware(RequestDelegate next) {
      _next = next;
    }

    public async Task Invoke(HttpContext context) {
      try {
        _ContextAdapter.CurrentHttpContext = context;

        await _next.Invoke(context);

      }
      finally {
        if (context != null) {
          _ContextAdapter.RaiseTerminate();
        }
        _ContextAdapter.CurrentHttpContext = null;
      }
    }
  }

}
