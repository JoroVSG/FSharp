module PersistenceSQLClient.Mapping

open System.Data.SqlClient
open Domains.Common.CommonTypes
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



//    let recFields = typeof<'a>.GetMembers() |> Array.filter (fun (f:MemberInfo) -> f.MemberType.ToString() = "Property")
//    [while reader.Read() do yield (recFields |> Array.map (fun (f:MemberInfo) ->
//            let c = f.GetCustomAttributes()
//            let t = c |> Seq.tryFind(fun cc -> cc.GetType() = typeof<MapColumn>)
//            match t with
//            | Some attr ->
//                let mapTo = attr :?> MapColumn
//                box (reader.[mapTo.FieldName])
//            | None -> unbox (reader.[f.Name])    
//        ))] 
//        |> List.map (fun oArray -> Activator.CreateInstance(typeof<'a>, oArray))
//        |> Seq.ofList |> Seq.map (fun o -> o :?> 'a)    