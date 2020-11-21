module App.Handlers.GreetHandler

open Giraffe
open Microsoft.AspNetCore.Http
open App.Common.Authentication
 
let greet =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let claim = ctx.User.FindFirst "name"
        let name = claim.Value
        text ("Hello " + name) next ctx


let greetGetRoutes: HttpHandler list = [
    route "/greet" >=> authorize >=> greet
]
    

let greetPostRoutes: HttpHandler list = [];