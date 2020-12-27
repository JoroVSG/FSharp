module Migrations.Up

open System
open System.Reflection
open DbUp

type ConsoleMessage = ConsoleException of Exception | Message of string
let printToConsole color (message: ConsoleMessage) =
    Console.ForegroundColor <- color
    match message with
        | ConsoleException ex -> Console.WriteLine(ex)
        | Message str -> Console.WriteLine(str)
    Console.ResetColor()
let errorToConsole = printToConsole ConsoleColor.Red
let successToConsole = printToConsole ConsoleColor.Green
    
let migrateDb = fun (connectionString: string option) ->
    match connectionString with
        | Some connStr ->
            let upgrader =
                DeployChanges
                    .To
                    .SqlDatabase(connStr)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToConsole()
                    .Build()
            
            let result = upgrader.PerformUpgrade()
            
            if (result.Successful = false) then
                errorToConsole <| ConsoleException(result.Error)
                Console.ReadLine() |> ignore
            else
                successToConsole <| Message("Success!")
        | None ->
            errorToConsole <| Message("No connection string provided!")
            Console.ReadLine() |> ignore 
        
            


