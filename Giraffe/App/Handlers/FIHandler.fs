module App.Handlers.FIHandler

open App.DTOs.FiDTO
open AutoMapper
open Giraffe
open App.Common.Authentication
open App.Common.JsonApiResponse
open App.Common.Transaction
open Microsoft.AspNetCore.Http
open PersistenceSQLClient.FiData
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions
open FSharp.Control.Tasks.V2.ContextInsensitive

let getFs = fun payload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! res = getFis payload
        return res
            |> List.map(fun r -> mapper.Map<FiDTO>(r))
            |> Ok
    }

let getFiById = fun iid payload _ ->
    task {
        let! fi = getFiById iid payload
        return fi |> resultOrNotFound
    }


let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"

let fiGetRoutes: HttpHandler list = [
    route "/fi" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction getFs
    routef "/fi/%O" (fun guid -> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction (getFiById guid))
]