module Domains.B2CUserResponse

open Newtonsoft.Json
open Domains.B2CUser

type B2CResponse = {
    [<JsonProperty("value")>] B2CGraphUsers: B2CUser list
}