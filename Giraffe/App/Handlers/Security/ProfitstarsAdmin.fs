module App.Handlers.Security.ProfitstarsAdmin

open System
open System.Security.Claims
open FSharp.Data
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Helpers.MSALClient
open App.Common.Exceptions

[<CLIMutable>]
type B2CGroup = {
    [<JsonProperty("id")>]Id: Guid
    [<JsonProperty("displayName")>]DisplayName: string
    [<JsonProperty("mailEnabled")>]MailEnabled: bool
    [<JsonProperty("mailNickname")>]MailNickname: string
    [<JsonProperty("securityEnabled")>]SecurityEnabled: bool
}

type B2CGroups = {
    [<JsonProperty("value")>]Groups: B2CGroup list
}

    

let profitStarsAdminCheck = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let objectId = ctx.User.Claims |> Seq.find (fun claim -> claim.Type = ClaimTypes.NameIdentifier)
        
        let apiUrl = sprintf "%s/users/%s/memberOf?$select=id,displayName" config.["GraphApi:ApiVersion"] objectId.Value
        let! result = sendGETGraphApiWithConfigRequest<B2CGroups> config apiUrl
        
        let isInProfitStarsGroup = result.Groups |> List.exists(fun group -> string group.Id = config.["GraphApi:ProfitStarsGroupId"])
                                   
        if isInProfitStarsGroup then
            return! next ctx
        else
            let error = setStatusCode 401 >=> json (createJsonApiError "Only Profitstars admins are allowed to query users" HttpStatusCodes.Unauthorized)
            return! error next ctx
    }

