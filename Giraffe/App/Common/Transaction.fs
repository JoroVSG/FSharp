module App.Common.Transaction

open System
open System.Data.Common
open System.Data.SqlClient
open System.Threading.Tasks
open App.Common
open Giraffe
open App.Common.JsonApiResponse
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration
open PersistenceSQLClient.DbConfig


type TransactionFunction<'a> = TransactionPayload -> HttpContext -> Task<'a>
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
    let (c, trans) = connectionInit.Result
    member this.Bind(func, next) =
        try
            let payload: TransactionPayload = (c, trans)
            
            let res =
                match func with
                    | TAsync f -> f payload |> Async.RunSynchronously
                    | TTask f -> (f payload).Result
                    | ATIgnore f -> f |> Async.RunSynchronously |> ResultSuccess
                    | TTIgnore t ->  t.Result |> ResultSuccess
            
            match res with
                | Success a -> next a
                | Error ex ->
                    let err = task {
                        do! this.DisposeAsync()
                        return ex 
                    }
                    Error err.Result
        with ex ->
            let err = task {
                do! trans.RollbackAsync()
                do! this.DisposeAsync()
                return ex
            }
            Error err.Result
        

    member this.Return(x) =
        let wrap = task {
            do! trans.CommitAsync()
            do! c.CloseAsync()
            
            do! this.DisposeAsync()
            return x
        }
        Success wrap.Result
    member this.ReturnFrom(x) =
        try
            let wrap = task {
                let payload: TransactionPayload = (c, trans)
                
                let res =
                    match x with
                    | TAsync f -> f payload |> Async.RunSynchronously
                    | TTask f ->
                        let t = f payload
                        t.Result
                
                do! trans.CommitAsync()
                do! c.CloseAsync()
                
                do! this.DisposeAsync()
                return res
            }
            wrap.Result
        with ex ->
            let err = task {
                do! trans.RollbackAsync()
                do! this.DisposeAsync()
                return Error ex
            }
            err.Result
    member this.DisposeAsync(): Task<unit> =
        task {
            do! c.DisposeAsync()
            do! trans.DisposeAsync()
        }
        
        
        
let transaction' cStr = TransactionBuilder(cStr)

let createTransactionBuild (ctx: HttpContext) =
    let settings = ctx.GetService<IConfiguration>()
    let conStr = settings.["ConnectionString:DefaultConnectionString"]
    transaction' conStr
    