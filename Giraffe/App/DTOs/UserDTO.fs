module App.DTOs.UserDTO

open System
open JsonApiSerializer.JsonApi
open App.DTOs.ApplicationDTO
open Domains.B2CUser
open Newtonsoft.Json

[<CLIMutable>]
type UserDTO = {
    Id: Guid
    IdUser: Guid
    ObjectId: Guid option
    Email: string
    DeletionTimestamp: obj
    AccountEnabled: bool
    City: string
    CompanyName: obj
    Country: string
    CreationType: string
    Department: string
    DisplayName: string
    EmployeeId: obj
    FirstName: string
    LastName: string
    JobTitle: string
    Mobile: obj
    OtherMails: obj list
    PasswordPolicies: string
    PasswordProfile: B2CPasswordProfile
    PostalCode: string
    SignInNames: SignInNames list
    State: string
    StreetAddress: string option
    TelephoneNumber: obj option
    UsageLocation: string
    UserIdentities: obj list
    UserPrincipalName: string
    UserType: string
    Type: string
    ActivationStatus: string option
    Applications: Relationship<ApplicationDTO list>
    IsFiAdmin: bool
}

type B2CIdentities = {
    [<JsonProperty("signInType")>]SignInType: string
    [<JsonProperty("issuer")>]Issuer: string
    [<JsonProperty("issuerAssignedId")>]IssuerAssignedId: string
}

[<CLIMutable>]
type UserUpdateDTO = {
    [<JsonProperty("id"); JsonIgnore>]Id: string
    [<JsonProperty("isFiAdmin")>]mutable IsFiAdmin: string
    [<JsonProperty("objectId")>]mutable ObjectId: string
    [<JsonProperty("email")>]mutable Email: string
    [<JsonProperty("identities")>]mutable Identities: B2CIdentities list
    [<JsonProperty("displayName")>]mutable DisplayName: string
    [<JsonProperty("givenName")>]mutable GivenName: string
    [<JsonProperty("surName")>]mutable SurName: string

}