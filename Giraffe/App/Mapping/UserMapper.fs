module App.Mapping.UserMapper

open App.Helpers.HelperFunctions
open App.DTOs.ApplicationDTO
open App.DTOs.UserDTO
open Domains.B2CUser
open Domains.Users.CLCSUser
open JsonApiSerializer.JsonApi

let mapToUserDTO = fun apps (b2cUser:B2CUser) (clcsUser: CLCSUser) ->
    let relationshipApps = Relationship<ApplicationDTO list>()
    relationshipApps.Data <- apps
    let dto = {
        ObjectId = clcsUser.ObjectId
        Id = clcsUser.IdUser
        IdUser = clcsUser.IdUser
        Email = clcsUser.Email
        ActivationStatus = clcsUser.ActivationStatus
        DeletionTimestamp = b2cUser.DeletionTimestamp
        AccountEnabled = b2cUser.AccountEnabled
        City = b2cUser.City
        CompanyName = b2cUser.CompanyName
        Country = b2cUser.Country
        CreationType = b2cUser.CreationType
        Department = b2cUser.Department
        DisplayName = b2cUser.DisplayName
        EmployeeId = b2cUser.EmployeeId
        FirstName = b2cUser.GivenName
        LastName = b2cUser.Surname
        JobTitle = b2cUser.JobTitle
        Mobile = b2cUser.Mobile
        OtherMails = b2cUser.OtherMails
        PasswordPolicies = b2cUser.PasswordPolicies
        PasswordProfile = b2cUser.B2CPasswordProfile
        PostalCode = b2cUser.PostalCode
        SignInNames = b2cUser.SignInNames
        State = b2cUser.State
        StreetAddress = optional' b2cUser.StreetAddress
        TelephoneNumber = optional' b2cUser.TelephoneNumber
        UsageLocation = b2cUser.UsageLocation
        UserIdentities = b2cUser.UserIdentities
        UserPrincipalName = b2cUser.UserPrincipalName
        UserType = b2cUser.UserType
        Type = "user"
        Applications = relationshipApps
        IsFiAdmin = b2cUser.IsFiAdmin
    }
    dto