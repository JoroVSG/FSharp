module App.Handlers.ApplicationHandler
open App.Common
open App.DTOs.ApplicationDTO
open AutoMapper
open Domains.Applications.Application
open Giraffe
open Authentication
open Microsoft.AspNetCore.Http
open PersistenceSQLClient.ApplicationData
open App.Common.Transaction
open App.Common.JsonApiResponse
open FSharp.Control.Tasks.V2.ContextInsensitive

let getAllApplications = fun transPayload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! models = getAllApplicationsAsync transPayload
        return models
            |> Seq.toList
            |> List.map (fun app -> mapper.Map<ApplicationDTO>(app))
            |> Ok
    }

let getApplicationById = fun guid transPayload (ctx: HttpContext) ->
      task {
          let! model = getAllApplicationById guid transPayload
          let mapper = ctx.GetService<IMapper>()
          return mapResultOrNotFound model (fun m -> mapper.Map<ApplicationDTO>(m))
      }


let createApp = fun transPayload (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<ApplicationDTO>()
        let mapper = ctx.GetService<IMapper>()
        let model = mapper.Map<Application>(application)
        let! result = createApplicationAsync model transPayload
        return result
    }  
let deleteApplication = fun guid transPayload ctx ->
    task {
        let! res = deleteApplicationAsync guid transPayload
        let! x = createApp transPayload ctx
        return res |> resultOrNotFound
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