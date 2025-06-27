using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore {

  public class AmbientFieldAdapterMiddleware {

    private class AmbientFieldToHttpContextAdapter : IAmbienceToSomeContextAdapter {

      public event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;

      public bool IsUsable {
        get {
          HttpContext context = this.CurrentHttpContext;
          try {

            //context.Features could throw ObjectDisposedException:
            //https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http/src/DefaultHttpContext.cs#L139
            //but there is no way to check this first :-(
            return (context != null && context.Features != null);

          }
          catch (ObjectDisposedException ex) {
            this.CurrentHttpContext = null;
            return false;
          }
        }
      }

      public void SetCurrentValue(string key, string value) {
        HttpContext context = this.CurrentHttpContext;
        if (context == null) {
          return;
        }
        try {
          context.Items[key] = value;
        }
        catch (ObjectDisposedException ex) {
          this.CurrentHttpContext = null;
          return;
        }
      }

      public string TryGetCurrentValue(string key) {
        HttpContext context = this.CurrentHttpContext;
        if (context == null) {
          return null;
        }
        try {
          context.Items.TryGetValue(key, out var rawValue);
          string value = rawValue?.ToString();
          return value;
        }
        catch (ObjectDisposedException ex) {
          this.CurrentHttpContext = null;
          return null;
        }
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

    private readonly RequestDelegate _OnPassOnInnerRequestExecutor;

    public AmbientFieldAdapterMiddleware(RequestDelegate next) {
      _OnPassOnInnerRequestExecutor = next;
    }

    public async Task Invoke(HttpContext context) {
      try {
        _ContextAdapter.CurrentHttpContext = context;

        await _OnPassOnInnerRequestExecutor.Invoke(context);

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
