using Microsoft.AspNetCore.Server.Kestrel.Core;
using FeatherLite;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// Cache CSS at startup
var cssPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "style.css");
var cachedCss = File.Exists(cssPath) ? await File.ReadAllTextAsync(cssPath) : null;

// Own the request-response loop completely
app.Run(async context =>
{
    var pathValue = context.Request.Path.Value ?? "/";
    var pathSpan = pathValue.AsSpan();
    var method = context.Request.Method;

    if (method == "GET")
    {
        if (pathSpan.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            await HandleHomePage(context);
        }
        else if (pathSpan.Equals("/css/style.css", StringComparison.OrdinalIgnoreCase))
        {
            await HandleCss(context);
        }
        else
        {
            await Handle404(context);
        }
    }
    else
    {
        await Handle404(context);
    }
});

async Task HandleHomePage(HttpContext context)
{
    var html = GenerateHomePage();

    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.StatusCode = 200;

    await context.Response.WriteAsync(html);
}

async Task HandleCss(HttpContext context)
{
    // Hand rolled CSS serving
    if (cachedCss == null)
    {
        context.Response.StatusCode = 404;
        return;
    }

    context.Response.ContentType = "text/css; charset=utf-8";
    context.Response.StatusCode = 200;

    await context.Response.WriteAsync(cachedCss);
}

async Task Handle404(HttpContext context)
{
    context.Response.StatusCode = 404;
    context.Response.ContentType = "text/html; charset=utf-8";

    await context.Response.WriteAsync(Generate404Page());
}

string GenerateHomePage()
{
    // Server side HTML generation using custom template renderer
    // This avoids the overhead of a full framework like Razor
    var variables = new Dictionary<string, string>
    {
        ["SERVER_TIME"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
    };

    return TemplateRenderer.RenderTemplate("home", variables);
}

string Generate404Page()
{
    return TemplateRenderer.RenderTemplate("404");
}

app.Run();
