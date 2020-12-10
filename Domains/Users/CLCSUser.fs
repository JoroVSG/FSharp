module Domains.Users.CLCSUser

open System

[<CLIMutable>]
type CLCSUser = {
    IdUser: Guid
    ObjectId: Guid
    ActivationKey: string
    IdFinancialInstitution: Guid
    Email: string
    ActivationStatus: string
}