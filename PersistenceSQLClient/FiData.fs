module PersistenceSQLClient.FiData

open Domains.FIs.FinancialInstitution
open PersistenceSQLClient.DbConfig
open FSharp.Data
open PersistenceSQLClient.Mapping

let getFiByInstitutionId = fun iid payload ->
    let (con, trans) = payload
    async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[FinancialInstitution] where InstitutionId  = @iid
            """ , ConnectionString, SingleRow=true>(con, transaction=trans)
        
        let! fi = cmd.AsyncExecute(iid = iid)
        return fi |> Option.map mapToRecord<FI>
    }
    
let getFiById = fun iid payload ->
    let (con, trans) = payload
    async {
        use cmd =
            new SqlCommandProvider<"""
                select * from dbo.[FinancialInstitution] where IdFinancialInstitution  = @id
            """ , ConnectionString, SingleRow=true>(con, transaction=trans)
        
        let! fi = cmd.AsyncExecute(id = iid)
        return fi |> Option.map mapToRecord<FI>
    }


let getFis = fun payload ->
    let (con, trans) = payload
    async {
        use cmd = new SqlCommandProvider<"select * from dbo.[FinancialInstitution]" , ConnectionString>(con, transaction=trans)
        
        let! fi = cmd.AsyncExecute()
        let res = fi
                  |> Seq.map mapToRecord<FI>
                  |> Seq.toList
        
        return res
    }