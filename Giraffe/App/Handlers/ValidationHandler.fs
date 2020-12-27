module App.Handlers.ValidationHandler

open System.IdentityModel.Tokens.Jwt
open System.Threading
open System.Web
open App.DTOs.ActivationKey
open Crypto
open Giraffe
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Helpers.HelperFunctions
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.IdentityModel.Tokens
open Newtonsoft.Json

let invitation = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let crypto = ctx.GetService<ICryptoService>()
        let config = ctx.GetService<IConfiguration>()
        let activationKey =
            match ctx.TryGetQueryStringValue "activationKey" with
                | Some ak -> ak
                | None -> ""
        
        let decoded = decodeBase64 activationKey
        
        let wrapper = JsonConvert.DeserializeObject<ActivationKeyWrapper>(decoded);
        
        let encryptedJson = crypto.Decrypt(wrapper.ActivationKeyEncrypted, config.["SecretPassPhrase"]);
            
        let key = JsonConvert.DeserializeObject<ActivationKey>(encryptedJson)
        
        let url = sprintf "%s://%s%s/api/validation/redeemed"
                           ctx.Request.Scheme
                           (string ctx.Request.Host)
                           (string ctx.Request.PathBase)

        let properties = AuthenticationProperties (RedirectUri = url);
        

        properties.Items.["Policy"] <- config.["Authentication:AzureAdB2C:InvitePolicyId"]
        properties.Items.["verified_email"] <- key.Email
        properties.Items.["display_name"] <- wrapper.DisplayName
        properties.Items.["first_name"] <- wrapper.FirstName
        properties.Items.["last_name"] <- wrapper.LastName
        properties.Items.["phone"] <- wrapper.Phone

        properties.Items.["activationKey"] <- wrapper.ActivationKeyEncrypted
        let schema = OpenIdConnectDefaults.AuthenticationScheme
        
        do! ctx.ChallengeAsync(schema, properties = properties)
        
        return! next ctx
    }
    
let redeemed = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! body = ctx.ReadBodyFromRequestAsync()
        let config = ctx.GetService<IConfiguration>()
        let inputClaims = HttpUtility.ParseQueryString(body)
        
        let authority = sprintf "https://%s.b2clogin.com/%s.onmicrosoft.com/%s/v2.0"
                            config.["Authentication:AzureAdB2C:Tenant"]
                            config.["Authentication:AzureAdB2C:Tenant"]
                            config.["Authentication:AzureAdB2C:InvitePolicyId"]
                            
        let configurationManager =
                     ConfigurationManager<OpenIdConnectConfiguration>(sprintf "%s/.well-known/openid-configuration" authority,
                         OpenIdConnectConfigurationRetriever());

        let! openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None);

        let parameters = TokenValidationParameters (
                                ValidateIssuer = true,
                                ValidIssuer = openIdConfig.Issuer,
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKeys = openIdConfig.SigningKeys
                            )
        let handler = JwtSecurityTokenHandler()
        let token = inputClaims.["access_token"]

        let claimsPrincipal = handler.ValidateToken(token, parameters);

                //await _userMappingService.MapUserAsync(claimsPrincipal);

               // Response.Redirect(_appSettings.DashboardUrl);
        return! next ctx
    }
    
let validationGetRoutes: HttpHandler list = [
    routeCi "/validation/invitation" >=> invitation
]

let validationPostRoutes: HttpHandler list = [
    routeCi "/validation/redeemed" >=> redeemed
]