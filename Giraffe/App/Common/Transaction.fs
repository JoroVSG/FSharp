module App.Common.Transaction

open System
open System.Data.SqlClient
open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration
open PersistenceSQLClient.DbConfig
open FsToolkit.ErrorHandling.AsyncResultCE
open App.Helpers.HelperFunctions
open App.Common.Exceptions

type TransactionFunction<'a, 'b> =  TransactionPayload -> HttpContext -> Task<Result<'a, 'b>>
type TransactionFunction'<'a> = TransactionFunction<'a>
let withTransaction<'a> = fun (f: TransactionFunction<'a, exn>) (ctx: HttpContext) ->
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
            match res with
                | Ok a -> return a |> Ok
                | Error exp ->
                    do! trans.RollbackAsync()
                    return exp |> Error
        with ex ->
            do! trans.RollbackAsync()
            return Error ex
    }
    
let transaction = fun f next ctx ->
    task {
        match! withTransaction f ctx with
            | Ok res -> return! json res next ctx
            | Error exp -> return! handleErrorJsonAPI exp next ctx
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
                | Error' ex -> return this.HandleError(ex)
            }
            wrap.Result
        with ex ->
            let err = task {
                do! this.RollBackAndDispose()
                return Error' ex
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
            | Error' ex -> this.HandleError(ex)
    
    member this.HandleError(ex: Exception) =
            let err = task {
                do! this.RollBackAndDispose()
                return ex 
            }
            Error' err.Result
            
        
        
        
let transaction' cStr = TransactionBuilder(cStr)

let createTransactionBuild (ctx: HttpContext) =
    let settings = ctx.GetService<IConfiguration>()
    let conStr = settings.["ConnectionString:DefaultConnectionString"]
    transaction' conStr

let transactionFunctionCompose = fun (f1: TransactionFunction<'a, 'b>) (f2: TransactionFunction<'c, 'b>) ->
    fun (payload: TransactionPayload) (ctx: HttpContext) ->
        asyncResult{
            let! res1 = f1 payload ctx
            let! res2 = f2 payload ctx
            return (res1, res2)
        }
        |> wrap

let transactionFunctionCompose'= fun (f1: TransactionFunction<'a, 'b>) (f2: 'a -> TransactionFunction<'c, 'b>) ->
    fun (payload: TransactionPayload) (ctx: HttpContext) ->
        asyncResult {
            let! res1 = f1 payload ctx
            let! res2 = f2 res1 payload ctx
            return res2
        }
        |> wrap

let transactionFunctionComposeIgnoreValue = fun (f1: TransactionFunction<'a, 'b>) (f2: TransactionFunction<'c, 'b>) ->
    fun (payload: TransactionPayload) (ctx: HttpContext) ->
        asyncResult {
            let! _ = f1 payload ctx
            let! res2 = f2 payload ctx
            return res2
        }
        |> wrap

 
let (=>) = transactionFunctionCompose
let (>==>) = transactionFunctionCompose'
let (>>!) = transactionFunctionComposeIgnoreValue



type State = int

let bind = fun monad binder ->
    fun initialState ->
            let x, newState = monad initialState
            let y, ss = binder x newState
            (y, ss)
    
type StateMonad() =
    let (>>=) monad binder = bind monad binder
    member __.Bind(monad, binder) = monad >>= binder     
    member __.Return(a) = fun s -> (a, s)
    member __.Combine(statefulA, statefulB) =
        statefulA >>= (fun _ -> statefulB)


let tick = fun (s: State) -> ((), s + 1)

type Term =
    | Const of int
    | Div of (Term * Term)

let state = StateMonad()
    
let rec eval = fun (term: Term) ->
    state {
        match term with
            | Const a -> return a
            | Div (x, y) ->
                let! x' = eval x
                let! z = eval y
                
                do! tick   
                do! tick   
                do! tick   
                do! tick   
                do! tick   
                do! tick
                
                return (x' / z)
    }
    
let term = (Const 6, Const 2) |> Div
let (res, calculatedState) = eval term 0   