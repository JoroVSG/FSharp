module App.Handlers.ApplicationHandler
open System
open App.Common
open App.DTOs.ApplicationDTO
open AutoMapper
open AutoMapper
open Domains.Applications.Application
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Authentication
open Microsoft.Extensions.Configuration
open PersistenceSQLClient.ApplicationData
open App.Common.Transaction
open PersistenceSQLClient.DbConfig
open App.Common.Exceptions
open App.Common.JsonApiResponse

let getAllApplications = fun transPayload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! models = getAllApplicationsAsync transPayload
        return models
            |> Seq.toList
            |> List.map (fun app -> mapper.Map<ApplicationDTO>(app))
    }

let divide j (x: TransactionPayload) =
//    try
//        let res = j / j
//        res |> Success
//    with ex -> Error ex
       async {
            let res = j / j
            return res |> Success
       }


//let getApplicationById = fun guid transPayload (ctx: HttpContext) ->
//      task {
//          let! model = getAllApplicationById transPayload guid
//          return
//            match model with
//                | Some m ->
//                    let mapper = ctx.GetService<IMapper>()
//                    mapper.Map<ApplicationDTO>(m) |> Some
//                | None -> None
//      }

let getApplicationById = fun (guid: Guid) (next: HttpFunc) (ctx: HttpContext) ->
          let transaction = createTransactionBuild ctx
          let tResult =
                transaction {
                  return! getAllApplicationById guid
                }

          let mappedValueFunc = fun (mapper: IMapper) app -> mapper.Map<ApplicationDTO>(app);
          jsonApiWrapHandlerWithMapper tResult mappedValueFunc next ctx
      
      
   
    
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
    //routef "/applications/%O" (fun guid -> authorize >=> transaction (getApplicationById guid))
    routef "/applications/%O" (fun guid -> authorize >=> getApplicationById guid)
    ]

let applicationPostRoutes: HttpHandler list = [
    route "/applications" >=> authorize >=> transaction createApp
]

let applicationDeleteRoutes: HttpHandler list = [
    routef "/applications/%O" (fun guid -> authorize >=> transaction (deleteApplication guid))
]