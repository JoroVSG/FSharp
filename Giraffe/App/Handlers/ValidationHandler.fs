module App.Handlers.ValidationHandler

open App.DTOs.ActivationKey
open Crypto
open Giraffe
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open App.Helpers.HelperFunctions
open Microsoft.Extensions.Configuration
open Newtonsoft.Json

let invitation = fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
        let crypto = ctx.GetService<ICryptoService>()
        let config = ctx.GetService<IConfiguration>()
        let activationKey =
            match ctx.TryGetQueryStringValue "activationKey" with
                | Some ak -> ak
                | None -> ""
        
        let decoded = decodeBase64 activationKey
        
        let wrapper = JsonConvert.DeserializeObject<ActivationKeyWrapper>(decoded);
        
        let encryptedJson = crypto.Decrypt(wrapper.ActivationKeyEncrypted, config.["SecretPassPhrase"]);
            
        let key = JsonConvert.DeserializeObject<ActivationKey>(encryptedJson)
        
        // let url = sprintf $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}/api/validation/redeemed";
        let url = sprintf "%s://%s%s/api/validation/redeemed"
                           ctx.Request.Scheme
                           (string ctx.Request.Host)
                           (string ctx.Request.PathBase)

        let properties = AuthenticationProperties (RedirectUri = url);
        

        properties.Items.["Policy"] = config.["Authentication:AzureAdB2C:InvitePolicyId"] |> ignore
        properties.Items.["verified_email"] = key.Email |> ignore
        properties.Items.["display_name"] = wrapper.DisplayName |> ignore
        properties.Items.["first_name"] = wrapper.FirstName |> ignore
        properties.Items.["last_name"] = wrapper.LastName |> ignore
        properties.Items.["phone"] = wrapper.Phone |> ignore

        properties.Items.["activationKey"] = wrapper.ActivationKeyEncrypted |> ignore
        let schema = OpenIdConnectDefaults.AuthenticationScheme
        
        do! ctx.ChallengeAsync(schema, properties = properties)
        
        return! next ctx
    }