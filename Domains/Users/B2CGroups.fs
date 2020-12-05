module Domains.Users.B2CGroups

open System
open Newtonsoft.Json

[<CLIMutable>]
type B2CGroup = {
    [<JsonProperty("id")>]Id: Guid
    [<JsonProperty("displayName")>]DisplayName: string
    [<JsonProperty("mailEnabled")>]MailEnabled: bool
    [<JsonProperty("mailNickname")>]MailNickname: string
    [<JsonProperty("securityEnabled")>]SecurityEnabled: bool
}

type B2CGroups = {
    [<JsonProperty("value")>]Groups: B2CGroup list
}