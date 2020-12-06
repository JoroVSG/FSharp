module Domains.Common.CommonTypes

open System

type OperationStatus = {
    Id: Guid
    Success: bool
    Exception: Exception option
}

type MapColumn(fieldName: string) =
    inherit Attribute()
    member __.FieldName = fieldName 