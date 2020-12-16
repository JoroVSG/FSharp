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
open FsToolkit.ErrorHandling
open App.Helpers.HelperFunctions

let getAllApplications = fun payload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! models = getAllApplicationsAsync payload
        return models
            |> Seq.toList
            |> List.map (fun app -> mapper.Map<ApplicationDTO>(app))
            |> Ok
    }

let getApplicationById = fun guid payload (ctx: HttpContext) ->
      task {
          let! model = getAllApplicationById guid payload
          let mapper = ctx.GetService<IMapper>()
          return mapResultOrNotFound model (fun m -> mapper.Map<ApplicationDTO>(m))
      }


let createApp = fun payload (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<ApplicationDTO>()
        let mapper = ctx.GetService<IMapper>()
        let model = mapper.Map<Application>(application)
        let! result = createApplicationAsync model payload
        return result
    }  
let deleteApplication = fun guid payload _ ->
    task {
        let! res = deleteApplicationAsync guid payload
        return res |> resultOrNotFound
    }

let deleteApplicationWithError = fun guid payload (ctx: HttpContext) ->
    asyncResult {
      let! model = getApplicationById guid payload ctx
      let! _ = deleteApplication guid payload ctx
      let! x = createApp payload ctx
      return x
    }
    |> wrap
    

let applicationsGetRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> transaction getAllApplications
    routef "/applications/%O" (fun guid -> authorize >=> transaction (getApplicationById guid))
]

let applicationPostRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> transaction createApp
]

let applicationDeleteRoutes: HttpHandler list = [
    routef "/applications/%O" (fun guid -> authorize >=> transaction (deleteApplicationWithError guid))
]