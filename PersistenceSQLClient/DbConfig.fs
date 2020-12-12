module PersistenceSQLClient.DbConfig

open System
open System.Data.SqlClient
open System.Threading.Tasks

[<Literal>]
let ConnectionString = "Server=192.168.5.11;Initial Catalog=CLCSPortal;Persist Security Info=False;User ID=LVAccount;Password=cluemt**1!;persist security info=False;"
// let connectionString = "Server=192.168.5.11;Initial Catalog=CLCSPortal;Persist Security Info=False;User ID=LVAccount;Password=cluemt**1!;persist security info=False;"
//let ConnectionString = "data source=.;Initial catalog=Rodeo.bgQL;Integrated Security=SSPI;"
// let ConnectionString = "data source=.;Initial catalog=LoanVantage_Trunk_IBS_1;Integrated Security=SSPI;";
type TransactionResult<'a> = Success of Option<'a> | Error of Exception

let ResultNone<'a> = None |> Success :> TransactionResult<'a>
let ResultSuccess(a: 'a) = Some a |> Success

type TransactionPayload = (SqlConnection * SqlTransaction)

type TransactionFunctionTask<'a> = TransactionPayload -> Task<TransactionResult<'a>>
type TransactionFunctionAsync<'a> = TransactionPayload -> Async<TransactionResult<'a>>

type TransactionFunction<'a> =
    | TAsync of TransactionFunctionAsync<'a>
    | ATIgnore of Async<'a>
    | TTask of TransactionFunctionTask<'a>
    | TTIgnore of Task<'a>
    
type TransactionException(code, message) =
    inherit Exception(message)
    member __.Code = code

