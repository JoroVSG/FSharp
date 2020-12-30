module PersistenceSQLClient.ApplicationData

open System
open DbConfig
open FSharp.Data
open Domains.Applications.Application
open Domains.Common.CommonTypes
open Dapper.FSharp
open Dapper.FSharp.MSSQL
open FSharp.Control.Tasks.V2
open PersistenceSQLClient.Mapping
        

let getAllApplicationsAsync = fun payload ->
    let (conn, trans) = payload
    async {
        use cmd = new SqlCommandProvider<"""
            SELECT IdApplication, Code, Description, Name FROM dbo.[Application]"""
        , ConnectionString>(conn, transaction = trans)
        let! reader = cmd.AsyncExecute()
        let res = reader
                  |> Seq.map mapToRecord<Application>
                  |> Seq.toList
        return res
    }
    
let getAllApplicationById = fun idApplication payload ->
   let (conn, trans) = payload
   async {
        use cmd =
            new SqlCommandProvider<"""
                SELECT * FROM dbo.[Application] WHERE IdApplication = @idApplication
            """ , ConnectionString, SingleRow=true>(conn, transaction = trans)
        
        let! app = cmd.AsyncExecute(idApplication = idApplication)
        return app |> Option.map mapToRecord<Application>
   }
   
let getApplicationsByUserIdAsync idUser payload =
    let (conn, trans) = payload
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
                  |> Seq.map mapToRecord<Application>
                  |> Seq.toList
    }

let deleteApplicationAsync = fun idApp payload ->
    let (conn, trans) = payload
    async {
        use cmd =
            new SqlCommandProvider<"""
                DELETE FROM dbo.[Application] where IdApplication = @idApplication
            """ , ConnectionString>(conn, transaction = trans)
        
        let! rowsAffected = cmd.AsyncExecute(idApplication = idApp)
        return
            if rowsAffected = 1
            then Some { Id = Guid.NewGuid(); Success = true; Exception = None }
            else None
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
    

let createApplicationAsync = fun app (payload: TransactionPayload) ->
    let (conn, trans) = payload
    task {
        let guid = Guid.NewGuid()
        let app' = { app with IdApplication = guid }
        let insertCE = insert {
            table "Application"
            value app'
        }
        let! rowsAffected = conn.InsertAsync(insertCE, trans)
        return
            if rowsAffected = 1
            then Some { Id = guid; Success = true; Exception = None }
            else None
                        
    }
    
    
    
