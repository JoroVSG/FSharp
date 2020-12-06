module App.Handlers.ApplicationHandler

open System.Data.SqlClient
open App.Common
open Domains.Applications.Application
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open Persistence.Data.ApplicationData
open App.Common.JsonApiResponse
open App.Common.Transaction


let getAllApplications = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! applications = getAllApplicationsAsync
        let res = jsonApiWrap applications
        return! json res next ctx
    }

let getApplicationById = fun guid (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! application = getAllApplicationByIdAsync guid
        return! json (jsonApiWrap application) next ctx
    }

let x = fun guid  ->
     fun (con: SqlConnection) ->
        task {
            let! application = getAllApplicationByIdAsync guid
            return application
        }
        
let y iid (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! res = withTransaction (x iid) next ctx   
        return! res
    }
    
   
let createApplication: HttpHandler = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<Application>()
        let! newApp = createApplication application 
        return! json (jsonApiWrap newApp) next ctx
    }
    
let deleteApplication = fun guid (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! newApp = deleteApplication guid 
        return! json (jsonApiWrap newApp) next ctx
    }
    
let applicationsGetRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> getAllApplications
    routef "/applications/%O" (fun guid -> authorize >=> getApplicationById guid)
]

let applicationPostRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> createApplication
]

let applicationDeleteRoutes: HttpHandler list = [
    routef "/applications/%O" (fun guid -> authorize >=> y guid)
]