module Domains.B2CUser

open System
open Newtonsoft.Json


type B2CPasswordProfile ={
    [<JsonProperty("password")>] Password: obj
    [<JsonProperty("forceChangePasswordNextLogin")>] ForceChangePasswordNextLogin: bool
    [<JsonProperty("enforceChangePasswordPolicy")>] EnforceChangePasswordPolicy: bool 
}

type SignInNames = {
    [<JsonProperty("value")>] Value: string
    [<JsonProperty("type")>] Type: string 
}

type B2CUser = {
    [<JsonProperty("id")>] Id: Guid
    [<JsonProperty("objectType")>] ObjectType: string
    [<JsonProperty("objectId")>] ObjectId: string
    [<JsonProperty("deletionTimestamp")>]DeletionTimestamp: obj
    [<JsonProperty("accountEnabled")>] AccountEnabled: bool
    [<JsonProperty("ageGroup")>] AgeGroup: obj
    [<JsonProperty("assignedLicenses")>] AssignedLicenses: obj list
    [<JsonProperty("assignedPlans")>] AssignedPlans: obj list
    [<JsonProperty("city")>] City: string
    [<JsonProperty("companyName")>] CompanyName: string
    [<JsonProperty("consentProvidedForMinor")>] ConsentProvidedForMinor: obj
    [<JsonProperty("country")>] Country: string
    [<JsonProperty("creationType")>] CreationType: string
    [<JsonProperty("department")>] Department: string
    [<JsonProperty("dirSyncEnabled")>] DirSyncEnabled :string
    [<JsonProperty("displayName")>] DisplayName: string
    [<JsonProperty("employeeId")>]  EmployeeId: obj
    [<JsonProperty("facsimileTelephoneNumber")>] FacsimileTelephoneNumber : obj
    [<JsonProperty("givenName")>]  GivenName: string
    [<JsonProperty("immutableId")>] ImmutableId: string
    [<JsonProperty("isCompromised")>] IsCompromised: obj
    [<JsonProperty("jobTitle")>] JobTitle: string
    [<JsonProperty("lastDirSyncTime")>] LastDirSyncTime: obj
    [<JsonProperty("legalAgeGroupClassification")>] LegalAgeGroupClassification: obj
    [<JsonProperty("mail")>] Mail: string
    [<JsonProperty("mailNickname")>] MailNickname: string 
    [<JsonProperty("mobile")>] Mobile: obj
    [<JsonProperty("onPremisesDistinguishedName")>] OnPremisesDistinguishedName: obj
    [<JsonProperty("onPremisesSecurityIdentifier")>] OnPremisesSecurityIdentifier: obj
    [<JsonProperty("otherMails")>] OtherMails: obj list
    [<JsonProperty("passwordPolicies")>] PasswordPolicies: string
    [<JsonProperty("passwordProfile")>]  B2CPasswordProfile: B2CPasswordProfile 
    [<JsonProperty("physicalDeliveryOfficeName")>] PhysicalDeliveryOfficeName: obj 
    [<JsonProperty("postalCode")>] PostalCode: string
    [<JsonProperty("preferredLanguage")>]  PreferredLanguage: obj
    [<JsonProperty("provisionedPlans")>]ProvisionedPlans: obj list
    [<JsonProperty("provisioningErrors")>] ProvisioningErrors: obj list
    [<JsonProperty("proxyAddresses")>] ProxyAddresses: obj list
    //[<JsonProperty("showInAddressList")>] ShowInAddressList: bool
    [<JsonProperty("signInNames")>] SignInNames: SignInNames list
    [<JsonProperty("sipProxyAddress")>] SipProxyAddress: obj 
    [<JsonProperty("state")>] State: string
    [<JsonProperty("streetAddress")>] StreetAddress: string
    [<JsonProperty("surname")>] Surname: string
    [<JsonProperty("telephoneNumber")>] TelephoneNumber: obj
    [<JsonProperty("usageLocation")>]  UsageLocation: string
    [<JsonProperty("userIdentities")>] UserIdentities: obj list
    [<JsonProperty("userPrincipalName")>] UserPrincipalName: string
    [<JsonProperty("userType")>] UserType: string 
}