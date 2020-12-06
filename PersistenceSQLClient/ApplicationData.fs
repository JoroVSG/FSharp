module PersistenceSQLClient.ApplicationData

open System
open System.Data.SqlClient
open System.Reflection
open DbConfig
open FSharp.Data
open Domains.Applications.Application
open Domains.Common.CommonTypes
open Dapper.FSharp
open Dapper.FSharp.MSSQL
open FSharp.Control.Tasks.V2
open Domains.Common.CommonTypes
open Microsoft.FSharp.Reflection


let mapResult<'a> = fun (reader: SqlDataReader) ->
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
    
        
let getAllApplications = fun (connectionString: string) ->
    async {
        use cmd = new SqlCommandProvider<"""
            SELECT IdApplication, Code, Description, Name, Rating, Image FROM dbo.[Application]"""
        , ConnectionString, ResultType=ResultType.DataReader>(connectionString)
        let! reader = cmd.AsyncExecute()
        return reader |> mapResult<Application>
               
    }
let getAllApplicationsAsync = getAllApplications connectionString
    
let getAllApplicationById = fun (conn: string) idApplication ->
   async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[Application] where IdApplication = @idApplication
            """ , ConnectionString, SingleRow=true>(conn)
        
        let! app = cmd.AsyncExecute(idApplication = idApplication)
        return
            match app with
            | Some a -> Some {
                    Id = Some a.IdApplication
                    Code = a.Code
                    Description = a.Description
                    Name = a.Name
                    Rating = a.Rating
                    Image = a.Image
                    IdApplication = a.IdApplication }
            | None -> None
   }
   
let getApplicationsByUserId (conn: string) idUser =
    async {
        use cmd =
            new SqlCommandProvider<"""
                SELECT a.* FROM dbo.[Application] as a
                    INNER JOIN dbo.[UserApplication] as ua on ua.IdApplication = a.IdApplication
                    INNER JOIN [User] as u on u.IdUser = ua.IdUser
                    WHERE u.IdUser = @idUser
            """ , ConnectionString, ResultType = ResultType.DataReader>(conn)
        
        let! reader = cmd.AsyncExecute(idUser = idUser)
        return reader |> mapResult<Application>
    }
let getApplicationsByUserIdAsync = getApplicationsByUserId connectionString


let deleteApplication = fun (conn: string) idApp ->
    async {
        use cmd =
            new SqlCommandProvider<"""
                DELETE FROM dbo.[Application] where IdApplication = @idApplication
            """ , ConnectionString>(conn)
        
        let! _ = cmd.AsyncExecute(idApplication = idApp)
        return { Id = Guid.NewGuid(); Success = true; Exception = None }
    }

let deleteApplicationAsync = deleteApplication connectionString

let createApplication = fun conn app ->
    task {
        use cStr = new SqlConnection(conn)
        do! cStr.OpenAsync()
        let guid = Guid.NewGuid()

        let app' = { app with IdApplication = guid }
        let insertCE = insert {
            table "Application"
            value app'
        }
        let! _ = cStr.InsertAsync insertCE
        
        return { Id = guid; Success = true; Exception = None }
    }
    
    
let createApplicationAsync = createApplication connectionString