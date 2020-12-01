module App.Helpers.HelperFunctions

open System
open FSharp.Data
open Giraffe
open App.Common.Exceptions
open Microsoft.AspNetCore.Http

let lower (s: string) =
    s
    |> Seq.mapi (fun i c -> match i with | 0 -> (Char.ToLower(c)) | _ -> c)
    |> String.Concat

let boolIgnoreCase s =
    match s with
    | "true" -> true
    | "false" -> false
    | _-> failwith("Error: returns " + s)

let bool = lower >> boolIgnoreCase
    
let tryGetClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.tryFind (fun claim -> claim.Type = claimType)
let getClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.find (fun claim -> claim.Type = claimType)

let convert<'T> (value: string) : 'T =
  match box Unchecked.defaultof<'T> with
  | :? uint32 -> uint32 value |> unbox<'T>
  | :? uint16 -> uint16 value |> unbox<'T>
  | :? bool -> bool value |> unbox<'T>
  | :? string -> string value |> unbox<'T>
  | _ -> failwith "not convertible"

let getClaimValue<'T> = fun ctx claimName ->
    let claim = tryGetClaim claimName ctx
    match claim with
    | Some claimValue -> convert claimValue.Value
    | None -> Unchecked.defaultof<'T>
 
   
let createResponse = fun status message ->
    setStatusCode status >=> (json <| createJsonApiError message status)

type ErrorMessage = string
let notFound: (ErrorMessage -> HttpHandler) = createResponse HttpStatusCodes.NotFound
let forbidden: (ErrorMessage -> HttpHandler) = createResponse HttpStatusCodes.Forbidden