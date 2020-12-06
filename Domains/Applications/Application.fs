module Domains.Applications.Application

open System
open Dapper.Contrib.Extensions
open Domains.Common.CommonTypes
//open FSharp.Data.Sql.Common

[<CLIMutable>]
type Application = {
    // [<MappedColumn("IdApplication")>]Id: Guid
    [<MapColumn("IdApplication")>][<Computed>]Id: Guid option
    IdApplication: Guid
    Description: string option
    Name: string option
    Code: string option
    Rating: int option
    Image: byte[] option
}