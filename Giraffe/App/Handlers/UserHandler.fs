module App.Handlers.UserHandler


open App.DTOs.ApplicationDTO
open AutoMapper
open Domains.B2CUser
open Domains.B2CUserResponse
open Domains.Users.CLCSUser
open Domains.Users.CommonTypes
open Giraffe
open JsonApiSerializer.JsonApi
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
open App.DTOs.UserDTO
open System.Threading.Tasks
open App.Common.Transaction
// open Persistence.Data.ApplicationData

let orEmptyString = fun endOfList -> if endOfList then "" else "or "
let getOperand = fun index -> isEndOfTheList index >> orEmptyString
let getInstitutionFilter = fun iid (clientId: string) ->
    let adminFilter = sprintf "$filter=extension_%s_InstitutionId eq '%s'"
    adminFilter (clientId.Replace("-", "")) (upper iid)
let createMsalFilter = fun iidFilter (objectIds: CLCSUser list) ->
    let userIdsMapped = objectIds |> Seq.mapi(fun index user -> sprintf "id eq '%s' %s" (string user.ObjectId.Value) (getOperand index objectIds) )
    let userIdToString = userIdsMapped |> String.concat(" ")
    sprintf "%s and (%s)" iidFilter userIdToString

let msalFilter = fun iid -> getInstitutionFilter iid >> createMsalFilter

let mapToUserDTO = fun apps (b2cUser:B2CUser) (clcsUser: CLCSUser) ->
    
        
    let relationshipApps = Relationship<ApplicationDTO list>()
    relationshipApps.Data <- apps
    let dto = {
        ObjectId = clcsUser.ObjectId
        Id = clcsUser.IdUser
        IdUser = clcsUser.IdUser
        Email = clcsUser.Email
        ActivationStatus = clcsUser.ActivationStatus
        DeletionTimestamp = b2cUser.DeletionTimestamp
        AccountEnabled = b2cUser.AccountEnabled
        City = b2cUser.City
        CompanyName = b2cUser.CompanyName
        Country = b2cUser.Country
        CreationType = b2cUser.CreationType
        Department = b2cUser.Department
        DisplayName = b2cUser.DisplayName
        EmployeeId = b2cUser.EmployeeId
        FirstName = b2cUser.GivenName
        LastName = b2cUser.Surname
        JobTitle = b2cUser.JobTitle
        Mobile = b2cUser.Mobile
        OtherMails = b2cUser.OtherMails
        PasswordPolicies = b2cUser.PasswordPolicies
        PasswordProfile = b2cUser.B2CPasswordProfile
        PostalCode = b2cUser.PostalCode
        SignInNames = b2cUser.SignInNames
        State = b2cUser.State
        StreetAddress = optional' b2cUser.StreetAddress
        TelephoneNumber = optional' b2cUser.TelephoneNumber
        UsageLocation = b2cUser.UsageLocation
        UserIdentities = b2cUser.UserIdentities
        UserPrincipalName = b2cUser.UserPrincipalName
        UserType = b2cUser.UserType
        Type = "user"
        Applications = relationshipApps
    }
    dto
        

let getAllUsersByFi = fun iid transPayload (ctx: HttpContext) ->
    task {
        let! emailsBody = ctx.ReadBodyFromRequestAsync()
        let clcsUsers = JsonConvert.DeserializeObject<Email seq>(emailsBody)
        let! usersByListOfEmails = getUsersByEmailAsync transPayload clcsUsers
        let config = ctx.GetService<IConfiguration>()
        let localUsers = usersByListOfEmails |> Seq.filter(fun us -> us.ObjectId.IsSome) |> Seq.toList
        
        let partitioned = localUsers.Batch(9)
        
        let! userMergedTasks =
            partitioned
            |> Seq.map(fun chunk ->
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
                    let matchFound = userMerged |> Seq.find (fun u -> user.ObjectId.Value = u.Id)
                    let! apps = getApplicationsByUserIdAsync transPayload user.IdUser
                    let appMapped = apps |> Seq.map(fun a -> mapper.Map<ApplicationDTO>(a)) |> Seq.toList
                    return mapToUserDTO appMapped matchFound user
                }
            )
            |> Task.WhenAll
        
        return
            users |> Array.toList
    }

let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let usersPermissionCheck = fun iid -> profitStarsFiAdminCombined iid >=> profitStarsFiAdminErrorHandling profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
    // route "/users" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getAllUsers
    routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> transaction (getAllUsersByFi iid))
]
let usersPostRoutes: HttpHandler list  = [
     routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> transaction (getAllUsersByFi iid))
]