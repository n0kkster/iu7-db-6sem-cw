var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS to allow Blazor app to use API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorHttpsOrigin",
        policy =>
        {
            policy.WithOrigins("https://localhost:7249")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorHttpsOrigin");

app.MapGet("/", () => "Main page");
app.MapGet("/aboba", () => "aboba!");

app.Run();
