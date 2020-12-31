module App.Handlers.FIHandler

open System
open App.DTOs.UserDTO
open Crypto.Resolvers
open Domains.Common.CommonTypes
open App.DTOs.ActivationKey
open App.DTOs.FiDTO
open AutoMapper
open Crypto
open Giraffe
open App.Common.Authentication
open App.Common.JsonApiResponse
open App.Common.Transaction
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open PersistenceSQLClient.FiData
open App.Handlers.Security.Permissions
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Common.Messages
open App.DTOs.UserInviteDTO
open PersistenceSQLClient.UserData
open App.Common.Exceptions
open App.Helpers.SMTPClient
open App.Helpers.MSALClient
open App.Helpers.HelperFunctions

let getFs = fun payload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! res = getFis payload
        return res
            |> List.map(mapper.Map<FiDTO>)
            |> Ok
    }

let getFiById = fun iid payload _ ->
    task {
        let! fi = getFiById iid payload
        return fi |> resultOrNotFound
    }
    
let inviteUser = fun iid payload (ctx: HttpContext) ->    
    task {
        let! inviteDto = ctx.BindJsonAsync<UserInviteDTO>()
        let settings = ctx.GetService<IConfiguration>()
        
        let! institution = getFiByInstitutionId iid payload
        
        let! user = getUserByEmailAsync inviteDto.Email payload
        
//        let apps = inviteDto.Applications
//                       |> Seq.filter(fun a -> a.Code.IsSome)
//                       |> Seq.map(fun a -> a.Code.Value)
//                       |> String.concat(",")
                       
        let crypto = ctx.GetService<ICryptoService>()
        
        let key: ActivationKey = {
            Email = inviteDto.Email
            InstitutionId = iid
            IsFiAdmin = inviteDto.IsFiAdmin
            RoutingNumber = inviteDto.RoutingNumber
            Apps = "" }
        
        let keyToString = string key        
        
        let encrypted = crypto.Encrypt(keyToString, settings.["SecretPassPhrase"])

        let keyWrapper = {
             DisplayName = inviteDto.DisplayName
             FirstName = inviteDto.FirstName
             LastName = inviteDto.LastName
             Phone = inviteDto.Phone
             ActivationKeyEncrypted = encrypted
        }
        
        let keyWrapperStringify = string keyWrapper
        
        let emailFrom = match institution.Value.EmailSendingInviteFrom with
                                | Some email -> email
                                | None -> "no-reply@clcsadmin.profitstars.com"
        
        match user with
            | Some usr ->
                let! _ = updateUserAsync { usr with ActivationKey = Some encrypted } payload
                do! sendInviteEmailAsync ctx emailFrom inviteDto.Email keyWrapperStringify
                return { Id = Guid.NewGuid(); Success = true; Exception = None } |> Ok
            | None ->
                let! createdUserResult =
                    createUserAsync {
                        Email = inviteDto.Email
                        ActivationKey = encrypted |> Some
                        IsFiAdmin = inviteDto.IsFiAdmin |> Some
                        IdFinancialInstitution = institution.Value.IdFinancialInstitution |> Some
                        IdUser = Guid.NewGuid()
                        ObjectId = None
                        ActivationStatus = "0" |> Some
                    } payload
                    
                match inviteDto.Applications with
                    | Some app -> 
                         app
                            |> Seq.map(fun app ->
                                async {
                                    let! y = insertUserApplication app.IdApplication createdUserResult.Id payload
                                    return y
                                })
                            |> Async.Parallel
                            |> Async.Ignore
                            |> Async.RunSynchronously
                    | None -> ()          
                do! sendInviteEmailAsync ctx emailFrom inviteDto.Email keyWrapperStringify
                return { Id = Guid.NewGuid(); Success = true; Exception = None } |> Ok
    }

let updateUser = fun (uid: Guid) payload (ctx: HttpContext) ->
    task {
        let! userDto = ctx.BindJsonAsync<UserUpdateDTO>()
        let config = ctx.GetService<IConfiguration>()
        
        match! getUserByObjectId uid payload  with
            | Some local ->
                let propertyRenameContractResolver = PropertyRenameAndIgnoreSerializerContractResolver()
                let claimName = sprintf "extension_%s_IsFiAdmin" (config.["GraphApi:ClientId"].Replace("-", ""))
                propertyRenameContractResolver.RenameProperty(userDto.GetType(), "isFiAdmin", claimName);
                
                match local.ObjectId with
                    | Some oid ->
                        let mutable email = local.Email
                        
                        
                        let userMapped =
                            if userDto.Email <> null then
                                let identity = {
                                    SignInType = "emailAddress"
                                    Issuer = sprintf "%s.onmicrosoft.com" config.["GraphApi:Tenant"]
                                    IssuerAssignedId = email
                                }
                                
                                email <- userDto.Email
                                
                                { userDto with Identities = [identity]; ObjectId = null; Email = null }
                            else
                                { userDto with ObjectId = null }
                                
                        let isFiAdmin = if userDto.IsFiAdmin <> null then userDto.IsFiAdmin else string local.IsFiAdmin.Value
                        
                        let serializerSettings = JsonSerializerSettings()
                        serializerSettings.DefaultValueHandling <- DefaultValueHandling.Ignore
                        serializerSettings.ContractResolver <- propertyRenameContractResolver
                        
                        let p = JsonConvert.SerializeObject(userMapped, serializerSettings)
                        let api = sprintf "%s/users/%s" config.["GraphApi:ApiVersion"] (string oid)
                        do! sendPATCHGraphApiWithConfigRequest p ctx api
                        let! _ = updateUserAsync { local with Email = email; IsFiAdmin = (bool isFiAdmin |> Some) } payload
                        
                        return { Id = Guid.NewGuid(); Success = true; Exception = None } |> Ok
                    | None -> return NotFoundRequestResult "User has not been mapped to Azure B2C" |> Error
            | None -> return NotFoundRequestResult "User not found by object Id" |> Error
    }

let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"
let usersPermissionCheckInvite = fun iid -> profitStarsFiAdminCombined iid >=> profitStarsFiAdminErrorHandling (forbidden INVITE_USER_PROFIT_STARS_FAIL) (forbidden INVITE_USER_FI_FAIL)

let fiGetRoutes: HttpHandler list = [
    routeCi "/fi" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction getFs
    routeCif "/fi/%O" (fun iid -> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction (getFiById iid))
]

let fiPostRoutes: HttpHandler list = [
    routeCif "/fi/%s/users/invite" (fun iid -> authorize >=> usersPermissionCheckInvite iid >=> transaction (inviteUser iid)) 
]

let fiPatchRoutes: HttpHandler list = [
    routeCif "/fi/%s/relationship/users/%O" (fun (iid, uid) -> authorize >=> usersPermissionCheckInvite iid >=> transaction (updateUser uid))
]