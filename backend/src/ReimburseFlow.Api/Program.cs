using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReimburseFlow.Api.ExceptionHandling;
using ReimburseFlow.Application;
using ReimburseFlow.Infrastructure;
using ReimburseFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
	.AddJsonOptions(options =>
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.UseInlineDefinitionsForEnums());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.InvalidModelStateResponseFactory = context =>
	{
		var problemDetails = new ValidationProblemDetails(context.ModelState)
		{
			Status = StatusCodes.Status400BadRequest,
			Title = "Bad Request",
			Instance = context.HttpContext.Request.Path
		};
		problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
		return new BadRequestObjectResult(problemDetails);
	};
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
	?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
	options.AddPolicy("DevelopmentFrontend", policy =>
		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod());
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
	await ApplyMigrationsAsync(app);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("DevelopmentFrontend");

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
	const int maxAttempts = 10;

	for (var attempt = 1; attempt <= maxAttempts; attempt++)
	{
		try
		{
			using var scope = app.Services.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<ReimburseFlowDbContext>();
			await dbContext.Database.MigrateAsync();
			app.Logger.LogInformation("Database migrations applied successfully.");
			return;
		}
		catch (Exception exception) when (attempt < maxAttempts)
		{
			app.Logger.LogWarning(
				exception,
				"Database migration attempt {Attempt} of {MaxAttempts} failed. Retrying...",
				attempt,
				maxAttempts);
			await Task.Delay(TimeSpan.FromSeconds(3));
		}
	}

	using var finalScope = app.Services.CreateScope();
	var finalDbContext = finalScope.ServiceProvider.GetRequiredService<ReimburseFlowDbContext>();
	await finalDbContext.Database.MigrateAsync();
}

public partial class Program;
