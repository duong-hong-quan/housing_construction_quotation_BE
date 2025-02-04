using Hangfire;
using HCQS.BackEnd.API.Attributes;
using HCQS.BackEnd.API.Installers;
using HCQS.BackEnd.Service.Implementations;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(p => p.AddPolicy(MyAllowSpecificOrigins, builder =>
{
    builder.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "https://hcqs-backend.azurewebsites.net/", "https://love-house.vercel.app")
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials(); // Add this line to allow credentials

    // Other configurations...
}));
builder.Services.InstallerServicesInAssembly(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger(op=> op.SerializeAsV2= true);
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new DashboardNoAuthorizationFilter() }
}); ;

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var workerService = serviceProvider.GetRequiredService<WorkerService>();
    workerService.Start();
}

app.Run();

//void ApplyMigration()
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var _db = scope.ServiceProvider.GetRequiredService<HCQSDbContext>();
//        if (_db.Database.GetPendingMigrations().Count() > 0)
//        {
//            _db.Database.Migrate();
//        }
//    }
//}