module Domains.FIs.FinancialInstitution

open System

[<CLIMutable>]
type FI = {
    IdFinancialInstitution: Guid
    Name: string option
    ObjectId: Guid option
    Description: string option
    InstitutionId: string option
    EmailSendingInviteFrom: string option
    RoutingNumber: string option
}