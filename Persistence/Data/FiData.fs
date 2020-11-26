module Persistence.Data.FiData

open System
open DataContext
open FSharp.Data.Sql

[<CLIMutable>]
type FI = {
    IdFinancialInstitution: Guid
    Name: string
    ObjectId: Guid
    Description: string
    EmailSendingInviteFrom: string
}

let getFiByInstitutionId = fun iid ->
   async {
       let! res =
           query {
               for fi in CLCSPortalContext.Dbo.FinancialInstitution do
                where (fi.InstitutionId = iid)
                select fi
               
           } |> Seq.headAsync
       
       return res.MapTo<FI>()
   }