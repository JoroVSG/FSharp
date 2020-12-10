module Domains.Users.CLCSUser

open System

[<CLIMutable>]
type CLCSUser = {
    IdUser: Guid
    ObjectId: Guid option
    ActivationKey: string option
    IdFinancialInstitution: Guid option
    Email: string
    ActivationStatus: string option
    IsFiAdmin: bool option
}