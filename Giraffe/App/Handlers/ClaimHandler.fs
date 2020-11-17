module App.Handlers.ClaimHandler

open System.Security.Claims
open Giraffe
open Microsoft.AspNetCore.Http
open App.Common.Authentication
    
let showClaims =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let claims = ctx.User.Claims
        let simpleClaims = Seq.map (fun (i : Claim) -> {|Type = i.Type; Value = i.Value|}) claims
        json simpleClaims next ctx
        
let claimGetRoutes: HttpHandler list = [
        route "/claims" >=> authorize' >=> showClaims
    ]
