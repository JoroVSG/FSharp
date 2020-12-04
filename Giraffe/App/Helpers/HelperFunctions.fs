module App.Helpers.HelperFunctions

open System
open FSharp.Data
open Giraffe
open App.Common.Exceptions
open Microsoft.AspNetCore.Http

let lowerFirstChar s =
    s
    |> Seq.mapi (fun i c -> match i with | 0 -> (Char.ToLower(c)) | _ -> c)
    |> String.Concat
    
let lower = String.map Char.ToLower
let upper = String.map Char.ToUpper


let isEndOfTheList<'a> index (anyList: 'a list) = index = anyList.Length - 1 
let boolCaseInsensitive s =
    match s with
    | "true" -> true
    | "false" -> false
    | _-> failwith("Error: returns " + s)

let bool = lower >> boolCaseInsensitive
    
let tryGetClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.tryFind (fun claim -> claim.Type = claimType)
let getClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.find (fun claim -> claim.Type = claimType)

let convert<'T> (value: string) : 'T =
  match box Unchecked.defaultof<'T> with
  | :? uint32 -> uint32 value |> unbox<'T>
  | :? uint16 -> uint16 value |> unbox<'T>
  | :? bool -> bool value |> unbox<'T>
  | :? string -> value |> unbox<'T>
  | _ -> failwith "not convertible"

let optional'<'T> (value: obj) : option<'T> =
  match box Unchecked.defaultof<'T> with
  | :? uint32 -> Some (uint32 (string value) |> unbox<'T>)
  | :? uint16 -> Some (uint16 (string value) |> unbox<'T>)
  | :? bool -> Some (bool (string value) |> unbox<'T>)
  | :? string -> Some (value |> unbox<'T>)
  | _ -> None

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