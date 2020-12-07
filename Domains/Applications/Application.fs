module Domains.Applications.Application

open System
open Dapper.Contrib.Extensions
open Dapper.Contrib.Extensions
open Domains.Common.CommonTypes
//open FSharp.Data.Sql.Common

[<CLIMutable>]
[<Table("Application")>]
type Application = {
    // [<MappedColumn("IdApplication")>]Id: Guid
    [<MapColumn("IdApplication")>][<Write(false)>]Id: Guid
    IdApplication: Guid
    Description: string option
    Name: string option
    Code: string option
    Rating: int option
    Image: byte[] option
}