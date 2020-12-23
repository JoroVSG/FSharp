module App.Common.JsonApiResponse

open System.Reflection
open JsonApiSerializer.JsonApi
open Microsoft.AspNetCore.Http
open Giraffe
open App.Common.Exceptions
open PersistenceSQLClient.DbConfig
open System


let jsonApiWrap<'a> = fun (data: 'a)  ->
    let result = DocumentRoot<'a>()
    
    result.Data <- data
    let versionInfo = VersionInfo()
    versionInfo.Version <- Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
    
    result.JsonApi <- versionInfo
    result

let jsonApiWrapHandler = fun result (next: HttpFunc) (ctx: HttpContext) ->
    match result with
          | Success a ->
              match a with
              | Some value -> json (jsonApiWrap value) next ctx
              | None ->
                  let notFound = RestException(StatusCodes.Status404NotFound, "")
                  handleErrorJsonAPI notFound  next ctx
          | Error' ex  -> handleErrorJsonAPI ex next ctx

let mapResultOption = fun f result ex ->
    match result with
        | Some m -> f m |> Ok
        | None -> ex |> Result.Error

let resultOptionNoMap result ex = mapResultOption id result ex
let resultOption result f ex = mapResultOption f result ex

let resultOrNotFound = fun result ->
    let notFound = RestException(StatusCodes.Status404NotFound, "") :> Exception
    resultOptionNoMap result notFound

let mapResultOrNotFound = fun result f ->
    let notFound = RestException(StatusCodes.Status404NotFound, "") :> Exception
    resultOption result f notFound

let jsonApiWrapHandler' = fun result (next: HttpFunc) (ctx: HttpContext) ->
    match result with
          | Ok a -> json (jsonApiWrap a) next ctx
          | Error ex -> handleErrorJsonAPI ex next ctx

