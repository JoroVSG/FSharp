module App.Helpers.HelperFunctions

open System
open System.Text
open AutoMapper
open FSharp.Data
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive

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
    | _-> failwith("String -> bool convert error: returns " + s)

let bool = lower >> boolCaseInsensitive

let wrap (a: Async<'a>) = task { return! a }

let tryGetClaimWithPredicate = fun predicate (ctx: HttpContext) -> ctx.User.Claims |> Seq.tryFind predicate 
let tryGetClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.tryFind (fun claim -> claim.Type = claimType)
let getClaim = fun claimType (ctx: HttpContext) -> ctx.User.Claims |> Seq.find (fun claim -> claim.Type = claimType)

let mapOption<'a, 'b> = fun (a: Option<'a>) (ctx: HttpContext) ->
    let mapper = ctx.GetService<IMapper>()
    match a with
         | Some v -> mapper.Map<'b>(v) |> Some
         | None -> None

let convert<'T> (value: string) : 'T =
  match box Unchecked.defaultof<'T> with
  | :? uint32 -> uint32 value |> unbox<'T>
  | :? uint16 -> uint16 value |> unbox<'T>
  | :? bool -> bool value |> unbox<'T>
  | :? string -> value |> unbox<'T>
  | :? int32 -> int value |> unbox<'T>
  | :? int64 -> int value |> unbox<'T>
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
    
let getValue = fun value ->
    match value with
    | Some v -> v
    | None -> Unchecked.defaultof<'a>
 
let emptyArray =
    let array: byte [] = Array.zeroCreate 0
    array

let encodeBase64 (str: string) =
    let plainTextBytes = Encoding.UTF8.GetBytes(str);
    Convert.ToBase64String(plainTextBytes)

let decodeBase64 str =
    Encoding.UTF8.GetString(Convert.FromBase64String(str));