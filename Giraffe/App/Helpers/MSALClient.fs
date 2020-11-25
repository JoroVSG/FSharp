module App.Helpers.MSALClient

open System
open System.Net
open Microsoft.Extensions.Configuration
open Microsoft.Identity.Client
open FSharp.Control.Tasks.V2.ContextInsensitive

open FSharp.Data.HttpRequestHeaders
open FSharp.Data
open Newtonsoft.Json

let acquireAccessTokenAsync = fun (config: IConfiguration) ->
   task {
        let authority = sprintf "%s%s.onmicrosoft.com" config.["GraphApi:Instance"] config.["GraphApi:Tenant"]
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
   
let sendGraphApiWithConfigRequest<'T> = fun method (config: IConfiguration) api ->
    task {
        let url = sprintf "%s%s" config.["GraphApi:ApiUrl"] api
        
        let! accessToken = acquireAccessTokenAsync config
        
        let! response = Http.AsyncRequestString(url,
                                                httpMethod = method,
                                                silentHttpErrors = true,
                                                headers = [ Authorization accessToken; ContentType HttpContentTypes.Json ])
        return JsonConvert.DeserializeObject<'T>(response)
    }
    
let sendGETGraphApiWithConfigRequest<'T> = sendGraphApiWithConfigRequest<'T> HttpMethod.Get
let sendPOSTGraphApiWithConfigRequest<'T> = sendGraphApiWithConfigRequest<'T> HttpMethod.Post