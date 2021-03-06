module JwtApp.App

open System
open System.IO
open App.Helpers.MSALClient
open Giraffe
open Giraffe.Serialization
open App.Common.Converters
open JsonApiSerializer
open JsonApiSerializer.ContractResolvers
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open App.Common.Authentication
open App.Handlers.ApplicationHandler
open App.Common.Exceptions
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open App.Handlers.UserHandler


let mutable Configurations: IConfigurationRoot = null

let allGetRoutes: HttpHandler list =
    [ route "/" >=> text "Public endpoint."]
    @ applicationsGetRoutes
    @ usersGetRoutes

let allPostRoutes: HttpHandler list = applicationPostRoutes @ usersPostRoutes
let allDeleteRoutes: HttpHandler list = applicationDeleteRoutes

let webApp =
     choose [
        GET >=> choose allGetRoutes
        POST >=> choose allPostRoutes
        DELETE >=> choose allDeleteRoutes
        setStatusCode 404 >=> text "Not Found" ]

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> handleErrorJsonAPI ex

let configureApp (app : IApplicationBuilder) =
    app.UseAuthentication()
       .UseGiraffeErrorHandler(errorHandler)
       .UseStaticFiles()
       .UseGiraffe webApp

let authenticationOptions (o : AuthenticationOptions) =
    o.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    o.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

let jwtBearerOptions (cfg : JwtBearerOptions) =
    cfg.SaveToken <- true
    cfg.IncludeErrorDetails <- true
    cfg.Authority <- Configurations.["AzureAd:Authority"]
    cfg.Audience <- Configurations.["AzureAd:Audience"]
    cfg.TokenValidationParameters <- (getTokenValidationParameters Configurations).Result

let configureServices (services : IServiceCollection) =
    services
        .AddGiraffe()
        .AddAuthentication(authenticationOptions)
        .AddJwtBearer(Action<JwtBearerOptions> jwtBearerOptions) |> ignore
        
    let settings = JsonApiSerializerSettings()
    settings.Converters.Add(IdiomaticDuConverter())
        
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(settings)) |> ignore
    services.AddSingleton<MSALAccessTokenHolder>({ AccessToken = None }) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore
  
let configureAppSettings (context: WebHostBuilderContext) (config: IConfigurationBuilder) =
    let configuration = 
        config
          .AddJsonFile("appsettings.json",false,true)
          .AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName ,true)
          .AddEnvironmentVariables()
          .Build()

    Configurations <- configuration;          

    configuration |> ignore 
     
[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseKestrel()
                    .UseContentRoot(contentRoot)
                    .ConfigureAppConfiguration(configureAppSettings)
                    .UseWebRoot(webRoot)
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0