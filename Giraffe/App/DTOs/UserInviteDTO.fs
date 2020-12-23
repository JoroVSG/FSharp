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
    IsFiAdmin: bool
    Applications: ApplicationDTO list
}