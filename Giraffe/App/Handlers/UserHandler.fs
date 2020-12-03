module App.Handlers.UserHandler

open System
open Domains.B2CUser
open Domains.B2CUserResponse
open Domains.Users
open Domains.Users.CommonTypes
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open Persistence.Data.UserData
open App.Common.JsonApiResponse
open App.Common.Authentication
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions
open App.Helpers.MSALClient
open Domains.Users.UserDTO

let orEmptyString = fun endOfList -> if endOfList then "" else "or "
let getOperand = fun index -> isEndOfTheList index >> orEmptyString
let getInstitutionFilter = fun iid (clientId: string) ->
    sprintf "$filter=extension_%s_InstitutionId eq '%s'" (clientId.Replace("-", "")) (upper iid)
let createMsalFilter = fun iidFilter (objectIds: CLCSUser list) ->
    let userIdsMapped = objectIds |> Seq.mapi(fun index user -> sprintf "id eq '%s' %s" (string user.ObjectId) (getOperand index objectIds) )
    let userIdToString = userIdsMapped |> String.concat(" ")
    sprintf "%s and (%s)" iidFilter userIdToString

let msalFilter = fun iid -> getInstitutionFilter iid >> createMsalFilter

let mapToUserDTO = fun (b2cUser:B2CUser) (clcsUser: CLCSUser) ->
    {   ObjectId = clcsUser.ObjectId
        Id = clcsUser.Id
        IdUser = clcsUser.Id
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
        StreetAddress = b2cUser.StreetAddress
        TelephoneNumber = b2cUser.TelephoneNumber
        UsageLocation = b2cUser.UsageLocation
        UserIdentities = b2cUser.UserIdentities
        UserPrincipalName = b2cUser.UserPrincipalName
        UserType = b2cUser.UserType
        // IsFiAdmin = user.IsFiAdmin
        Type = "user"
    }

let getAllUsers = fun iid (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! emailsBody = ctx.ReadBodyFromRequestAsync()
        let clcsUsers = JsonConvert.DeserializeObject<Email seq>(emailsBody)
        let! usersByListOfEmails = getUsersByEmailAsync clcsUsers
        let config = ctx.GetService<IConfiguration>()
        let localUsers = usersByListOfEmails |> Seq.filter(fun us -> us.ObjectId <> Unchecked.defaultof<Guid>) |> Seq.toList
        
        let graphApiUserFilter = msalFilter iid config.["GraphApi:ClientId"] localUsers
        let api = sprintf "%s/users?%s" config.["GraphApi:ApiVersion"] graphApiUserFilter
        
        let! b2cUsers = sendGETGraphApiWithConfigRequest<B2CResponse> ctx api
        
        let users =
            localUsers
            |> Seq.map (fun user ->
                let matchFound = b2cUsers.B2CGraphUsers |> Seq.find (fun u -> user.ObjectId = u.Id)
                mapToUserDTO matchFound user
            )
        return! json (jsonApiWrap users) next ctx
    }

let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let usersPermissionCheck = fun iid -> profitStarsFiAdminCombined iid >=> combinedErrors profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
    // route "/users" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getAllUsers
    routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> getAllUsers iid)
]
let usersPostRoutes: HttpHandler list  = [
     routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> getAllUsers iid)
]