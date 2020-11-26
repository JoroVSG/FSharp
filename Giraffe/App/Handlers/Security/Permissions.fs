module App.Handlers.Security.Permissions

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
open Persistence.Data.FiData
open Persistence.Data.UserData

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

let IS_PROFITSTARS_CLAIM_TYPE = "IsProfitstarsAdmin";
let EMAIL_CLAIM_TYPE = "emails";
let FI_ADMIN_CLAIM_TYPE = "extension_IsFiAdmin"
let IS_FI_ADMIN = "isFiAdmin"

let INSTITUTION_ID_CLAIM_TYPE = "extension_InstitutionId";

let getClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.find (fun claim -> claim.Type = claimType)

let createResponse = fun status message ->
    setStatusCode status >=> json (createJsonApiError message status)

let notFound = createResponse HttpStatusCodes.NotFound
let forbidden: (string -> HttpHandler) = createResponse HttpStatusCodes.Forbidden


let isFiAdminCheck = fun iid (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let isProfitstarsAdmin = getClaim IS_PROFITSTARS_CLAIM_TYPE ctx
        
        if isProfitstarsAdmin.Value = "true" then
            return! next ctx
        else
            let email = getClaim EMAIL_CLAIM_TYPE ctx
            let isFiAdmin = getClaim FI_ADMIN_CLAIM_TYPE ctx
            
            let! institution = getFiByInstitutionId iid
            
            if institution.IdFinancialInstitution = Guid.NewGuid() then
                return! notFound "Financial Institution not found" next ctx
            else
                let currentUserInstitutionId = getClaim INSTITUTION_ID_CLAIM_TYPE ctx
                let! user = getUserByEmailAsync email.Value
                
                let identity = ctx.User.Identity :?> ClaimsIdentity
                identity.AddClaim(Claim(IS_FI_ADMIN, string isFiAdmin))
                
                return! next ctx
                
//                if string user.IdFinancialInstitution <> currentUserInstitutionId.Value then
//                    //return! forbidden message next ctx
//                    return None
//                else
//                    return! next ctx
                    
        
    }

let profitStarsAdminCheck = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let objectId = getClaim ClaimTypes.NameIdentifier ctx
        
        let apiUrl = sprintf "%s/users/%s/memberOf?$select=id,displayName" config.["GraphApi:ApiVersion"] objectId.Value
        let! result = sendGETGraphApiWithConfigRequest<B2CGroups> config apiUrl
        
        let isInProfitStarsGroup = result.Groups |> List.exists(fun group -> string group.Id = config.["GraphApi:ProfitStarsGroupId"])
        let identity = ctx.User.Identity :?> ClaimsIdentity
        identity.AddClaim(Claim(IS_PROFITSTARS_CLAIM_TYPE, string isInProfitStarsGroup))
        
        return! next ctx
                                   
//        if isInProfitStarsGroup then
//            let! res = next ctx
//            return res
//        else
////            let error = notFound message
////            return! error next ctx
//            return None
    }
    
let x' = fun (error1: HttpHandler) (error2: HttpHandler) (next: HttpFunc) (ctx: HttpContext) ->
    let isProfitStarsAdmin = (getClaim IS_PROFITSTARS_CLAIM_TYPE ctx).Value
    let isFiAdmin = (getClaim IS_FI_ADMIN ctx).Value
    
    if isProfitStarsAdmin = "false" then
        error1 next ctx
    else if isFiAdmin = "false" then
        error2 next ctx
    else
        next ctx
        
    
let x = fun iid -> profitStarsAdminCheck >=> isFiAdminCheck iid