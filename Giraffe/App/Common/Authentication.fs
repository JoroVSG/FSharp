module App.Common.Authentication
    
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Threading
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
       

let validateAzureB2CToken = fun (ctx: HttpContext) (token: string) ->
    task {
        try
            let config = ctx.GetService<IConfiguration>()
            let authority = sprintf "https://%s.b2clogin.com/%s.onmicrosoft.com/%s/v2.0"
                                    config.["Authentication:AzureAdB2C:Tenant"]
                                    config.["Authentication:AzureAdB2C:Tenant"]
                                    config.["Authentication:AzureAdB2C:InvitePolicyId"]
                                    
            let configurationManager =
                         ConfigurationManager<OpenIdConnectConfiguration>(sprintf "%s/.well-known/openid-configuration" authority,
                             OpenIdConnectConfigurationRetriever());

            let! openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None);

            let parameters =
                    TokenValidationParameters (
                        ValidateIssuer = true,
                        ValidIssuer = openIdConfig.Issuer,
                        ValidateIssuerSigningKey = true,
                        ValidateAudience = true,
                        ValidAudiences = [
                            config.["AzureAd:IcApplicationId"]
                        ],
                        IssuerSigningKeys = openIdConfig.SigningKeys
                    )
            let handler = JwtSecurityTokenHandler()
        
            let (claimsPrincipal, _) = handler.ValidateToken(token, parameters)
            ctx.User <- claimsPrincipal
            return claimsPrincipal |> Ok
        
        with ex -> return Error ex
    }
    
    


let authorize''': HttpHandler = requiresAuthentication unAuthorized
let authorize: HttpHandler = authorize'''  
