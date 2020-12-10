module PersistenceSQLClient.Mapping

open System
open System.Data.SqlClient
open Domains.Common.CommonTypes
open FSharp.Data.SqlClient
open Microsoft.FSharp.Reflection
open System.Reflection
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

let mapToRecord<'a> = fun (record: obj) ->
    let recFields = FSharpType.GetRecordFields(typeof<'a>)
    
    let tt = record :?> DynamicRecord
    let dynamicMembers = tt.GetDynamicMemberNames()
    
    let arr =
        recFields
            |> Array.map(fun pf ->
                let r = dynamicMembers |> Seq.tryFind(fun mem -> mem = pf.Name)
                
                if r.IsSome && not (isNull tt.[pf.Name])
                then tt.[pf.Name]
                else
                    let cases = FSharpType.GetUnionCases(pf.PropertyType)
                    FSharpValue.MakeUnion(cases.[0], [||])
            )
    
    FSharpValue.MakeRecord(typeof<'a>, arr) :?> 'a

type SqlDataReader with
    member this.ToRecords<'T>() =
        let recFields = FSharpType.GetRecordFields(typeof<'T>)
        [while this.Read() do
            yield (recFields |> Array.map (fun f ->
                let value = unbox (this.[f.Name])
                let piType= f.PropertyType
                let isDbNull = Convert.IsDBNull(value)
                if isOption f then
                    let cases = FSharpType.GetUnionCases(piType)
                    let maybe = if (isNull value || isDbNull)  then FSharpValue.MakeUnion(cases.[0], [||])
                                else FSharpValue.MakeUnion(cases.[1], [|value|])
                    maybe
                else value
                
        ))]
        |> List.map (fun oArray -> FSharpValue.MakeRecord(typeof<'T>, oArray) :?> 'T)