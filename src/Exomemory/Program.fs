module Exomemory.App

open System
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Microsoft.FSharpLu.Json
// open Microsoft.AspNetCore.Authentication
// open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Exomemory.HttpHandlers

// ---------------------------------
// Web app
// ---------------------------------

// let authenticate =
//     requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let webApp =
    choose
        [ GET
          >=> authenticate
          >=> choose
              [ route "/overview" >=> handleGetOverview
                routef "/inspect/room/%d" handleGetInspectRoom
                subRoute
                    "/search"
                    (choose
                        [ route "/usernames" >=> handleGetSearchUsernames
                          route "/messages" >=> handleGetSearchMessages ])
                subRoute
                    "/lookup"
                    (choose
                        [ route "/user" >=> handleGetLookupUser
                          route "/message" >=> handleGetLookupMessage
                          route "/room" >=> handleGetLookupRoom ]) ]
          setStatusCode 404 (* >=> text "Not Found" *) ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 (* >=> text ex.Message *)

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder
        //  .WithOrigins(
        //      "http://localhost:5000",
        //      "https://localhost:5001")
        // .AllowAnyMethod()
        .WithMethods("GET")
        .AllowAnyHeader()
        .AllowAnyOrigin()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler(errorHandler).UseHttpsRedirection())
        // .UseAuthentication()
        .UseCors(configureCors)
        .UseGiraffe(webApp)

// let authenticationOptions (o: AuthenticationOptions) =
//     o.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
//     o.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

// let jwtBearerOptions (settings: IConfiguration) (cfg: JwtBearerOptions) =
//     cfg.SaveToken <- true
//     cfg.IncludeErrorDetails <- true
//     cfg.Authority <- settings["Jwt:Domain"]
//     cfg.Audience <- settings["Jwt:Audience"]
//     cfg.TokenValidationParameters <- TokenValidationParameters(ValidIssuer = settings["Jwt:ValidIssuer"])

let configureAppConfiguration (context: WebHostBuilderContext) (config: IConfigurationBuilder) =
    config
        .AddJsonFile("appsettings.json", false, true)
        .AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName, true)
        .AddEnvironmentVariables()
    |> ignore

let configureServices (services: IServiceCollection) =
    // let sp = services.BuildServiceProvider()
    // let settings = sp.GetService<IConfiguration>()

    services.AddCors().AddGiraffe()
    // .AddAuthentication(authenticationOptions)
    // .AddJwtBearer(Action<JwtBearerOptions>(settings |> jwtBearerOptions))
    |> ignore

    // to make the JSON serializer happy with Option types -- unwraps them automatically
    let jsonSerializerSettings =
        JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver())

    jsonSerializerSettings.Converters.Add(CompactUnionJsonConverter(true))

    services.AddSingleton<Json.ISerializer>(NewtonsoftJson.Serializer(jsonSerializerSettings))
    |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureAppConfiguration(configureAppConfiguration)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
