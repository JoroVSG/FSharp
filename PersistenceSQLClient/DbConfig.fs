module PersistenceSQLClient.DbConfig

open System
open System.Data.SqlClient

[<Literal>]
let ConnectionString = "Server=192.168.5.11;Initial Catalog=CLCSPortal;Persist Security Info=False;User ID=LVAccount;Password=cluemt**1!;persist security info=False;"
// let connectionString = "Server=192.168.5.11;Initial Catalog=CLCSPortal;Persist Security Info=False;User ID=LVAccount;Password=cluemt**1!;persist security info=False;"
//let ConnectionString = "data source=.;Initial catalog=Rodeo.bgQL;Integrated Security=SSPI;"
// let ConnectionString = "data source=.;Initial catalog=LoanVantage_Trunk_IBS_1;Integrated Security=SSPI;";
    
type TransactionPayload = (SqlConnection * SqlTransaction)
type TransactionResult<'a> = Success of 'a | Error of Exception | NotFound

type TransactionFunctionAsync<'a> = TransactionPayload -> Async<TransactionResult<'a>>

