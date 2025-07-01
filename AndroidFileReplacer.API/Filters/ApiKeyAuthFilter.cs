using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace AndroidFileReplacer.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 尝试从请求头获取API Key
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key is missing");
            return;
        }

        // 获取配置中的API Key
        var appSettings = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = appSettings.GetValue<string>("ApiKey");

        // 如果未设置API Key，则提示用户
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key is not configured on the server");
            return;
        }
        
        // 验证API Key
        if (!apiKey.Equals(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("Invalid API Key");
            return;
        }

        await next();
    }
} 