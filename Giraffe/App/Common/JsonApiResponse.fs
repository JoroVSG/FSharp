module App.Common.JsonApiResponse

open System.Reflection
open JsonApiSerializer.JsonApi

let jsonApiWrap<'a> = fun (data: 'a)  ->
    let result = DocumentRoot<'a>()
    result.Data <- data
    
    let versionInfo = VersionInfo()
    versionInfo.Version <- Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
    
    result.JsonApi <- versionInfo
    result
