module PersistenceSQLClient.ApplicationData

open System
open System.Data.SqlClient
open DbConfig
open FSharp.Data
open Domains.Applications.Application
open Domains.Common.CommonTypes
open Dapper.FSharp
open Dapper.FSharp.MSSQL
open FSharp.Control.Tasks.V2
open PersistenceSQLClient.Mapping
        

let getAllApplicationsAsync = fun (connectionString: SqlConnection, trans) ->
    async {
        use cmd = new SqlCommandProvider<"""
            SELECT IdApplication, Code, Description, Name FROM dbo.[Application]"""
        , ConnectionString>(connectionString, transaction = trans)
        let! reader = cmd.AsyncExecute()
        let res = reader
                  |> Seq.map(fun app -> mapToRecord<Application> app)
                  |> Seq.toList
        return res
    }
    
let getAllApplicationById = fun (conn: SqlConnection, trans) idApplication ->
  
   async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[Application] where IdApplication = @idApplication
            """ , ConnectionString, SingleRow=true>(conn, transaction = trans)
        
        let! app = cmd.AsyncExecute(idApplication = idApplication)
        return match app with
                | Some a -> mapToRecord<Application> a |> Some
                | None -> None
   }
   
let getApplicationsByUserIdAsync (conn: SqlConnection, trans) idUser =
    async {
        use cmd =
            new SqlCommandProvider<"""
                SELECT a.* FROM dbo.[Application] as a
                    INNER JOIN dbo.[UserApplication] as ua on ua.IdApplication = a.IdApplication
                    INNER JOIN [User] as u on u.IdUser = ua.IdUser
                    WHERE u.IdUser = @idUser
            """ , ConnectionString>(conn, transaction = trans)
        
        let! reader = cmd.AsyncExecute(idUser = idUser)
        return reader
                  |> Seq.map(fun app -> mapToRecord<Application> app)
                  |> Seq.toList
    }

let deleteApplicationAsync = fun (conn: SqlConnection, trans) idApp ->
    async {
        use cmd =
            new SqlCommandProvider<"""
                DELETE FROM dbo.[Application] where IdApplication = @idApplication
            """ , ConnectionString>(conn, transaction = trans)
        
        let! _ = cmd.AsyncExecute(idApplication = idApp)
        return { Id = Guid.NewGuid(); Success = true; Exception = None }
    }
    
//let createApplicationAsync = fun (conn: SqlConnection, trans: SqlTransaction) (app: Application) ->
//        let guid = Guid.NewGuid()
//        use cmd =
//            new SqlCommandProvider<"""
//                INSERT INTO dbo.[Application](IdApplication, Name, Code, Description)
//                VALUES(@idApplication, @name, @code, @description)
//            """ , ConnectionString, AllParametersOptional = true>(conn, transaction = trans)
//        let _ = cmd.Execute(
//                   idApplication = Some guid,
//                   name = app.Name,
//                   code = app.Code,
//                   // image = app.Image,
//                   description = app.Description
//               )
//        { Id = guid; Success = true; Exception = None }
    

let createApplicationAsync = fun (conn: SqlConnection, trans: SqlTransaction) app ->
    task {
        let guid = Guid.NewGuid()
        let app' = { app with IdApplication = guid }
        let insertCE = insert {
            table "Application"
            value app'
        }
        let! _ = conn.InsertAsync(insertCE, trans)
        
        return { Id = guid; Success = true; Exception = None }
    }
    
    
    
