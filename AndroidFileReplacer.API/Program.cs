var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "AndroidFileReplacer API", Version = "v1" });
});

// 添加CORS支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // 记录API密钥状态
    var apiKey = builder.Configuration["ApiKey"] ?? "YOUR_API_KEY_HERE";
    app.Logger.LogInformation($"API密钥已配置: {!string.IsNullOrEmpty(apiKey)}");
    app.Logger.LogInformation("");
    app.Logger.LogInformation($"服务器启动于: {DateTime.Now}");
}

// 在开发环境中禁用HTTPS重定向
// app.UseHttpsRedirection();
app.UseStaticFiles();

// 启用CORS
app.UseCors();

app.UseAuthorization();
app.MapControllers();

// 将根路径重定向到index.html
app.MapGet("/", context =>
{
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

app.Run(); 