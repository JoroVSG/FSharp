module PersistenceSQLClient.FiData

open System.Data.SqlClient
open PersistenceSQLClient.DbConfig
open FSharp.Data

let getFiByInstitutionId = fun (con: SqlConnection) iid ->
    async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[FinancialInstitution] where IdFinancialInstitution  = @iid
            """ , ConnectionString, SingleRow=true>(con)
        
        let! fi = cmd.AsyncExecute(iid = iid)
        return fi
    }