using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Security.AccessTokenHandling {

  [AttributeUsage(validOn: AttributeTargets.Method)]
  public class ExtractFlowedDataAttribute : Attribute, IAsyncActionFilter {

    private string _HttpHeaderName;
    private bool _IsOptional;
    private string[] _MandatoryKeys;

    public ExtractFlowedDataAttribute(string httpHeaderName, bool isOptional = false, params string[] mandatoryKeys) {
      _HttpHeaderName = httpHeaderName;
      _IsOptional = isOptional;
      _MandatoryKeys = mandatoryKeys;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
      Dictionary<string, string> flowedData = null;

      try {
        string rawFlowedData = null;

        if (context.HttpContext.Request.Headers.TryGetValue(_HttpHeaderName, out var extractedHeader)) {
          rawFlowedData = extractedHeader.ToString();
          if (String.IsNullOrWhiteSpace(rawFlowedData)) {
            rawFlowedData = null;
          }
        }

        if (!string.IsNullOrWhiteSpace(rawFlowedData)) {
          try {
            flowedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(rawFlowedData);
            if(flowedData != null) {
              foreach (string mandatoryKey in _MandatoryKeys) {
                if (!flowedData.ContainsKey(mandatoryKey)) {
                  context.Result = new ContentResult() {
                    StatusCode = 400,
                    Content = $"The flowed data needs to contain '{mandatoryKey}'!"
                  };
                  return;
                }
              }
            }
            else {
              context.Result = new ContentResult() {
                StatusCode = 400,
                Content = $"The '{_HttpHeaderName}'-Header is empty!"
              };
              return;
            }
          }
          catch (Exception ex) {
            context.Result = new ContentResult() {
              StatusCode = 400,
              Content = $"Could not deseriailze the '{_HttpHeaderName}'-Header: " + ex.Message
            };
            return;
          }
        }
        else if (!_IsOptional) {
          context.Result = new ContentResult() {
            StatusCode = 400,
            Content = $"Missing the '{_HttpHeaderName}'-Header, which should contain flowed data!"
          };
          return;
        }
      }
      catch (Exception ex) {
        context.Result = new ContentResult() {
          StatusCode = 500,
          Content = "Error extracting flowed data: " + ex.Message
        };
        return;
      }

      try {
        if (flowedData != null) {
          AmbienceHub.RestoreValuesFrom(flowedData);
        }
        else {
          AmbienceHub.RestoreValuesFrom(new Dictionary<string, string>());
        }
      }
      catch (Exception ex) {
        context.Result = new ContentResult() {
        StatusCode = 500,
          Content = "Error exposing flowed data to the ambience room: " + ex.Message
        };
        return;
      }

      await next();

    }//OnActionExecutionAsync()

  }//Attribute

}//NS
