using UserService.Handler.User;
using UserService.Middleware;
using UserService.Repository.User;
using UserBusinessService = UserService.Service.User.UserService;

using System.Text.Json;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Production
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var userRepository = new UserRepository();
var userService = new UserBusinessService(userRepository);
var userHandler = new UserHandler(userService);

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<RecoveryMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<TimingMiddleware>();

app.MapPost("/api/users", userHandler.CreateUser);
app.MapGet("/api/users/{id}", userHandler.GetUserById);
app.MapGet("/api/users", userHandler.GetUsers);

app.Urls.Clear();
app.Urls.Add("http://127.0.0.1:8080");

Console.WriteLine("Starting server on http://127.0.0.1:8080");
Console.WriteLine("Available endpoints:");
Console.WriteLine("  POST /api/users - Create new user");
Console.WriteLine("  GET  /api/users - Get all users");
Console.WriteLine("  GET  /api/users/{id} - Get user by ID");

app.Run();
