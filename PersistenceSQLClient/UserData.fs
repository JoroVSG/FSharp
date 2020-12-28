module PersistenceSQLClient.UserData

open System
open System.Data.SqlClient
open Domains.Applications.Application
open Domains.Common.CommonTypes
open Domains.Users.CLCSUser
open Domains.Users.CommonTypes
open FSharp.Data
open DbConfig
open PersistenceSQLClient.Mapping
open FSharp.Control.Tasks.V2.ContextInsensitive
open Dapper.FSharp
open Dapper.FSharp.MSSQL

let getAllUsersAsync = fun (connectionString: SqlConnection, trans) ->
     async {
        use cmd = new SqlCommandProvider<"""
            SELECT * FROM dbo.[User]"""
        , ConnectionString>(connectionString, transaction = trans)
        let! reader = cmd.AsyncExecute()
        let res = reader
                  |> Seq.map(fun app -> mapToRecord<CLCSUser> app)
                  |> Seq.toList
        return res
    }
let getUserByEmailAsync = fun email (connectionString: SqlConnection, trans) ->
     async {
        use cmd = new SqlCommandProvider<"""
            SELECT * FROM dbo.[User] where email = @email"""
        , ConnectionString, SingleRow=true>(connectionString, transaction = trans)
        let! user = cmd.AsyncExecute(email = email)
        return match user with
                | Some a -> mapToRecord<CLCSUser> a |> Some
                | None -> None
        
    }
     
let getUserApplicationsByEmail = fun email (connectionString: SqlConnection, trans) ->
     async {
        use cmd = new SqlCommandProvider<"""
            SELECT a.* FROM dbo.[UserApplication] as ua
                INNER JOIN dbo.[Application] as a on a.IdApplication = ua.IdApplication
                INNER JOIN dbo.[User] as u on u.IdUser = ua.IdUser
            WHERE u.Email = @email"""
        , ConnectionString>(connectionString, transaction = trans)
        let! apps = cmd.AsyncExecute(email = email)
        return apps
               |> Seq.map mapToRecord<Application>
               |> Seq.toList
        
    }
     
let getAllUsersByInstitutionIdAsync = fun (connectionString: SqlConnection, trans) iid ->
     async {
        use cmd = new SqlCommandProvider<"""
            SELECT * FROM dbo.[User] where IdFinancialInstitution = @idFinancialInstitution"""
        , ConnectionString>(connectionString, transaction = trans)
        let! reader = cmd.AsyncExecute(idFinancialInstitution = iid)
        let res = reader
                  |> Seq.map mapToRecord<CLCSUser>
                  |> Seq.toList
        return res
    }
let getUsersByEmailAsync = fun (emails: Email seq) (payload: TransactionPayload) ->
     let (connectionString, trans) = payload
     async {
        let inClause = emails |> String.concat " ,"
        use cmd = new SqlCommandProvider<"""
            DECLARE @str nvarchar(1000), @delimiter varchar(10)
            SET @str = @emails
            SET @delimiter = ','
            ;WITH cte AS
            (
                SELECT 0 a, 1 b
                UNION ALL
                SELECT b, CHARINDEX(@delimiter, @str, b) + LEN(@delimiter)
                FROM CTE
                WHERE b > a
            )
                  
            SELECT * FROM dbo.[User] WHERE Email IN (
                SELECT SUBSTRING(@str, a,
                CASE WHEN b > LEN(@delimiter) 
                THEN b - a - LEN(@delimiter) 
                ELSE LEN(@str) - a + 1 END) value
                FROM cte WHERE a > 0
            )
            """
        , ConnectionString>(connectionString, transaction = trans)
        let! reader = cmd.AsyncExecute(emails = inClause)
        let res = reader
                  |> Seq.map(fun app -> mapToRecord<CLCSUser> app)
                  |> Seq.toList
        
        return res 
        //|> ResultSuccess
    }
let createUserAsync = fun (user: CLCSUser) (payload: TransactionPayload) ->
    let (conn, trans) = payload
    task {
        let insertCE = insert {
            table "User"
            value user
        }
        let! _ = conn.InsertAsync(insertCE, trans)
        return { Id = user.IdUser; Success = true; Exception = None } |> Ok
            
    }
let updateUserAsync = fun (user: CLCSUser) (payload: TransactionPayload) ->
    let (conn, trans) = payload
    task {
        let updateCE = update {
            table "User"
            set user
            where (eq "IdUser" user.IdUser)
        }
        let! _ = conn.UpdateAsync(updateCE, trans)
        return { Id = user.IdUser; Success = true; Exception = None } |> Ok
            
    }

let insertUserApplication = fun idApplication idUser payload ->
    async {
        let id = Guid.NewGuid();
        let (conn, trans) = payload
        use cmd = new SqlCommandProvider<"""
            INSERT INTO UserApplication(IdUserApplication, IdApplication, IdUser)
            VALUES(@idUserApplication, @idApplication, @idUser)"""
        , ConnectionString>(conn, transaction = trans)
        let! _ =
            cmd.AsyncExecute(
                idApplication = idApplication,
                idUser = idUser,
                idUserApplication = id
            )
        return { Id = id; Success = true; Exception = None } |> Ok
    }
   