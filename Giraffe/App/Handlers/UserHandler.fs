module App.Handlers.UserHandler

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

let getAllUsers = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! u = getAllUsersAsync
        let res = jsonApiWrap u
        
        let config = ctx.GetService<IConfiguration>()
        let api = sprintf "%s/users" config.["GraphApi:ApiVersion"]
        let! b2cUsers = sendGETGraphApiWithConfigRequest<B2CResponse> ctx api
        let users = jsonApiWrap b2cUsers.B2CGraphUsers;
        return! json users next ctx
    }
    

let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let usersPermissionCheck = fun iid -> profitStarsFiAdminCombined iid >=> combinedErrors profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
    route "/users" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getAllUsers
    routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> getAllUsers)
]