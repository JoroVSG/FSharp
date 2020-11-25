module Persistence.Data.UserData

open System
open DataContext
open FSharp.Data.Sql
open FSharp.Data.Sql.Common

[<CLIMutable>]
type CLCSUser = {
    [<MappedColumn("IdUser")>]Id: Guid
    ObjectId: Guid
    ActivationKey: string
    IdFinancialInstitution: Guid
    Email: string
    ActivationStatus: bool
}

let getAllUsersAsync =
     async {
       let! res =
           query {
               for user in CLCSPortalContext.Dbo.User do
               select user
           } |> Seq.executeQueryAsync
       let mapped = res |> Seq.map(fun app -> app.MapTo<CLCSUser>())
       return mapped
   }
     
     
