module Persistence.Data.AnimalData
open DataContext
open FSharp.Data.Sql

type AnimalType = {
   AnimalId: int;
   Name: string
}

let animalAsync =
   async {
       let! res =
           query {
               for animal in RodeoContext.Dbo.Animal do
               select animal
           } |> Seq.executeQueryAsync
       let mapped = res |> Seq.map(fun animal -> animal.MapTo<AnimalType>())
       return mapped
   } |> Async.StartAsTask

let animals = 
    query {
       for animal in RodeoContext.Dbo.Animal do
       select animal
    } |> Seq.map(fun animal -> animal.MapTo<AnimalType>())
