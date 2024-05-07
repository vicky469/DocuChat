using Carter;
using DocumentAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConfig(builder.Configuration)
    .AddDependencyGroup();

var app = builder.Build();

app.RegisterMiddlewares();

app.MapCarter();

app.Run();
