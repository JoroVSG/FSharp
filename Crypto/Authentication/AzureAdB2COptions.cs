namespace Crypto.Authentication
{
    public class AzureAdB2COptions
    {
        public const string PolicyAuthenticationProperty = "Policy";
        
        public string ClientId { get; set; }
        
        private string FullyQualifiedTenantName => $"{Tenant}.onmicrosoft.com";
        public string SignInSecret { get; set; }

        private string AzureAdB2CInstance => $"https://{Tenant}.b2clogin.com/";

        public string Tenant { get; set; }
        public string InvitePolicyId { get; set; }
        public string RedirectUri { get; set; }

        public string DefaultPolicy => InvitePolicyId;
        public string Authority => $"{AzureAdB2CInstance}/{FullyQualifiedTenantName}/{DefaultPolicy}/v2.0";

        public string ClientSecret { get; set; }
        public string ApiScopes { get; set; }
        public string IcScope { get; set; }
    }
}