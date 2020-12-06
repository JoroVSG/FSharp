module Domains.FIs.FinancialInstitution

open System

[<CLIMutable>]
type FI = {
    IdFinancialInstitution: Guid
    Name: string
    ObjectId: Guid
    Description: string
    InstitutionId: string
    EmailSendingInviteFrom: string
}