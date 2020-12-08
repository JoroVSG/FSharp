module Domains.Applications.Application

open System
[<CLIMutable>]
type Application = {
    IdApplication: Guid
    Description: string option
    Name: string option
    Code: string option
    Rating: int option
    // Image: byte[] option
}