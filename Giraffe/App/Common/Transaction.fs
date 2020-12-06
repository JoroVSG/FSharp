module App.Common.Transaction

open System
open System.Data.SqlClient
open System.Threading.Tasks
open Giraffe
open App.Common.JsonApiResponse
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration

let withTransaction<'a> = fun (f: SqlConnection -> Task<'a>) (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let connectionStringFromConfig = config.["ConnectionString:DefaultConnectionString"]
        use connectionString = new SqlConnection(connectionStringFromConfig)
        do! connectionString.OpenAsync()
        use! scope = connectionString.BeginTransactionAsync()
        try
            let! res = f connectionString
            do! scope.CommitAsync()
            return json (jsonApiWrap res) next ctx
        with
            _ ->
                do! scope.RollbackAsync()
                return earlyReturn ctx
        
    }
type TransactionFunction<'a> = SqlConnection -> HttpContext -> Task<'a>
let withTransaction'<'a> = fun (f: TransactionFunction<'a>) (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let connectionStringFromConfig = config.["ConnectionString:DefaultConnectionString"]
        use connectionString = new SqlConnection(connectionStringFromConfig)
        do! connectionString.OpenAsync()
        use! scope = connectionString.BeginTransactionAsync()
        try
            let! res = f connectionString ctx
            do! scope.CommitAsync()
            return res
        with ex ->
            do! scope.RollbackAsync()
            return raise ex
                
        
    }
    
let transaction<'a> = fun (f: TransactionFunction<'a>) (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let! res = withTransaction' f ctx
        return! json (jsonApiWrap res) next ctx
    }