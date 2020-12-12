module App.Common.JsonApiResponse

open System.Collections.Generic
open System.Reflection
open AutoMapper
open JsonApiSerializer.JsonApi
open Microsoft.AspNetCore.Http
open Giraffe
open App.Common.Exceptions
open PersistenceSQLClient.DbConfig


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
          | Error ex  -> handleErrorJsonAPI ex next ctx
