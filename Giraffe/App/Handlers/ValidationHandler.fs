module App.Handlers.ValidationHandler

open System.Collections.Generic
open System.Dynamic
open System.Security.Claims
open System.Web
open App.DTOs.ActivationKey
open Domains.FIs.FinancialInstitution
open Domains.Users.CLCSUser
open Microsoft.Extensions.Configuration
open PersistenceSQLClient.UserData
open PersistenceSQLClient.FiData
open Crypto
open Giraffe
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Helpers.HelperFunctions
open Microsoft.Extensions.Configuration
open App.Common.Authentication
open App.Common.Exceptions
open App.Common.Transaction
open Newtonsoft.Json
open System
open App.Helpers.Constants
open App.Helpers.MSALClient

type UserGroup = {
    [<JsonProperty("@odata.id")>] OdataId: string
}
with override this.ToString() = JsonConvert.SerializeObject(this)

let invitation = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let crypto = ctx.GetService<ICryptoService>()
        let config = ctx.GetService<IConfiguration>()
        
        match ctx.TryGetQueryStringValue "activationKey" with
            | Some activationKey ->
                let decoded = decodeBase64 activationKey
    
                let wrapper = JsonConvert.DeserializeObject<ActivationKeyWrapper>(decoded);
                
                let encryptedJson = crypto.Decrypt(wrapper.ActivationKeyEncrypted, config.["SecretPassPhrase"]);
                    
                let key = JsonConvert.DeserializeObject<ActivationKey>(encryptedJson)
                
                let url = sprintf "%s://%s%s/api/validation/redeemed"
                                   ctx.Request.Scheme
                                   (string ctx.Request.Host)
                                   (string ctx.Request.PathBase)

                let properties = AuthenticationProperties (RedirectUri = url);
                

                properties.Items.["Policy"] <- config.["Authentication:AzureAdB2C:InvitePolicyId"]
                properties.Items.["verified_email"] <- key.Email
                properties.Items.["display_name"] <- wrapper.DisplayName
                properties.Items.["first_name"] <- wrapper.FirstName
                properties.Items.["last_name"] <- wrapper.LastName
                properties.Items.["phone"] <- wrapper.Phone

                properties.Items.["activationKey"] <- wrapper.ActivationKeyEncrypted
                let schema = OpenIdConnectDefaults.AuthenticationScheme
                
                do! ctx.ChallengeAsync(schema, properties = properties)
                
                return! next ctx
            | None ->
                let exp = BadRequestResult "Activation key is required parameter"
                return! handleErrorJsonAPI exp next ctx
        
        
    }

let getEmail = fun __ _ (ctx: HttpContext) ->
    task {
        //        var email = user.Claims?.FirstOrDefault(c => c.Type == "emails")?.Value ??
        //                            user.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        //
        //       if (string.IsNullOrEmpty(email))
        //       {
        //           var activationKey = user.Claims?.FirstOrDefault(c => c.Type == "extension_ActivationKey")?.Value;
        //           var encryptedJson = _crypto.Decrypt(activationKey, _appSettings.SecretPassPhrase);
        //
        //           var key = (ActivationKey) JsonConvert.DeserializeObject(encryptedJson, typeof(ActivationKey));
        //
        //           email = key.Email;
        //       }
        
        let tryEmail = tryGetClaimWithPredicate (fun claim -> claim.Type = EMAIL_CLAIM_TYPE || claim.Type = ClaimTypes.Email) ctx
    
        match tryEmail with
            | Some email -> return email.Value |> Ok
            | None ->
                match tryGetClaim ACTIVATION_KEY_CLAIM_TYPE ctx with
                    | Some activationKey ->
                        try
                            let crypto = ctx.GetService<ICryptoService>()
                            let config = ctx.GetService<IConfiguration>()
                            let encryptedJson = crypto.Decrypt(activationKey.Value, config.["SecretPassPhrase"])
                            let key = JsonConvert.DeserializeObject<ActivationKey>(encryptedJson)
                            return key.Email |> Ok
                    
                        with ex -> return ex |> Error
                    | None -> return NotFoundRequestResult "No email Found" |> Error
        
    }
 
 
let getLocalUserByEmail = fun email payload _ ->
     task {
         match! getUserByEmailAsync email payload with
            | Some user -> return user |> Ok
            | None -> return NotFoundRequestResult "User not found" |> Error
         
     }
let getFi = fun (user: CLCSUser) payload _ ->
    task {
        match user.IdFinancialInstitution with
            | Some iid ->
                match! getFiById iid payload with 
                    | Some institution -> return (user, institution) |> Ok 
                    | None -> return NotFoundRequestResult "The institution was not found" |> Error
            | None -> return NotFoundRequestResult "Institution Claim not found" |> Error
    }
    
    
let updateUserObjectId = fun user payload ctx ->
    task {
        match tryGetClaim ClaimTypes.NameIdentifier ctx with
            | Some oid ->
                let u: CLCSUser = { user with ObjectId = Guid(oid.Value) |> Some;  ActivationStatus = "1" |> Some }
                let! _ = updateUserAsync u payload
                return u |> Ok
            | None -> return NotFoundRequestResult "ObjectId Claim not found" |> Error
    }

let assignedTheUserToAzureFiGroup = fun (user: CLCSUser, fi: FI) _ (ctx: HttpContext) ->
    task {
        let config = ctx.GetService<IConfiguration>()
        let userRef = sprintf "%s/%s/users/%s" config.["GraphApi:ApiUrl"] config.["GraphApi:ApiVersion"] (string <| getValue user.ObjectId)
        
        let userGroup = {
            OdataId = userRef
        }
        let api = sprintf "%s/groups/%s/$ref" config.["GraphApi:ApiVersion"] (string <| getValue fi.ObjectId)
        
        do! sendPOSTGraphApiWithConfigRequest (string userGroup) ctx api
        return (user, fi) |> Ok
        
        //        var userRef =
        //                    $"{_appSettings.AzureB2CGraphSettings.AadGraphEndpoint_v2}/{_appSettings.AzureB2CGraphSettings.AadGraphEndpoint_v2_version}/users/{oid}";
        //
        //     var userGroup = new UserHandler.UserGroup {OdataId = userRef};

        //     await _msalService.SendGraphRequestAsync<B2CGroup>(
        //         $"{_appSettings.AzureB2CGraphSettings.AadGraphEndpoint_v2_version}/groups/{fi?.ObjectId}/members/$ref",
        //         userGroup.ToString(), HttpMethod.Post);
    }
    
let parseAndValidateToken = fun _ (ctx: HttpContext) ->
    task {
        let! body = ctx.ReadBodyFromRequestAsync()
        let inputClaims = HttpUtility.ParseQueryString(body)
        let token = inputClaims.["access_token"]
        
        if String.IsNullOrWhiteSpace token = false then
            return! validateAzureB2CToken ctx token
        else return BadRequestResult "No access_token presented" |> Error
    }
    
let updatingUserApplicationClaim = fun (user: CLCSUser, _) payload (ctx: HttpContext) ->
    task {
        let! apps = getUserApplicationsByEmail user.Email payload
        let config = ctx.GetService<IConfiguration>()
        
        if apps.Length > 0 then
            let claim = ExpandoObject() :> IDictionary<string, obj>
            let claimName = sprintf "extension_%s_Applications" (config.["GraphApi:ClientId"].Replace("-", ""))
            
            let claimValue = apps
                                   |> List.filter(fun a -> a.Code.IsSome)
                                   |> List.map(fun a -> a.Code.Value)
                                   |> String.concat(",")
            
            claim.Add(claimName, claimValue)
            let body = JsonConvert.SerializeObject(claim)
            let api = sprintf "/users/%s" (string <| getValue user.ObjectId)
            do! sendPATCHGraphApiWithConfigRequest body ctx api
            return () |> Ok
        else
            return () |> Ok
    }

let redeemed = parseAndValidateToken
               >==> getEmail
               >==> getLocalUserByEmail
               >==> updateUserObjectId
               >==> getFi
               >==> assignedTheUserToAzureFiGroup
               >==> updatingUserApplicationClaim
               
let deleteAzureUserIfSomethingGoesWrong (ctx: HttpContext) =
    task {
        let config = ctx.GetService<IConfiguration>()
        let oid = getClaim ClaimTypes.NameIdentifier ctx
        let api = sprintf "%s/users/%s" config.["GraphApi:ApiVersion"] oid.Value
        do! sendDELETEGraphApiWithConfigRequest ctx api
    }
               
let redeemAndRedirect  next ctx = 
    task {
        match! withTransaction redeemed ctx with
            | Ok _ ->
                let config = ctx.GetService<IConfiguration>()
                return! redirectTo true config.["AfterUserCreationRedirectUrl"] next ctx
            | Error exp ->
                do! deleteAzureUserIfSomethingGoesWrong ctx
                return! handleErrorJsonAPI exp next ctx
    }

let validationGetRoutes: HttpHandler list = [
    routeCi "/validation/invitation" >=> invitation
]

let validationPostRoutes: HttpHandler list = [
    routeCi "/validation/redeemed" >=> redeemAndRedirect
]