module App.Handlers.ApplicationHandler
open System
open App.Common
open App.DTOs.ApplicationDTO
open AutoMapper
open Domains.Applications.Application
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open PersistenceSQLClient.ApplicationData
open App.Common.Transaction
open PersistenceSQLClient.DbConfig
open App.Common.JsonApiResponse

let getAllApplications = fun next ctx ->
    let transaction = createTransactionBuild ctx
    let tres =
        transaction {
            let mapper = ctx.GetService<IMapper>()
            let! models = getAllApplicationsAsync |> TAsync
            return models.Value
                |> Seq.toList
                |> List.map (fun app -> mapper.Map<ApplicationDTO>(app))
                |> Some
        }
    jsonApiWrapHandler tres next ctx

let getApplicationById = fun guid next ctx ->
      let transaction = createTransactionBuild ctx
      let tres =
          transaction {
              let! app = getAllApplicationById guid |> TAsync
              match app with
                | Some a ->
                  let mapper = ctx.GetService<IMapper>()
                  let dto = mapper.Map<ApplicationDTO>(a)
                  return Some dto
                | None -> return None
          }
      jsonApiWrapHandler tres next ctx
          
let createApp = fun next ctx ->
    let transaction = createTransactionBuild ctx
    let tres =
        transaction {
            let! application = ctx.BindJsonAsync<ApplicationDTO>() |> TTIgnore
            let mapper = ctx.GetService<IMapper>()
            let model = mapper.Map<Application>(application.Value)
            return! createApplicationAsync model |> TTask
        }
    jsonApiWrapHandler tres next ctx

let deleteApplication = fun guid next ctx   ->
    let transaction = createTransactionBuild ctx
    let res = transaction {
        let! res = deleteApplicationAsync guid |> TAsync
//        let mapper = ctx.GetService<IMapper>()
//        let! application = ctx.BindJsonAsync<ApplicationDTO>() |> TTIgnore
//        let model = mapper.Map<Application>(application.Value)
//        let! _ = createApplicationAsync model |> TTask
        return res
    }
    
    jsonApiWrapHandler res next ctx

    
let applicationsGetRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> getAllApplications
    routef "/applications/%O" (fun guid -> authorize >=> getApplicationById guid)
    ]

let applicationPostRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> createApp
]
let applicationDeleteRoutes: HttpHandler list = [
    routef "/applications/%O" (fun guid -> authorize >=> (deleteApplication guid))
]