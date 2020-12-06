module Persistence.Data.FiData

open System
open Persistence.DataContext
open FSharp.Data.Sql

[<CLIMutable>]
type FI = {
    IdFinancialInstitution: Guid
    Name: string
    ObjectId: Guid
    Description: string
    InstitutionId: string
    EmailSendingInviteFrom: string
}

let getFiByInstitutionId = fun iid ->
   async {
       let! res =
           query {
               for fi in CLCSPortalContext.Dbo.FinancialInstitution do
                where (fi.InstitutionId.Value = iid)
                select fi
               
           } |> Seq.tryHeadAsync
      return
          match res with
          | Some ins -> Some (ins.MapTo<FI>())
          | None -> None
   }