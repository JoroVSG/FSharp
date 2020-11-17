module DataContext
open FSharp.Data.Sql
[<Literal>]
let ConnectionString = "Data source = localhost; initial catalog = Rodeo.bgQL6; user id = sa; password = Temp123!";

type RodeoSchema = SqlDataProvider<Common.DatabaseProviderTypes.MSSQLSERVER, ConnectionString>

let RodeoContext = RodeoSchema.GetDataContext ConnectionString