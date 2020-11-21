module Persistence.Data.AnimalData
open DataContext
open FSharp.Data.Sql

//let animalAsync =
//   async {
//       let! res =
//           query {
//               for animal in RodeoContext.Dbo.Animal do
//               select (animal.AnimalId, animal.Name)
//           } |> Seq.executeQueryAsync
//       let mapped = res |> Seq.map(fun (id, name) -> {| AnimalId = id; Name = name |})
//       return mapped
//   }

//let animalAsync =
//   async {
//       let! res =
//           query {
//               for qe in RodeoContext.IbClue.Loan do
//               select (qe.``IbClue.Loan by IDLoan``.``IbClue.Loan by ``, qe.``IbClue.File by IDFile``)
//           } |> Seq.executeQueryAsync
//       let mapped = res |> Seq.map(fun (cn, cv, pn) -> {| ColumnName = cn; ColumnValues = Seq.map (fun kv -> {|   |} ) cv; PropertyName = pn |})
//       return mapped
//   }