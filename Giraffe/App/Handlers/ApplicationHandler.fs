module App.Handlers.ApplicationHandler
open App.Common
open App.DTOs.ApplicationDTO
open App.Mapping.ApplicationMapper
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open PersistenceSQLClient.ApplicationData
open App.Common.Transaction

let getAllApplications = fun transPayload _ ->
    task {
        let! models = getAllApplicationsAsync transPayload
        return models
            |> List.map (fun app -> modelToDto app)
    }

let getApplicationById = fun guid transPayload _ ->
      task {
          let! model = getAllApplicationById transPayload guid
          return
            match model with
                | Some m -> modelToDto m |> Some
                | None -> None
      }
   
    
let createApp = fun transPayload (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<ApplicationDTO>()
        let model = dtoToModel application
        return! createApplicationAsync transPayload model
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