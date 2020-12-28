using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crypto.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Crypto
{
    public static class AzureAdB2CAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureAdB2C(this AuthenticationBuilder builder) => builder.AddAzureAdB2C(_ => { });

        public static AuthenticationBuilder AddAzureAdB2C(this AuthenticationBuilder builder, Action<AzureAdB2COptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectOptionsSetup>();
            builder.AddOpenIdConnect();
            return builder;
        }

        public class OpenIdConnectOptionsSetup : IConfigureNamedOptions<OpenIdConnectOptions>
        {

            public OpenIdConnectOptionsSetup(IOptions<AzureAdB2COptions> b2cOptions)
            {
                AzureAdB2COptions = b2cOptions.Value;
            }

            private AzureAdB2COptions AzureAdB2COptions { get; }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ClientId = AzureAdB2COptions.ClientId;
                options.Authority = AzureAdB2COptions.Authority;
                options.UseTokenLifetime = true;
                options.ClientSecret = AzureAdB2COptions.ClientSecret;
                options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = "name" };
                options.MetadataAddress = $"{AzureAdB2COptions.Authority}/.well-known/openid-configuration";
                options.ProtocolValidator = new OpenIdConnectProtocolValidator { RequireNonce = false, RequireState = false };
                options.ResponseType = OpenIdConnectResponseType.CodeIdTokenToken;
                options.SaveTokens = true;

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = OnRedirectToIdentityProviderAsync,
                    OnRemoteFailure = OnRemoteFailureAsync,
                    OnAuthorizationCodeReceived = OnAuthorizationCodeReceivedAsync
                };
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }

            private async Task OnRedirectToIdentityProviderAsync(RedirectContext context)
            {
                var defaultPolicy = AzureAdB2COptions.DefaultPolicy;
                if (context.Properties.Items.TryGetValue(AzureAdB2COptions.PolicyAuthenticationProperty, out var policy) &&
                    !policy.Equals(defaultPolicy))
                {
                    context.ProtocolMessage.Scope = $"{AzureAdB2COptions.IcScope} {OpenIdConnectScope.OpenIdProfile}";
                    context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdTokenToken;
                    context.ProtocolMessage.RedirectUri = context.Properties.RedirectUri;
                    context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.ToLower().Replace(defaultPolicy.ToLower(), policy.ToLower());
                    context.Properties.Items.Remove(AzureAdB2COptions.PolicyAuthenticationProperty);
                }
                else
                {
                    context.ProtocolMessage.Scope = AzureAdB2COptions.ApiScopes.Replace("{0}", AzureAdB2COptions.IcScope);

                    var instancePolicyClaims = new List<Claim>();

                    if (context.Properties.Items.ContainsKey("verified_email"))
                    {
                        var verifiedEmail = new Claim("verified_email", context.Properties.Items["verified_email"] ?? string.Empty);
                        instancePolicyClaims.Add(verifiedEmail);
                        context.Properties.Items.Remove("verified_email");
                    }

                    if (context.Properties.Items.ContainsKey("activationKey"))
                    {
                        var activationKey = new Claim("extension_ActivationKey", context.Properties.Items["activationKey"] ?? string.Empty);
                        instancePolicyClaims.Add(activationKey);
                        context.Properties.Items.Remove("activationKey");
                    }

                    if (context.Properties.Items.ContainsKey("display_name") && context.Properties.Items["display_name"] != null)
                    {
                        var displayName = new Claim("name", context.Properties.Items["display_name"]);
                        instancePolicyClaims.Add(displayName);
                        context.Properties.Items.Remove("display_name");
                    }

                    if (context.Properties.Items.ContainsKey("first_name") && context.Properties.Items["first_name"] != null)
                    {
                        var firstName = new Claim("given_name", context.Properties.Items["first_name"]);
                        instancePolicyClaims.Add(firstName);
                        context.Properties.Items.Remove("first_name");
                    }

                    if (context.Properties.Items.ContainsKey("last_name") && context.Properties.Items["last_name"] != null)
                    {
                        var lastName = new Claim("family_name", context.Properties.Items["last_name"]);
                        instancePolicyClaims.Add(lastName);
                        context.Properties.Items.Remove("last_name");
                    }

                    if (context.Properties.Items.ContainsKey("phone") && context.Properties.Items["phone"] != null)
                    {
                        var phone = new Claim("strongAuthenticationPhoneNumber", context.Properties.Items["phone"]);
                        instancePolicyClaims.Add(phone);
                        context.Properties.Items.Remove("phone");
                    }

                    var policyClaims = new List<Claim>();

                    if (instancePolicyClaims.Any())
                    {
                        policyClaims.AddRange(instancePolicyClaims);
                    }

                    if (policyClaims.Any())
                    {
                        var configuration = await context.Options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);

                        var selfIssuedToken = CreateSelfIssuedToken(
                            configuration.Issuer,
                            context.ProtocolMessage.RedirectUri,
                            new TimeSpan(7, 0, 0, 0),
                            AzureAdB2COptions.SignInSecret,
                            policyClaims);


                        context.ProtocolMessage.Parameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
                        context.ProtocolMessage.Parameters.Add("client_assertion", selfIssuedToken);
                        context.ProtocolMessage.RedirectUri = context.Properties.RedirectUri;
                    }
                }
            }

            private static Task OnRemoteFailureAsync(RemoteFailureContext context)
            {
                context.HandleResponse();

                //var logger = LogManager.GetLogger(GetType().FullName);
                //logger?.Error(context.Failure);

                if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("access_denied"))
                {
                    context.Response.Redirect("/");
                }
                else
                {
                    
                    context.Response.Redirect($"api/validation/error?message={context.Failure?.Message}");
                }
                return Task.FromResult(0);
            }

            private async Task OnAuthorizationCodeReceivedAsync(AuthorizationCodeReceivedContext context)
            {
                var code = context.ProtocolMessage.Code;

                var signedInUserId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //var logger = LogManager.GetLogger(GetType().FullName);

                var userTokenCache = new MSALSessionCache(signedInUserId, context.HttpContext).GetMsalCacheInstance();
                var cca = new ConfidentialClientApplication(AzureAdB2COptions.ClientId, AzureAdB2COptions.Authority, AzureAdB2COptions.RedirectUri, new ClientCredential(AzureAdB2COptions.ClientSecret), userTokenCache, null);
                
                AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, new[] { AzureAdB2COptions.IcScope });

                context.HandleCodeRedemption(result.AccessToken, result.IdToken);
            }
        }

        private static string CreateSelfIssuedToken(
            string issuer,
            string audience,
            TimeSpan expiration,
            string signingSecret,
            IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var nowUtc = DateTime.UtcNow;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret));
            var signingCredentials = new SigningCredentials(key, "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = audience,
                Expires = nowUtc.Add(expiration),
                IssuedAt = nowUtc,
                Issuer = issuer,
                NotBefore = nowUtc,
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(claims)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        

        internal class UserGroup
        {
            [JsonProperty("@odata.id")]
            public string OdataId { get; set; }

            public override string ToString() => JsonConvert.SerializeObject(this);
        }
    }
}