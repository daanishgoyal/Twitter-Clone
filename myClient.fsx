#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.Actor
open Akka.Configuration
open Akka.Dispatch.SysMsg
open Akka.FSharp
open System.Threading
type MessagePack_processor = MessagePack8 of  string  * string * string* string* string * string* string* string * string


let mutable N = 1000


let myConfig = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = "+string (System.Random( ).Next(10000,20000))+" 
                    hostname = localhost
                }
            }
        }")
let mysystem = ActorSystem.Create("RemoteFSharp", myConfig)

let echoServer = mysystem.ActorSelection(
                            "akka.tcp://RemoteFSharp@localhost:8777/user/EchoServer")


let mutable prev_query = ""
let mutable auto = false
let myActUserConnect (mailbox: Actor<_>) = 
    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        | username,password ->
            while not auto do
                Thread.Sleep(500)
            let task = echoServer <?  "querying, ," + username + "," + password + ", , , , , "
            let response = Async.RunSynchronously (task, 1000) |> string
            if not (response = prev_query) then
                prev_query <- response
                printfn "[AutoQuery]%s" response
                printfn "%s" ""
            Thread.Sleep(1000)
            mailbox.Self <? (username, password) |> ignore
            return! loop() 
    }
    loop ()
    
let connectCliUser = spawn mysystem "connectCliUser" myActUserConnect

let userActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        

        let cmd = Console.ReadLine()
        let result = cmd.Split ','
        let opt = result.[0]
        if opt="connect" then
            let username=result.[2]
            let password=result.[3]
            auto <- true
            connectCliUser <? (username, password) |> ignore
            return! loop() 
        else if opt="disconnect" then
            auto <- false
            return! loop() 
        let task = echoServer <? cmd
        let response = Async.RunSynchronously (task, 1000)
        printfn "[Reply]%s" (string(response))
        printfn "%s" ""
        return! loop()     
    }
    loop ()

let clientUser = spawn mysystem "clientUser" userActor


printfn "------------------------------------------------- \n " 
printfn "Please type functions to use:   " 
clientUser <? "go" |>ignore
Thread.Sleep(1000000)
mysystem.Terminate() |> ignore

0 
