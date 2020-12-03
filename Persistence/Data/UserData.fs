module Persistence.Data.UserData

open System
open System.Linq
open DataContext
open FSharp.Data.Sql
open FSharp.Data.Sql.Common
open Domains.Users.CommonTypes

[<CLIMutable>]
type CLCSUser = {
    [<MappedColumn("IdUser")>]Id: Guid
    ObjectId: Guid
    ActivationKey: string
    IdFinancialInstitution: Guid
    Email: string
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
     
let getUsersByEmailAsync = fun (emails: Email seq) ->
     async {
       let! res =
           query {
                for user in CLCSPortalContext.Dbo.User do
                where (emails.Contains user.Email)
                select user
           } |> Seq.executeQueryAsync
       return res |> Seq.map(fun user -> user.MapTo<CLCSUser>())
   }
     
     
