module Domains.Users.UserDTO

open System
open Domains.B2CUser

type UserDTO = {
    Id: Guid
    IdUser: Guid
    ObjectId: Guid
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
    StreetAddress: string
    TelephoneNumber: obj
    UsageLocation: string
    UserIdentities: obj list
    UserPrincipalName: string
    UserType: string
    Type: string
    ActivationStatus: string
    IsFiAdmin: bool
    
}
  
//public Relationship<List<ApplicationDTO>> Applications { get; set; } = new Relationship<List<ApplicationDTO>>();
//public Status ActivationStatus { get; set; }