module App.Helpers.MSALClient

open System
open System.Net
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Identity.Client
open System.IdentityModel.Tokens.Jwt
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open FSharp.Data.HttpRequestHeaders
open FSharp.Data
open Newtonsoft.Json

type MSALAccessTokenHolder = {
    mutable AccessToken: string option
}
let acquireAccessTokenAsync = fun (config: IConfiguration) ->
    let authority = sprintf "%s%s.onmicrosoft.com" config.["GraphApi:Instance"] config.["GraphApi:Tenant"]
    task {
        let clientId = config.["GraphApi:ClientId"]
        let secret = config.["GraphApi:ClientSecret"]
        let app = ConfidentialClientApplicationBuilder
                      .Create(clientId)
                      .WithClientSecret(secret)
                      .WithAuthority(Uri(authority))
                      .Build()
        
        let scope = sprintf "%s.default" config.["GraphApi:ApiUrl"]
        
        let scopes = [scope]
        let! res = app.AcquireTokenForClient(scopes).ExecuteAsync()
        return res.AccessToken 
    }
let acquireAccessTokenAndMutateAsync = fun tokenHolder (config: IConfiguration) ->
    task {
        let! token = acquireAccessTokenAsync config
        tokenHolder.AccessToken <- Some token
        return token
    }

let validateMsalToken = fun (config: IConfiguration) tokenHolder ->
   task {
        match tokenHolder.AccessToken with
        | Some access_token ->
            let validator = JwtSecurityTokenHandler()
            let now = DateTime.UtcNow
            let token = validator.ReadToken(access_token) :?> JwtSecurityToken
            if token.Payload.ValidTo > now then
                return access_token
            else return! acquireAccessTokenAndMutateAsync tokenHolder config
                
        | None -> return! acquireAccessTokenAndMutateAsync tokenHolder config
    }
  
let sendGraphApiWithConfigRequest<'T> = fun method body (ctx: HttpContext) api ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let msalTokenHolder = ctx.GetService<MSALAccessTokenHolder>()
        let url = sprintf "%s%s" config.["GraphApi:ApiUrl"] api
        
        let! accessToken = validateMsalToken config msalTokenHolder
        
        let! response = Http.AsyncRequestString(url,
                                                httpMethod = method,
                                                // silentHttpErrors = true,
                                                body = TextRequest body,
                                                headers = [ Authorization accessToken; ContentType HttpContentTypes.Json ])
        return JsonConvert.DeserializeObject<'T>(response)
    }
    
let sendGETGraphApiWithConfigRequest<'T> = sendGraphApiWithConfigRequest<'T> HttpMethod.Get ""
let sendPOSTGraphApiWithConfigRequest<'T> body = sendGraphApiWithConfigRequest<'T> HttpMethod.Post body
let sendPATCHGraphApiWithConfigRequest<'T> body = sendGraphApiWithConfigRequest<'T> HttpMethod.Patch body
let sendDELETEGraphApiWithConfigRequest<'T> = sendGraphApiWithConfigRequest<'T> HttpMethod.Delete ""