module App.Handlers.ApplicationHandler
open App.Common
open App.DTOs.ApplicationDTO
open AutoMapper
open Domains.Applications.Application
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open PersistenceSQLClient.ApplicationData
open App.Common.Transaction

let getAllApplications = fun transPayload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! models = getAllApplicationsAsync transPayload
        return models
            |> Seq.toList
            |> List.map (fun app -> mapper.Map<ApplicationDTO>(app))
    }

let getApplicationById = fun guid transPayload (ctx: HttpContext) ->
      task {
          let! model = getAllApplicationById transPayload guid
          return
            match model with
                | Some m ->
                    let mapper = ctx.GetService<IMapper>()
                    mapper.Map<ApplicationDTO>(m) |> Some
                | None -> None
      }
   
    
let createApp = fun transPayload (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<ApplicationDTO>()
        let mapper = ctx.GetService<IMapper>()
        let model = mapper.Map<Application>(application)
        return! createApplicationAsync transPayload model
    }  
let deleteApplication = fun guid transPayload ctx ->
    task {
        let! res = deleteApplicationAsync transPayload guid
        //let! x = createApp transPayload ctx
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