module App.Mapping.ApplicationMapper

open App.DTOs.ApplicationDTO
open Domains.Applications.Application

let modelToDto = fun model ->
   let dto: ApplicationDTO =
      { Id = model.IdApplication
        Description = model.Description
        Name = model.Name
        Code = model.Code
        Rating = model.Rating
        IdApplication = model.IdApplication
        Type = "application" }
   dto

let dtoToModel = fun dto ->
    let model: Application =
        { IdApplication = dto.Id
          Description = dto.Description
          Name = dto.Name
          Code = dto.Code
          Image = None
          Rating = dto.Rating }
    model    

