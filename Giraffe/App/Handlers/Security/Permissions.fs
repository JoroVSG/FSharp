module App.Handlers.Security.Permissions

open System
open System.Security.Claims
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Helpers.MSALClient
open App.Helpers.Constants
open Persistence.Data.FiData
open App.Helpers.HelperFunctions

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

let fiAdminCheck = fun iid (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let isFiAdmin = getClaim FI_ADMIN_CLAIM_TYPE ctx
        
        let! tryInstitution = getFiByInstitutionId iid
        
        match tryInstitution with
        | Some institution ->
            let isProAdmin = getClaimValue ctx IS_PROFITSTARS_CLAIM_TYPE
            
            if isProAdmin then
                return! next ctx
            else
                let currentUserInstitutionId = getClaim INSTITUTION_ID_CLAIM_TYPE ctx
                // let! user = getUserByEmailAsync email.Value
                
                let identity = ctx.User.Identity :?> ClaimsIdentity
                let claimValue = string (bool isFiAdmin.Value && (string institution.InstitutionId = currentUserInstitutionId.Value))
                identity.AddClaim(Claim(IS_FI_ADMIN, claimValue))
                
                return! next ctx
        | None -> return! notFound "Financial Institution not found" next ctx
    }

let profitStarsAdminCheck = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let objectId = getClaim ClaimTypes.NameIdentifier ctx
        
        let apiUrl = sprintf "%s/users/%s/memberOf?$select=id,displayName" config.["GraphApi:ApiVersion"] objectId.Value
        let! result = sendGETGraphApiWithConfigRequest<B2CGroups> ctx apiUrl
        
        let isInProfitStarsGroup = result.Groups |> List.exists(fun group -> string group.Id = config.["GraphApi:ProfitStarsGroupId"])
        let identity = ctx.User.Identity :?> ClaimsIdentity
        identity.AddClaim(Claim(IS_PROFITSTARS_CLAIM_TYPE, string isInProfitStarsGroup))
        
        return! next ctx
    }
    
let profitStarsErrorHandling = fun (error: HttpHandler) (next: HttpFunc) (ctx: HttpContext) ->
    let isProfitStarsAdmin = (tryGetClaim IS_PROFITSTARS_CLAIM_TYPE ctx)
    
    match isProfitStarsAdmin with
        | Some claim -> if bool claim.Value = false then error next ctx else next ctx
        | None -> error next ctx

let profitStarsFiAdminErrorHandling = fun (error1: HttpHandler) (error2: HttpHandler) (next: HttpFunc) (ctx: HttpContext) ->
    let isProAdmin = getClaimValue ctx IS_PROFITSTARS_CLAIM_TYPE
    let isFiAdmin = getClaimValue ctx IS_FI_ADMIN
    
    if isProAdmin || isFiAdmin then next ctx
    else if isProAdmin = false then error1 next ctx
    else error2 next ctx
            

let combinedErrors = fun error1 error2 -> profitStarsFiAdminErrorHandling error1 error2
    
let profitStarsFiAdminCombined = fun iid -> profitStarsAdminCheck >=> fiAdminCheck iid
let profitStarsAdminCheckOny = fun errorHandler -> profitStarsAdminCheck >=> combinedErrors errorHandler errorHandler
let fiAdminCheckOny = fun errorHandler iid -> fiAdminCheck iid >=> combinedErrors errorHandler errorHandler