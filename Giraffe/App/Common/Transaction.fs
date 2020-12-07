module App.Common.Transaction

open System
open System.Data.Common
open System.Data.SqlClient
open System.Threading.Tasks
open Giraffe
open App.Common.JsonApiResponse
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Configuration

type TransactionPayload = (SqlConnection * SqlTransaction)
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