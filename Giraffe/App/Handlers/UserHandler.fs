module App.Handlers.UserHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Persistence.Data.UserData
open App.Common.JsonApiResponse
open App.Common.Authentication
open App.Handlers.Security.ProfitstarsAdmin

let getAllUsers = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! applications = getAllUsersAsync
        let res = jsonApiWrap applications
        return! json res next ctx
    }
    
let usersGetRoutes: HttpHandler list = [
    route "/users" >=> authorize >=> profitStarsAdminCheck >=> getAllUsers
]