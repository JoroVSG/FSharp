module PersistenceSQLClient.ApplicationData

open System
open System.Data.Common
open System.Data.SqlClient
open System.Data.SqlClient
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
            SELECT IdApplication, Code, Description, Name, Rating, Image FROM dbo.[Application]"""
        , ConnectionString>(connectionString, transaction = trans)
        let! reader = cmd.AsyncExecute()
        return reader
            |> Seq.map(fun a ->
                {
                    Id = a.IdApplication
                    Code = a.Code
                    Description = a.Description
                    Name = a.Name
                    Rating = a.Rating
                    Image = a.Image
                    IdApplication = a.IdApplication }
                )
            |> Seq.toList   
    }
    
let getAllApplicationById = fun (conn: SqlConnection, trans) idApplication ->
   async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[Application] where IdApplication = @idApplication
            """ , ConnectionString, SingleRow=true>(conn, transaction = trans)
        
        let! app = cmd.AsyncExecute(idApplication = idApplication)
        return
            match app with
            | Some a -> Some {
                    Id = a.IdApplication
                    Code = a.Code
                    Description = a.Description
                    Name = a.Name
                    Rating = a.Rating
                    Image = a.Image
                    IdApplication = a.IdApplication }
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
            """ , ConnectionString, ResultType = ResultType.DataReader>(conn, transaction = trans)
        
        let! reader = cmd.AsyncExecute(idUser = idUser)
        return reader |> mapResult<Application>
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

let createApplicationAsync = fun (conn: SqlConnection, trans: SqlTransaction) app ->
    task {
        let guid = Guid.NewGuid()
        let app' = { app with IdApplication = guid }
        let insertCE = insert {
            table "Application"
            value app'
        }
        let! _ = conn.InsertAsync insertCE
        
        return { Id = guid; Success = true; Exception = None }
    }
