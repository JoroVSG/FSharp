module Domains.Applications.Application

open System
open FSharp.Data.Sql.Common

[<CLIMutable>]
type Application = {
    [<MappedColumn("IdApplication")>]Id: Guid
    Description: string
    Name: string
    Code: string
    Rating: int
    Image: byte[]
}