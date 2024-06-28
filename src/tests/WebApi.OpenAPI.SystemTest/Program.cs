using WebApi.OpenAPI.SystemTest;
using WebApi.OpenAPI.SystemTest.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwagger();

// Setup specific to Client Credentials Grant
builder.Services.AddAuthentication().AddClientCredentials();
builder.Services.AddClientCredentialsAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapMinimalEndpoint();
app.MapControllers();

app.Run();
