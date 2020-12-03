module App.Handlers.UserHandler

open System
open Domains.B2CUser
open Domains.B2CUserResponse
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration
open Persistence.Data.UserData
open App.Common.JsonApiResponse
open App.Common.Authentication
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions
open App.Helpers.MSALClient
open Domains.Users.UserDTO

let getAllUsers = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! u = getAllUsersAsync
        let res = jsonApiWrap u
        
        let config = ctx.GetService<IConfiguration>()
        let api = sprintf "%s/users" config.["GraphApi:ApiVersion"]
        let! b2cUsers = sendGETGraphApiWithConfigRequest<B2CResponse> ctx api
        let users = jsonApiWrap b2cUsers.B2CGraphUsers
        
        let x = res.Data |> Seq.map (fun user ->
            let mat = b2cUsers.B2CGraphUsers |> Seq.find (fun u -> string user.ObjectId = u.ObjectId)
            let y: UserDTO =
                {
                    ObjectId = user.ObjectId
                    Id = user.Id
                    IdUser = user.Id
                    Email = user.Email
                    DeletionTimestamp = mat.DeletionTimestamp
                    AccountEnabled = mat.AccountEnabled
                    City = mat.City
                    CompanyName = mat.CompanyName
                    Country = mat.Country
                    CreationType = mat.CreationType
                    Department = mat.Department
                    DisplayName = mat.DisplayName
                    EmployeeId = mat.EmployeeId
                    FirstName = mat.GivenName
                    LastName = mat.Surname
                    JobTitle = mat.JobTitle
                    Mobile = mat.Mobile
                    OtherMails = mat.OtherMails
                    PasswordPolicies = mat.PasswordPolicies
                    PasswordProfile = mat.B2CPasswordProfile
                    PostalCode = mat.PostalCode
                    SignInNames = mat.SignInNames
                    State = mat.State
                    StreetAddress = mat.StreetAddress
                    TelephoneNumber = mat.TelephoneNumber
                    UsageLocation = mat.UsageLocation
                    UserIdentities = mat.UserIdentities
                    UserPrincipalName = mat.UserPrincipalName
                    UserType = mat.UserType
                    Type = failwith "todo"
                    ActivationStatus = user.ActivationStatus
                    IsFiAdmin = user.IsFiAdmin}
            y
        )
        return! json res next ctx
    }
//let mapping = fun (b2cUser: B2CUser) (user: CLCSUser) ->
//    {|
//       b2cUser 
//    |}

let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let usersPermissionCheck = fun iid -> profitStarsFiAdminCombined iid >=> combinedErrors profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
    route "/users" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getAllUsers
    routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> getAllUsers)
]