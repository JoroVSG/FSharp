module DataContext
open FSharp.Data.Sql
[<Literal>]
let ConnectionString = "data source=.;Initial catalog=Rodeo.bgQL;Integrated Security=SSPI;"

//[<Literal>]
//let ConnectionString = "data source=.;Initial catalog=LoanVantage_Trunk_IBS_1;Integrated Security=SSPI;";

type RodeoSchema = SqlDataProvider<Common.DatabaseProviderTypes.MSSQLSERVER, ConnectionString>

let RodeoContext = RodeoSchema.GetDataContext ConnectionString