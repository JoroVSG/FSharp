module Persistence.Data.ApplicationData

open System
open Persistence.DataContext
open FSharp.Data.Sql
open Domains.Applications.Application
open Domains.Common.CommonTypes

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
   
let getApplicationsByUserId idUser =
   async {
       let! res =
           query {
               for application in CLCSPortalContext.Dbo.Application do
               join userApp in CLCSPortalContext.Dbo.UserApplication on (application.IdApplication = userApp.IdApplication.Value)
               join user in CLCSPortalContext.Dbo.User on (userApp.IdUser.Value = user.IdUser)
               where (user.IdUser = idUser)         
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
        row.Code <- app.Code
        row.IdApplication <- Guid.NewGuid()
        row.Rating <- app.Rating
        
        do! CLCSPortalContext.SubmitUpdatesAsync()
        return! getAllApplicationByIdAsync row.IdApplication 
    }
    
let deleteApplication = fun (idApplication: Guid) ->
    async {
        let! _ =
            query {
                for app in CLCSPortalContext.Dbo.Application do
                where (app.IdApplication = idApplication)
                select app
            } |> Seq.``delete all items from single table``
        
        let operationStatus = { Id = Guid.NewGuid(); Success = true; Exception = None }
        return operationStatus
    }
   