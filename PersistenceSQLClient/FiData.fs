module PersistenceSQLClient.FiData

open System.Data.SqlClient
open Domains.FIs.FinancialInstitution
open PersistenceSQLClient.DbConfig
open FSharp.Data
open PersistenceSQLClient.Mapping

let getFiByInstitutionId = fun (con: SqlConnection, trans) iid ->
    async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[FinancialInstitution] where InstitutionId  = @iid
            """ , ConnectionString, SingleRow=true>(con, transaction=trans)
        
        let! fi = cmd.AsyncExecute(iid = iid)
        return
            match fi with
                | Some f -> mapToRecord<FI> f |> Some
                | None -> None
    }    