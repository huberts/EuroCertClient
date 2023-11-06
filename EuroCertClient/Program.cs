using EuroCertClient.Application.EuroCertSigner.Sign;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddTransient<SignRequestHandler>();

builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
  options.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.Run();
