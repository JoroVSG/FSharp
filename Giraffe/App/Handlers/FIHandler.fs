module App.Handlers.FIHandler

open System
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
open PersistenceSQLClient.FiData
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Common.Messages
open App.DTOs.UserInviteDTO
open PersistenceSQLClient.UserData


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
        
        let apps = inviteDto.Applications
                       |> Seq.filter(fun a -> a.Code.IsSome)
                       |> Seq.map(fun a -> a.Code.Value)
                       |> String.concat(",")
                       
        let crypto = ctx.GetService<ICryptoService>()
        
        let key: ActivationKey = {
            Email = inviteDto.Email
            InstitutionId = inviteDto.InstitutionId
            IsFiAdmin = inviteDto.IsFiAdmin
            RoutingNumber = inviteDto.RoutingNumber
            Apps = apps }
        
        let keyToString = string key        
        
        let encrypted = crypto.Encrypt(keyToString, settings.["SecretPassPhrase"])

        match user with
            | Some usr ->
                let! _ = updateUserAsync { usr with ActivationKey = Some keyToString } payload
                return encrypted |> Ok
            | None ->
                let! _ =
                    createUserAsync {
                        Email = inviteDto.Email
                        ActivationKey = Some keyToString
                        IsFiAdmin = Some inviteDto.IsFiAdmin
                        IdFinancialInstitution = Some institution.Value.IdFinancialInstitution
                        IdUser = Guid.NewGuid()
                        ObjectId = None
                        ActivationStatus = Some "0"
                    } payload
                return encrypted |> Ok
    }


let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"
let usersPermissionCheckInvite = fun iid -> profitStarsFiAdminCombined iid >=> profitStarsFiAdminErrorHandling (forbidden INVITE_USER_PROFIT_STARS_FAIL) (forbidden INVITE_USER_FI_FAIL)

let fiGetRoutes: HttpHandler list = [
    route "/fi" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction getFs
    routef "/fi/%O" (fun iid -> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction (getFiById iid))
    routef "/fi/%O/users/invite" (fun iid -> authorize >=> usersPermissionCheckInvite iid >=> transaction (inviteUser iid)) 
]