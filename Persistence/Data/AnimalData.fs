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
               select (animal.AnimalId, animal.Name)
           } |> Seq.executeQueryAsync
       let mapped = res |> Seq.map(fun (id, name) -> { AnimalId = id; Name = name })
       return mapped
   }
