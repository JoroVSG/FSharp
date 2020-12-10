module PersistenceSQLClient.UserData

open System.Data.SqlClient
open Domains.Users
open Domains.Users.CLCSUser
open Domains.Users.CommonTypes
open FSharp.Data
open DbConfig
open PersistenceSQLClient.Mapping


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
     
let getAllUsersByInstitutionIdAsync = fun (connectionString: SqlConnection, trans) iid ->
     async {
        use cmd = new SqlCommandProvider<"""
            SELECT * FROM dbo.[User] where IdFinancialInstitution = @idFinancialInstitution"""
        , ConnectionString>(connectionString, transaction = trans)
        let! reader = cmd.AsyncExecute(idFinancialInstitution = iid)
        let res = reader
                  |> Seq.map(fun app -> mapToRecord<CLCSUser> app)
                  |> Seq.toList
        return res
    }
let getUsersByEmailAsync = fun (connectionString: SqlConnection, trans) (emails: Email seq) ->
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

//        return reader
//            |> Seq.map(fun user ->
//                let u: CLCSUser = { IdUser = user.IdUser
//                                    ObjectId = user.ObjectId
//                                    ActivationKey = user.ActivationKey
//                                    IdFinancialInstitution = user.IdFinancialInstitution
//                                    Email = user.Email
//                                    ActivationStatus = user.ActivationStatus
//                                    IsFiAdmin = user.IsFiAdmin }
//                u
//            )
//            |> Seq.toList
    }
   