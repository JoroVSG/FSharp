module App.DTOs.ApplicationDTO

open System

[<CLIMutable>]
type ApplicationDTO = {
    Id: Guid
    IdApplication: Guid
    Description: string option
    Name: string option
    Code: string option
    Rating: int option
    Type: string
}
