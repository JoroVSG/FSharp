module Persistence.DataContext
open FSharp.Data.Sql
[<Literal>]
// let ConnectionString = "data source=.;Initial catalog=Rodeo.bgQL;Integrated Security=SSPI;"
let ConnectionString = "Server=192.168.5.11;Initial Catalog=CLCSPortal;Persist Security Info=False;User ID=LVAccount;Password=cluemt**1!;persist security info=False;"

//[<Literal>]
//let ConnectionString = "data source=.;Initial catalog=LoanVantage_Trunk_IBS_1;Integrated Security=SSPI;";
// data source=DEV;Initial catalog=CLCSPortal;user=LVAccount;password=cluemt**1!;persist security info=False;
type CLCSPortalSchema = SqlDataProvider<Common.DatabaseProviderTypes.MSSQLSERVER, ConnectionString, UseOptionTypes=true>
let CLCSPortalContext = CLCSPortalSchema.GetDataContext ConnectionString