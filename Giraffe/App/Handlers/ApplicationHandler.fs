module App.Handlers.ApplicationHandler

open App.Common
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open Persistence.Data.ApplicationData
open App.Common.JsonApiResponse

let getAllApplications = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! applications = getAllApplicationsAsync
        return! json (jsonApiWrap applications) next ctx
    }

let getApplicationById = fun guid (next: HttpFunc) (ctx: HttpContext) ->
    task{
        let! application = getAllApplicationByIdAsync guid
        return! json (jsonApiWrap application) next ctx
    }
    
let applicationsGetRoutes: HttpHandler list = [
    route "/applications" >=> authorize'' >=> getAllApplications
    routef "/applications/%O" (fun guid -> authorize'' >=> getApplicationById guid)
]