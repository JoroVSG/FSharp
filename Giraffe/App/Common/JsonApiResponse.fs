module App.Common.JsonApiResponse

open System.Reflection
open JsonApiSerializer
open JsonApiSerializer.JsonApi
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open App.Common.Converters

let jsonApiWrap<'a> = fun (data: 'a)  ->
    let result = DocumentRoot<'a>()
    
//    
//    let x = box data
//    
//    let d = match x with
//        | :? System.Collections.IEnumerable as l ->
//            let settings = JsonApiSerializerSettings()
//            settings.ContractResolver <- CamelCasePropertyNamesContractResolver()
//            settings.Converters.Add(OptionConverter())
//            let z = l |> Seq.cast<obj> |> Seq.map(fun y -> JsonConvert.SerializeObject(y, settings))
//            z
//        | _ -> failwith "doesn't match"   
    
    result.Data <- data
    let versionInfo = VersionInfo()
    versionInfo.Version <- Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
    
    result.JsonApi <- versionInfo
    result
