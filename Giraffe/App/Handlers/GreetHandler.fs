module App.Handlers.GreetHandler

open Giraffe
open Microsoft.AspNetCore.Http
open App.Common.Authentication
open Persistence.Data.AnimalData
open FSharp.Control.Tasks.V2
 
let greet =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let claim = ctx.User.FindFirst "name"
        let name = claim.Value
        text ("Hello " + name) next ctx

let animalsAsyncHandler =
   fun (next : HttpFunc) (ctx : HttpContext) ->
       task {
           let! res = animalAsync
           return! json res next ctx
       }


let greetGetRoutes: HttpHandler list = [
    route "/greet" >=> authorize'' >=> greet
    route "/animals" >=> authorize'' >=> animalsAsyncHandler
]
    

let greetPostRoutes: HttpHandler list = [];