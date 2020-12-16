module App.Common.Exceptions

open System
open System.Collections.Generic
open System.Data
open JsonApiSerializer
open JsonApiSerializer.JsonApi
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Server.IIS
open Microsoft.Data.SqlClient
open Newtonsoft.Json
type TransactionException = PersistenceSQLClient.DbConfig.TransactionException

type RestException(code, message) =
    inherit Exception(message)
    member __.Code = code 

let createJsonApiError = fun message code ->
    let error = Error()
    error.Detail <- message
    error.Code <- string code
    
    let errors = [error]
    
    let root = DocumentRoot<obj>()
    root.Errors <- ResizeArray<Error> errors
    root

let handleErrorJsonAPI = fun (ex: Exception) _ (ctx: HttpContext) ->
    task {
        let (code, message) =
             match ex with
                | :? InvalidOperationException -> (StatusCodes.Status404NotFound, ex.Message)
                | :? KeyNotFoundException -> (StatusCodes.Status404NotFound, "")
                | :? TransactionException ->
                    let transExp = ex :?> TransactionException
                    (transExp.Code :?> int, ex.Message)
                | :? RestException ->
                    let restEx = ex :?> RestException
                    let  message = if restEx.Code = StatusCodes.Status404NotFound then "The resource was not found." else restEx.Message
                    (restEx.Code, message)
                | :? UnauthorizedAccessException -> (StatusCodes.Status401Unauthorized, "")
                // | :? InvalidModelStateException  -> (StatusCodes.Status422UnprocessableEntity, "")
                | :? BadHttpRequestException -> (StatusCodes.Status400BadRequest, "")
                | :? ArgumentException -> (StatusCodes.Status400BadRequest, ex.Message)
                | :? DBConcurrencyException -> (StatusCodes.Status409Conflict, "This record has already been updated. Please try again.")
                | :? SqlException  -> (StatusCodes.Status409Conflict, ex.Message)
                | _ -> (StatusCodes.Status500InternalServerError, "Internal server error")
        
        let root = createJsonApiError message code
        
        let result = JsonConvert.SerializeObject(root, JsonApiSerializerSettings())
        ctx.SetStatusCode code
        do! ctx.Response.WriteAsync(result)
        
        return! earlyReturn ctx
    }
  