module JwtApp.App

open System
open System.IO
open System.Linq.Expressions
open App.DTOs.ApplicationDTO
open Domains.Applications.Application
open App.Helpers.MSALClient
open Giraffe
open Giraffe.Serialization
// open App.Common.Converters
open Newtonsoft.Json.FSharp
open JsonApiSerializer
open AutoMapper
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
//  App.Handlers.UserHandler

type AutoMapper.IMappingExpression<'TSource, 'TDestination> with
    // The overloads in AutoMapper's ForMember method seem to confuse
    // F#'s type inference, forcing you to supply explicit type annotations
    // for pretty much everything to get it to compile. By simply supplying
    // a different name, 
    member this.ForMemberFs<'TMember>
            (destGetter:Expression<Func<'TDestination, 'TMember>>,
             sourceGetter:Action<IMemberConfigurationExpression<'TSource, 'TDestination, 'TMember>>) =
        this.ForMember(destGetter, sourceGetter)

type OptionExpressions =
    static member MapFrom<'source, 'destination, 'sourceMember, 'destinationMember> (e: 'source -> 'sourceMember) =
        System.Action<IMemberConfigurationExpression<'source, 'destination, 'destinationMember>> (fun (opts: IMemberConfigurationExpression<'source, 'destination, 'destinationMember>) -> opts.MapFrom(e))
    static member UseValue<'source, 'destination, 'value> (e: 'value) =
        System.Action<IMemberConfigurationExpression<'source, 'destination, 'value>> (fun (opts: IMemberConfigurationExpression<'source, 'destination, 'value>) -> opts.UseValue(e))
    static member Ignore<'source, 'destination, 'destinationMember> () =
        System.Action<IMemberConfigurationExpression<'source, 'destination, 'destinationMember>> (fun (opts: IMemberConfigurationExpression<'source, 'destination, 'destinationMember>) -> opts.Ignore())

let mutable Configurations: IConfigurationRoot = null

let allGetRoutes: HttpHandler list =
    [ route "/" >=> text "Public endpoint."]
    @ applicationsGetRoutes
    // @ usersGetRoutes

let allPostRoutes: HttpHandler list = applicationPostRoutes
                                      //@ usersPostRoutes
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
    settings.Converters.Add(OptionConverter())
    // settings.Converters.Add(OptionConverter())
        
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(settings)) |> ignore
    services.AddSingleton<MSALAccessTokenHolder>({ AccessToken = None }) |> ignore
    Dapper.FSharp.OptionTypes.register() |> ignore
    let configuration = MapperConfiguration(fun cfg ->
        cfg.CreateMap<Application, ApplicationDTO>()
            .ForMemberFs(
                (fun d -> d.Id),
                (fun opts -> opts.MapFrom(fun s -> s.IdApplication))
            ) |> ignore
        cfg.CreateMap<ApplicationDTO, Application>()
            .ForMemberFs(
                (fun d -> d.IdApplication),
                (fun opts -> opts.MapFrom(fun s -> s.Id))
            )|> ignore
    )
    
    let mapper = configuration.CreateMapper()
    
    services.AddSingleton<IMapper>(mapper) |> ignore

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