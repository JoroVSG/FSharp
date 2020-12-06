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

let getAllApplications = fun transPayload _ ->
    task {
        return! getAllApplicationsAsync transPayload
    }

let getApplicationById = fun guid transPayload _ ->
      task {
          return! getAllApplicationById transPayload guid
      }
   
    
let createApp = fun transPayload (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<Application>()
        return! createApplicationAsync transPayload application
    }  
let deleteApplication = fun guid transPayload ctx ->
    task {
        let! res = deleteApplicationAsync transPayload guid
        let! _ = createApp transPayload ctx
        return res
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