module App.Handlers.FIHandler

open Giraffe
open App.Common.Authentication
open App.Common.JsonApiResponse
open App.Common.Transaction
open PersistenceSQLClient.DbConfig
open PersistenceSQLClient.FiData
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions

let getFiById = fun iid next ctx ->
    let transaction = createTransactionBuild ctx
    let tres = transaction {
        let! fi = getFiByInstitutionId iid |> TAsync
        return fi
    }
    jsonApiWrapHandler tres next ctx


let getFs = fun next ctx ->
    let transaction = createTransactionBuild ctx
    let tres = transaction {
        return! getFis |> TAsync
    }
    jsonApiWrapHandler tres next ctx


let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"


let fiGetRoutes: HttpHandler list = [
    route "/fi" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getFs
    routef "/fi/%O" (fun guid -> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> getFiById guid)
]