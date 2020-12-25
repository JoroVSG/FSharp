module App.DTOs.ActivationKey

open Newtonsoft.Json

type ActivationKey = {
    Email: string
    InstitutionId: string
    IsFiAdmin: bool
    RoutingNumber: string
    Apps: string
}
with override this.ToString() = JsonConvert.SerializeObject(this)


type ActivationKeyWrapper = {
    DisplayName: string
    FirstName: string
    LastName: string
    Phone: string
    ActivationKeyEncrypted:string
}
with override this.ToString() = JsonConvert.SerializeObject(this)