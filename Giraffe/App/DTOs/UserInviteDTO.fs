module App.DTOs.UserInviteDTO

open System
open App.DTOs.ApplicationDTO

type UserInviteDTO = {
    Id: Guid
    Email: string 
    DisplayName: string 
    FirstName: string 
    LastName: string 
    Phone: string 
    InstitutionId: string
    RoutingNumber: string
    IsFiAdmin: bool
    Applications: ApplicationDTO list
}
with member this.HasErrors() =
        if      this.FirstName.Length < 3  then Some "First name is too short."
        else if this.FirstName.Length > 50 then Some "First name is too long."
        else if this.LastName.Length  < 3  then Some "Last name is too short."
        else if this.LastName.Length  > 50 then Some "Last name is too long."
        else None