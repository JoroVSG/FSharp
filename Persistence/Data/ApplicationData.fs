module Persistence.Data.ApplicationData

open System
open DataContext
open FSharp.Data.Sql
open FSharp.Data.Sql.Common

[<CLIMutable>]
type Application = {
    [<MappedColumn("IdApplication")>]Id: Guid
    Description: string
    Name: string
    Code: string
    Rating: int
    Image: byte[]
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
   
let applications = CLCSPortalContext.Dbo.Application
let createApplication = fun (app: Application) ->
    async {
        let row = applications.Create()
        row.Description <- app.Description
        row.Name <- app.Name
        row.IdApplication <- app.Id
        row.Rating <- app.Rating
        row.Image <- app.Image
        do! CLCSPortalContext.SubmitUpdatesAsync()
        return row.MapTo<Application>()
    }
   