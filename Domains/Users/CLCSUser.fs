module Domains.Users.CLCSUser

open System
open Domains.Common.CommonTypes

[<CLIMutable>]
type CLCSUser = {
    [<MapColumn("IdUser")>]Id: Guid
    ObjectId: Guid
    ActivationKey: string
    IdFinancialInstitution: Guid
    Email: string
    ActivationStatus: string
}