module App.Common.Authentication
    
open System.IdentityModel.Tokens.Jwt
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.IdentityModel.Tokens
open FSharp.Control.Tasks.V2
open Giraffe

let getTokenValidationParameters = fun (config: IConfiguration) ->
    let wellKnowEndpoint = config.["AzureAd:Authority"] + ".well-known/openid-configuration"
    let configManager = ConfigurationManager<OpenIdConnectConfiguration>(wellKnowEndpoint, OpenIdConnectConfigurationRetriever())
    let openIdConfigurations = configManager.GetConfigurationAsync().Result
    TokenValidationParameters (
        ValidateIssuer = true,
        ValidIssuer = openIdConfigurations.Issuer,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeys = openIdConfigurations.SigningKeys,
        ValidateAudience = true,
        ValidAudiences = [
            config.["AzureAd:Audience"]
            config.["AzureAd:IcApplicationId"]
        ]
    )

let validateAuthHeader = fun (bearerToken: string) config ->
    let validator = JwtSecurityTokenHandler()
    let token = (bearerToken.Split " ").[1]
    let validationParameters = getTokenValidationParameters config
    let principal = validator.ValidateToken(token, validationParameters)
    principal

let authorize': HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let settings = ctx.GetService<IConfiguration>()
            match ctx.GetRequestHeader "Authorization" with
            | Ok headerValue ->
                let (a, _) = validateAuthHeader headerValue settings
                ctx.User <- a
                return! next ctx 
            | Error _ ->
                ctx.SetStatusCode 401
                do! ctx.Response.WriteAsync "Unauthorized access"
                return Some ctx
        }
        
