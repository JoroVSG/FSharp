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
    IsFiAdmin: bool
    ActivationStatus: string
}

let getAllUsersAsync =
     async {
       let! res =
           query {
               for user in CLCSPortalContext.Dbo.User do
               select user
           } |> Seq.executeQueryAsync
       let mapped = res |> Seq.map(fun user -> user.MapTo<CLCSUser>())
       return mapped
   }
     
let getAllUsersByInstitutionIdAsync = fun iid ->
     async {
       let! res =
           query {
               for user in CLCSPortalContext.Dbo.User do
               where (user.IdFinancialInstitution = iid)
               select user
           } |> Seq.executeQueryAsync
       let mapped = res |> Seq.map(fun user -> user.MapTo<CLCSUser>())
       return mapped
   }
     
let getUserByEmailAsync = fun email ->
     async {
       let! res =
           query {
                for user in CLCSPortalContext.Dbo.User do
                where (user.Email = email)
                select user
           } |> Seq.headAsync
       return res.MapTo<CLCSUser>()
   }
     
     
