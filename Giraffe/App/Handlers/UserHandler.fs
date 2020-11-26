module App.Handlers.UserHandler

open FSharp.Data
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Persistence.Data.UserData
open App.Common.JsonApiResponse
open App.Common.Authentication
open App.Handlers.Security.Permissions
open App.Common.Exceptions

let getAllUsers = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! applications = getAllUsersAsync
        let res = jsonApiWrap applications
        return! json res next ctx
    }
    

let createResponse = fun status message ->
    setStatusCode status >=> json (createJsonApiError message status)
// let x =  createResponse HttpStatusCodes.Forbidden "Cannot query users from a different financial institution"


let fiAdminErrorHandler = forbidden "Cannot query users from a different financial institution"
let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"
let profitStarsAdminUsersCheck = x >=> forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"
let y: (string -> HttpHandler) = fun iid -> (x iid) >=> x' profitStarsErrorHandler fiAdminErrorHandler
let usersGetRoutes: HttpHandler list = [
    route "/users" >=> authorize >=> profitStarsAdminUsersCheck >=> getAllUsers
    routef "%s/relationship/users" (fun iid -> authorize >=> y >=> getAllUsers)
]