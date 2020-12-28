module App.Handlers.UserHandler

open App.DTOs.ApplicationDTO
open AutoMapper
open Domains.B2CUser
open Domains.B2CUserResponse
open Domains.Users.CLCSUser
open Domains.Users.CommonTypes
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open MoreLinq
open PersistenceSQLClient.ApplicationData
open PersistenceSQLClient.UserData
open App.Common.Authentication
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions
open App.Helpers.MSALClient
open System.Threading.Tasks
open App.Common.Transaction
open App.Mapping.UserMapper
open FSharp.Core.Fluent
open App.Common.Exceptions

let orEmptyString = fun endOfList -> if endOfList then "" else "or "

let getOperand = fun index -> isEndOfTheList index >> orEmptyString

let getInstitutionFilter = fun iid (clientId: string) ->
    let adminFilter = sprintf "$filter=extension_%s_InstitutionId eq '%s'"
    adminFilter (clientId.Replace("-", "")) (upper iid)


let getObjectId = fun u -> string <| getValue u.ObjectId
let createMsalFilter = fun iidFilter (objectIds: CLCSUser list) ->
    //let userIdsMapped = objectIds |> Seq.mapi(fun index user -> sprintf "id eq '%s' %s" (string user.ObjectId.Value) (getOperand index objectIds) )
    let userIdsMapped = objectIds.mapi(fun index user -> sprintf "id eq '%s' %s" (getObjectId user) (getOperand index objectIds) )
    let userIdToString = userIdsMapped |> String.concat(" ")
    sprintf "%s and (%s)" iidFilter userIdToString

let msalFilter = fun iid -> getInstitutionFilter iid >> createMsalFilter
     
let getAllUsersByFi = fun iid transPayload (ctx: HttpContext) ->
    task {
        let! emailsBody = ctx.ReadBodyFromRequestAsync()
        let clcsUsers = JsonConvert.DeserializeObject<Email seq>(emailsBody)
        let! usersByListOfEmails = getUsersByEmailAsync clcsUsers transPayload
        let config = ctx.GetService<IConfiguration>()
        let localUsers = usersByListOfEmails.filter(fun us -> us.ObjectId.IsSome) |> Seq.toList
        
        let partitioned = localUsers.Batch(9)
        
        let! userMergedTasks =
            partitioned
                .map(fun chunk ->
                task {
                    let graphApiUserFilter = msalFilter iid config.["GraphApi:ClientId"] (chunk |> Seq.toList)
                    let api = sprintf "%s/users?%s" config.["GraphApi:ApiVersion"] graphApiUserFilter
                    
                    let! b2cUsers = sendGETGraphApiWithConfigRequest<B2CResponse> ctx api
                    return b2cUsers.B2CGraphUsers
                })
            |> Task.WhenAll
            
            
        let userMerged = userMergedTasks |> Array.toList |> List.concat
        let mapper = ctx.GetService<IMapper>()
        
        let! users =
            localUsers
            |> Seq.map (fun user ->
                task {
                    let matchFound = userMerged.find(fun u -> user.ObjectId.Value = u.Id)
                    let! apps = getApplicationsByUserIdAsync user.IdUser transPayload
                    let appMapped = apps.map(mapper.Map<ApplicationDTO>) |> Seq.toList
                    return mapToUserDTO appMapped matchFound user
                }
            )
            |> Task.WhenAll
        
        return
            users 
                |> Array.toList
                |> Ok
    }

let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let usersPermissionCheck = fun iid -> profitStarsFiAdminCombined iid >=> profitStarsFiAdminErrorHandling profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
     routeCif "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> transaction (getAllUsersByFi iid))
]
let usersPostRoutes: HttpHandler list  = [
     routeCif "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> transaction (getAllUsersByFi iid))
]