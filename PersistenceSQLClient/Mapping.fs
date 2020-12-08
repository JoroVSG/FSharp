module PersistenceSQLClient.Mapping

open System
open System.Data.SqlClient
open Domains.Common.CommonTypes
open FSharp.Data.SqlClient
open Microsoft.FSharp.Reflection
open System.Reflection
open Dapper
let mapResult<'a> = fun (reader: SqlDataReader) ->
    let recFields = FSharpType.GetRecordFields(typeof<'a>)
    let fields =
        [while reader.Read() do yield (recFields |> Array.map (fun f ->
            let c = f.GetCustomAttributes()
            let t = c |> Seq.tryFind(fun cc -> cc.GetType() = typeof<MapColumn>)
            match t with
            | Some attr ->
                let mapTo = attr :?> MapColumn
                box (reader.[mapTo.FieldName])
            | None -> unbox (reader.[f.Name])    
        ))]
        |> List.map(fun props -> FSharpValue.MakeRecord(typeof<'a>, props))
        
    fields
        |> Seq.ofList
        |> Seq.map (fun o -> o :?> 'a)

//let mapRecordToRecord<'a, 'b> = fun (source: 'a) ->
//    
//    let sourceFields =
//        FSharpType.GetRecordFields(typeof<'a>)
//            |> Seq.map(fun f ->
//                    let c = f.GetCustomAttributes()
//                    let t = c |> Seq.tryFind(fun cc -> cc.GetType() = typeof<MapColumn>)
//                    match t with
//                    | Some attr ->
//                        let mapTo = attr :?> MapColumn
//                        box (reader.[mapTo.FieldName])
//                    | None -> unbox (reader.[f.Name])    
//                )
//    let destination = Activator.CreateInstance(typeof<'b>, sourceFields) :?> 'b
//    destination
        
let isOption (p:PropertyInfo) = 
    p.PropertyType.IsGenericType &&
    p.PropertyType.GetGenericTypeDefinition() = typedefof<Option<_>>

let mapping<'a> = fun (reader: SqlDataReader) -> 
    let recFields = FSharpType.GetRecordFields(typeof<'a>)
    [while reader.Read() do yield (recFields |> Array.map (fun f ->
        let value = unbox (reader.[f.Name])
        let piType= f.PropertyType
        let isDbNull = Convert.IsDBNull(value)
        if isOption f then
            let cases = FSharpType.GetUnionCases(piType)
            let maybe = if (isNull value || isDbNull)  then FSharpValue.MakeUnion(cases.[0], [||])
                        else FSharpValue.MakeUnion(cases.[1], [|value|])
            maybe
        else value
        
        ))]  
    |> List.map (fun oArray -> FSharpValue.MakeRecord(typeof<'a>, oArray))
    |> Seq.ofList |> Seq.map (fun o -> o :?> 'a)
 