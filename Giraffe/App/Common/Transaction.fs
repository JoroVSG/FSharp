module App.Common.Transaction

open System
open System.Data.SqlClient
open System.Threading.Tasks
open Giraffe
open App.Common.JsonApiResponse
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration
open PersistenceSQLClient.DbConfig


type TransactionFunction<'a> = TransactionPayload -> HttpContext -> Task<'a>
type TransactionFunction'<'a> = PersistenceSQLClient.DbConfig.TransactionFunction<'a>
let withTransaction<'a> = fun (f: TransactionFunction<'a>) (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let connectionStringFromConfig = config.["ConnectionString:DefaultConnectionString"]
        use connectionString = new SqlConnection(connectionStringFromConfig)
        do! connectionString.OpenAsync()
        use trans = connectionString.BeginTransaction()
        try
            let! res = f (connectionString, trans) ctx
            do! trans.CommitAsync()
            do! connectionString.CloseAsync()
            return res
        with ex ->
            do! trans.RollbackAsync()
            return raise ex
    }
    
let transaction<'a> = fun (f: TransactionFunction<'a>) (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! res = withTransaction f ctx
        return! json (jsonApiWrap res) next ctx
    }

type TransactionBuilder(connectionString) =
    let connectionInit = task {
        let c = new SqlConnection(connectionString)
        do! c.OpenAsync()
        let trans = c.BeginTransaction()
        return (c, trans)
    }
    let payload = connectionInit.Result
    let (c, trans) = payload

    member __.Payload = payload
    
    member this.Bind(func, next) =
        try this.HandleNext(func, next)
        with ex -> this.HandleError(ex)
    
    member this.Return(finalValue) =
        let wrap = task {
            do! trans.CommitAsync()
            do! c.CloseAsync()
            
            do! this.DisposeAsync()
            return finalValue
        }
        Success wrap.Result
    member this.ReturnFrom(finalValueAsync) =
        try
            let wrap = task {
                let res =
                    match finalValueAsync with
                    | TAsync f -> f payload |> Async.RunSynchronously
                    | TTask f ->
                        let t = f payload
                        t.Result
                    | ATIgnore f -> f |> Async.RunSynchronously |> ResultSuccess
                    | TTIgnore t ->  t.Result |> ResultSuccess
                
                match res with
                | Success a ->
                    do! trans.CommitAsync()
                    do! c.CloseAsync()
                    
                    do! this.DisposeAsync()
                    return Success a
                | Error ex -> return this.HandleError(ex)
            }
            wrap.Result
        with ex ->
            let err = task {
                do! this.RollBackAndDispose()
                return Error ex
            }
            err.Result
    member __.DisposeAsync(): Task<unit> =
        task {
            do! c.DisposeAsync()
            do! trans.DisposeAsync()
        }

    member this.RollBackAndDispose(): Task<unit> =
        task {
            do! trans.RollbackAsync()
            do! this.DisposeAsync()
        }

    member this.HandleNext(resultValue, next) =
        let res =
            match resultValue with
            | TAsync f -> f payload |> Async.RunSynchronously
            | TTask f ->
                let t = f payload
                t.Result
            | ATIgnore f -> f |> Async.RunSynchronously |> ResultSuccess
            | TTIgnore t ->  t.Result |> ResultSuccess
        
        match res with
            | Success a -> next a
            | Error ex -> this.HandleError(ex)
    
    member this.HandleError(ex: Exception) =
            let err = task {
                do! this.RollBackAndDispose()
                return ex 
            }
            Error err.Result
            
        
        
        
let transaction' cStr = TransactionBuilder(cStr)

let createTransactionBuild (ctx: HttpContext) =
    let settings = ctx.GetService<IConfiguration>()
    let conStr = settings.["ConnectionString:DefaultConnectionString"]
    transaction' conStr
    