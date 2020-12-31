module JwtApp.App

open System
open System.IO
open App.Common.Converters
open App.Helpers.MSALClient
open Crypto
open Crypto.Authentication
open Dapper.FSharp
open Giraffe
open Giraffe.Serialization
open JsonApiSerializer
open AutoMapper
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open App.Common.Authentication
open App.Handlers.ApplicationHandler
open App.Common.Exceptions
open App.Handlers.UserHandler
open App.Mapping.Automapper.MapperConfig
open App.Handlers.FIHandler
open App.Handlers.ValidationHandler


let mutable Configurations: IConfiguration = null

let allGetRoutes: HttpHandler list =
    [ route "/" >=> text "Public endpoint."]
    @ applicationsGetRoutes
    @ usersGetRoutes
    @ fiGetRoutes
    @ validationGetRoutes

let allPostRoutes: HttpHandler list =
    applicationPostRoutes
      @ usersPostRoutes
      @ fiPostRoutes
      @ validationPostRoutes
let allDeleteRoutes: HttpHandler list = applicationDeleteRoutes
let allPatchRoutes: HttpHandler list = fiPatchRoutes

let webApp =
    subRouteCi "/api"
        <| choose [
            GET >=> choose allGetRoutes
            POST >=> choose allPostRoutes
            DELETE >=> choose allDeleteRoutes
            PATCH >=> choose allPatchRoutes
            setStatusCode 404 >=> text "Not Found" ]

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> handleErrorJsonAPI ex

let configureApp (app : IApplicationBuilder) =
    app.UseAuthentication()
       .UseGiraffeErrorHandler(errorHandler)
       .UseStaticFiles()
       .UseGiraffe webApp
       
    app.UsePathBase(PathString "/api/") |> ignore

let authenticationOptions (o : AuthenticationOptions) =
    o.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    o.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
    o.DefaultSignOutScheme <- OpenIdConnectDefaults.AuthenticationScheme

let jwtBearerOptions (cfg : JwtBearerOptions) =
    cfg.SaveToken <- true
    cfg.IncludeErrorDetails <- true
    cfg.Authority <- Configurations.["AzureAd:Authority"]
    cfg.Audience <- Configurations.["AzureAd:Audience"]
    cfg.TokenValidationParameters <- (getTokenValidationParameters Configurations).Result

let configureInviteOptions = fun (options: AzureAdB2COptions) ->
    Configurations.Bind("Authentication:AzureAdB2C", options)
let configureServices (services : IServiceCollection) =
    services
        .AddGiraffe()
        .AddAuthentication(authenticationOptions)
        .AddCookie()
        .AddAzureAdB2C(Action<AzureAdB2COptions> configureInviteOptions)
        .AddJwtBearer(Action<JwtBearerOptions> jwtBearerOptions) |> ignore
        
    let settings = JsonApiSerializerSettings()
    settings.Converters.Add(IdiomaticDuConverter())
    // settings.Converters.Add(OptionConverter())
        
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(settings)) |> ignore
    services.AddSingleton<ICryptoService>(CLCSCrypto()) |> ignore
    services.AddSingleton<MSALAccessTokenHolder>({ AccessToken = None }) |> ignore
    OptionTypes.register()
    
    services.AddOptions() |> ignore
    
    services.AddSingleton<IMapper>(createMapper) |> ignore

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