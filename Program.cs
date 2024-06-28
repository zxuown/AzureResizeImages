var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:InstrumentationKey"]);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/index.html");
        return;
    }
    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

app.Run();
