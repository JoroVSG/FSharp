module App.Common.Authentication
    
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.IdentityModel.Tokens
open FSharp.Control.Tasks.V2
open Giraffe

let unAuthorized: HttpHandler =
    RequestErrors.UNAUTHORIZED
        JwtBearerDefaults.AuthenticationScheme
        ""
        "Unauthorized access"


let getTokenValidationParameters = fun (config: IConfiguration) ->
    let wellKnowEndpoint = config.["AzureAd:Authority"] + ".well-known/openid-configuration"
    task {
        let configManager = ConfigurationManager<OpenIdConnectConfiguration>(wellKnowEndpoint, OpenIdConnectConfigurationRetriever())
        let! openIdConfigurations = configManager.GetConfigurationAsync()
        return TokenValidationParameters (
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
    }

let validateAuthHeader = fun (token: string) config ->
    task {
        let validator = JwtSecurityTokenHandler()
        let! validationParameters = getTokenValidationParameters config
        return validator.ValidateToken(token, validationParameters)
    }
   

let return401 = fun (ctx: HttpContext) ->
    task {
        ctx.SetStatusCode 401
        do! ctx.Response.WriteAsync "Unauthorized access"
        return Some ctx
    }

let return401': HttpHandler = setStatusCode 401 >=> text "Unauthorized access"
let authorize': HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let settings = ctx.GetService<IConfiguration>()
            match ctx.GetRequestHeader "Authorization" with
            | Ok headerValue ->
                let tokens = headerValue.Split " " |> Array.toList
                match tokens with
                | (_::token::_) ->
                    let! (user, _) = validateAuthHeader token settings
                    ctx.User <- user
                    let identity = ctx.User.Identity :?> ClaimsIdentity
                    identity.AddClaim(Claim("access_token", token))
                    return! next ctx
                | _ -> return! return401' next ctx
            | Error _ -> return! return401' next ctx
        }
        

let authorize'': HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            match ctx.User.Identity.IsAuthenticated with
            | true -> return! next ctx
            | false -> return! return401 ctx
        }
       
        


let authorize''': HttpHandler = requiresAuthentication unAuthorized
let authorize: HttpHandler = authorize'''  
