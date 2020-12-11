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
    
let jsonApiWrapHandlerWithMapper<'a, 'b> = fun result (mapping: IMapper -> 'a -> 'b) (next: HttpFunc) (ctx: HttpContext) ->
    let mapper = ctx.GetService<IMapper>()
    match result with
          | Success a -> json (jsonApiWrap (mapping mapper a)) next ctx
          | Error ex  -> handleErrorJsonAPI ex next ctx
          | NotFound  -> raise <| RestException(StatusCodes.Status404NotFound, "")
