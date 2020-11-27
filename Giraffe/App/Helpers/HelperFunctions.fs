module App.Helpers.HelperFunctions

open System
open FSharp.Data
open Giraffe
open App.Common.Exceptions
open Microsoft.AspNetCore.Http

let lower (s: string) =
    s |> Seq.mapi (fun i c -> match i with | 0 -> (Char.ToLower(c)) | _ -> c)  |> String.Concat

let bool s =
    match lower s with
    | "true" -> true
    | "false" -> false
    | _-> failwith("Error: returns " + s)
    
let tryGetClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.tryFind (fun claim -> claim.Type = claimType)
let getClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.find (fun claim -> claim.Type = claimType)

let createResponse = fun status message ->
    setStatusCode status >=> json (createJsonApiError message status)

let notFound: (string -> HttpHandler) = createResponse HttpStatusCodes.NotFound
let forbidden: (string -> HttpHandler) = createResponse HttpStatusCodes.Forbidden