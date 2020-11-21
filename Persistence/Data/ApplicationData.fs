module Persistence.Data.ApplicationData

open System
open DataContext
open FSharp.Data.Sql
open FSharp.Data.Sql.Common


type Application = {
    [<MappedColumn("IdApplication")>]Id: Guid
    Description: string
    Name: string
    Code: string
    Rating: int
    Image: string
}

let getAllApplicationsAsync =
   async {
       let! res =
           query {
               for application in CLCSPortalContext.Dbo.Application do
               select application
           } |> Seq.executeQueryAsync
       let mapped = res |> Seq.map(fun app -> app.MapTo<Application>())
       return mapped
   }
   
let getAllApplicationByIdAsync = fun (guid: Guid) ->
   async {
       let! res =
           query {
               for application in CLCSPortalContext.Dbo.Application do
                where (application.IdApplication = guid)
                select application
               
           } |> Seq.headAsync
       
       return res.MapTo<Application>()
   }