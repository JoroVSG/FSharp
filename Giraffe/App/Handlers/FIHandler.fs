module App.Handlers.FIHandler

open App.DTOs
open App.DTOs.FiDTO
open AutoMapper
open Crypto
open Giraffe
open App.Common.Authentication
open App.Common.JsonApiResponse
open App.Common.Transaction
open Microsoft.AspNetCore.Http
open PersistenceSQLClient.FiData
open App.Handlers.Security.Permissions
open App.Helpers.HelperFunctions
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Common.Messages
open Newtonsoft.Json
open App.DTOs.UserInviteDTO
open PersistenceSQLClient.UserData

let getFs = fun payload (ctx: HttpContext) ->
    task {
        let mapper = ctx.GetService<IMapper>()
        let! res = getFis payload
        return res
            |> List.map(fun r -> mapper.Map<FiDTO>(r))
            |> Ok
    }

let getFiById = fun iid payload _ ->
    task {
        let! fi = getFiById iid payload
        return fi |> resultOrNotFound
    }
    
let inviteUser = fun iid payload (ctx: HttpContext) ->    
    task {
        let! body = ctx.ReadBodyFromRequestAsync()
        let inviteDto = JsonConvert.DeserializeObject<UserInviteDTO>(body)
        
        let! institution = getFiByInstitutionId iid payload
        
        let! user = getUserByEmailAsync inviteDto.Email payload
        
        let crypto = ctx.GetService<ICryptoService>()
        
        return iid |> Ok
    }


let profitStarsErrorHandler = forbidden "Only Profitstars or Financial Institution admins are allowed to retrieve users for that financial institution"
let usersPermissionCheckInvite = fun iid -> profitStarsFiAdminCombined iid >=> profitStarsFiAdminErrorHandling (forbidden INVITE_USER_PROFIT_STARS_FAIL) (forbidden INVITE_USER_FI_FAIL)

let fiGetRoutes: HttpHandler list = [
    route "/fi" >=> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction getFs
    routef "/fi/%O" (fun iid -> authorize >=> profitStarsAdminCheckOny profitStarsErrorHandler >=> transaction (getFiById iid))
    routef "/fi/%O/users/invite" (fun iid -> authorize >=> usersPermissionCheckInvite iid >=> transaction (inviteUser iid)) 
]