module App.Handlers.ApplicationHandler

open System.Data.SqlClient
open App.Common
open Domains.Applications.Application
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open PersistenceSQLClient.ApplicationData
open App.Common.Transaction


let getAllApplications = fun (con: SqlConnection) _ ->
    task {
        return! getAllApplicationsAsync con
    }

let getApplicationById = fun guid (con: SqlConnection) _ ->
      task {
          return! getAllApplicationById con guid
      }
   
    
let createApp = fun (con: SqlConnection) (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<Application>()
        return! createApplicationAsync con application
    }  
let deleteApplication = fun guid (con: SqlConnection) (ctx: HttpContext) ->
    task {
        return! deleteApplicationAsync con guid 
    }
    
let applicationsGetRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> transaction getAllApplications
    routef "/applications/%O" (fun guid -> authorize >=> transaction (getApplicationById guid))
]

let applicationPostRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> transaction createApp
]

let applicationDeleteRoutes: HttpHandler list = [
    routef "/applications/%O" (fun guid -> authorize >=> transaction (deleteApplication guid))
]