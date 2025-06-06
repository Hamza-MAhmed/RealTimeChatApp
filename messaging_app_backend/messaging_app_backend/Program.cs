﻿using messaging_app_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using messaging_app_backend.Services;
using System.Text.Json;
using messaging_app_backend.Hubs;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.WebHost.UseUrls("http://0.0.0.0:5095");

// Add services to the container.
builder.Services.AddDbContext<ChatAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNative",
        policy =>
        {
            policy.WithOrigins("http://localhost:8081", "http://192.168.1.108:8081", "exp://192.168.1.108:8081")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });

    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

    //builder.Services.AddCors(options =>
    //{
    //    //options.AddDefaultPolicy(policy =>
    //    //{
    //    //    policy
    //    //        .WithOrigins("http://localhost:8081", "http://192.168.1.1:8081")  // Update these URLs as needed
    //    //        .AllowAnyHeader()
    //    //        .AllowAnyMethod();
    //    //});
    //    //services.AddCors(options =>{

    //    options.AddPolicy("AllowReactNative",
    //        policy =>
    //        {
    //            policy.WithOrigins("http://localhost:19006") // Or your actual Metro bundler URL
    //                  .AllowAnyHeader()
    //                  .AllowAnyMethod();
    //        });

    //    options.AddPolicy("AllowAll", builder =>
    //        builder.AllowAnyOrigin()
    //               .AllowAnyMethod()
    //               .AllowAnyHeader());
    //});
    ////});


    builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Trace);
});

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "yourapp.com",
        ValidAudience = "yourapp.com",
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("IAmTheSecretKeyWithExactly32CharsOK!"))
    };
});

// Register ChatListService
builder.Services.AddScoped<IChatListService, ChatListService>();
// Register UserService
builder.Services.AddScoped<IUserService, UserService>();
// SignalR For real time updates
builder.Services.AddSignalR();



// Middleware Configuration - Update this section
var app = builder.Build();

// CORS middleware must be placed before routing and endpoints
//app.UseCors("AllowAll");
app.UseCors("AllowReactNative");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Comment out HTTPS redirection if causing issues
//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//    // Add SignalR hub endpoint
//    app.MapHub<ChatHub>("/chatHub");
//    });




//    , options =>
//{
//    // Temporarily disable auth requirements for testing
//    options.RequireAuthorization = false;
//});

//});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();














//using messaging_app_backend.Data;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.AspNetCore.Builder;

//var builder = WebApplication.CreateBuilder(args);

//// Add DbContext (SQL Server)
//builder.Services.AddDbContext<ChatAppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// CORS Configuration
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", builder =>
//        builder
//        //AllowAnyOrigin()
//               .WithOrigins("http://localhost:8081", "http://192.168.1.102:8081")  // Update these URLs as needed
//               .AllowAnyMethod()
//               .AllowAnyHeader());
//});

//// Add Controllers
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//// builder.Services.AddSwaggerGen();

//// Authentication Configuration
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = "yourapp.com",
//        ValidAudience = "yourapp.com",
//        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("YourSuperSecretKeyHere123!"))
//    };
//});

//var app = builder.Build();

//// Enable CORS
//app.UseCors("AllowAll");

//// Authentication Middleware
//app.UseAuthentication();

//// Configure HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthorization();

//// Map controllers
//app.MapControllers();

//// Start the application and listen on port 7038 for external devices
////app.Run("http://0.0.0.0:5095"); // Listening on all IPs (external access)

//app.Run();

//options.AddDefaultPolicy(policy =>
//{
//    policy
//        .WithOrigins("http://localhost:8081", "http://192.168.1.1:8081")  // Update these URLs as needed
//        .AllowAnyHeader()
//        .AllowAnyMethod();
//});



//// Configure the HTTP request pipeline.

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
