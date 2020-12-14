module App.DTOs.FiDTO

open System

[<CLIMutable>]
type FiDTO = {
    Id: Guid
    Name: string option
    ObjectId: Guid option
    Description: string option
    InstitutionId: string option
    EmailSendingInviteFrom: string option
    RoutingNumber: string option
}