using Microsoft.AspNetCore.Builder;
using System.Threading;

namespace Microsoft.AspNetCore {

  public static class SmartAmbienceSetupExtensionsForApplicationBuilder {

    public static void UseAmbientFieldAdapterMiddleware(this IApplicationBuilder app) {
      app.UseMiddleware<AmbientFieldAdapterMiddleware>();
      AmbientField.ContextAdapter = AmbientFieldAdapterMiddleware.ContextAdapter;
    }

  }

}
