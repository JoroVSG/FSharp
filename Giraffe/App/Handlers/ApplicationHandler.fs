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
open PersistenceSQLClient.DbConfig
open System.Threading.Tasks
open Domains.Common.CommonTypes

let getAllApplications = fun payload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! models = getAllApplicationsAsync payload
        return models
            |> Seq.toList
            |> List.map mapper.Map<ApplicationDTO>
            |> Ok
    }

let getApplicationById = fun guid payload (ctx: HttpContext) ->
      task {
          let! model = getAllApplicationById guid payload
          let mapper = ctx.GetService<IMapper>()
          return mapResultOrNotFound model mapper.Map<ApplicationDTO>
      }


let createApp = fun payload (ctx: HttpContext) ->
    task {
        let! application = ctx.BindJsonAsync<ApplicationDTO>()
        let mapper = ctx.GetService<IMapper>()
        let model = mapper.Map<Application> application
        let! result = createApplicationAsync model payload
        return result
    }  
let deleteApplication = fun guid payload _ ->
    task {
        let! res = deleteApplicationAsync guid payload
        return res |> resultOrNotFound
    }

let deleteApplication' = fun (op: OperationStatus) payload _ ->
    task {
        let! res = deleteApplicationAsync op.Id payload
        return res |> resultOrNotFound
    }
 

let createAndDelete = createApp >>> deleteApplication'

let deleteApplicationWithError guid = deleteApplication guid => createApp => getApplicationById guid

let deleteApplicationHandler guid payload ctx =
    asyncResult {
        let! res = deleteApplicationWithError guid payload ctx
        let (_, r3) = res
        return r3
    }
    |> wrap
    
let applicationsGetRoutes: HttpHandler list = [
    routeCi "/applications" >=> authorize >=> transaction getAllApplications
    routeCif "/applications/%O" (fun guid -> authorize >=> transaction (getApplicationById guid))
]

let applicationPostRoutes: HttpHandler list = [
    routeCi "/applications" >=> authorize >=> transaction createAndDelete
]

let applicationDeleteRoutes: HttpHandler list = [
    routeCif "/applications/%O" (fun guid -> authorize >=> transaction (deleteApplication guid))
]