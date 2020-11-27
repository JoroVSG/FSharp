module App.Handlers.UserHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Persistence.Data.UserData
open App.Common.JsonApiResponse
open App.Common.Authentication
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions

let getAllUsers = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! applications = getAllUsersAsync
        let res = jsonApiWrap applications
        return! json res next ctx
    }
    

let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let usersPermissionCheck = fun iid -> profitStarsFiAdminCombined iid >=> combinedErrors profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
    route "/users" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getAllUsers
    routef "/%s/relationship/users" (fun iid -> authorize >=> usersPermissionCheck iid >=> getAllUsers)
]