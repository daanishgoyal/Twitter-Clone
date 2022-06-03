
open System
open Akka.Actor
open Akka.Actor
open Akka.Configuration
open Akka.Dispatch.SysMsg
open Akka.FSharp
open System.Threading
open FSharp.Data
open System.Text.RegularExpressions
open FSharp.Data.HttpRequestHeaders
type MessagePack_processor = MessagePack8 of  string  * string * string* string* string * string* string* string * string


let mutable N = 1000


let configuration = 
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
let mysystem = ActorSystem.Create("RemoteFSharp", configuration)

let echoServer = mysystem.ActorSelection(
                            "akka.tcp://RemoteFSharp@localhost:8080/user/EchoServer")


let mutable prev_query = ""

let sendCommand cmd =
    let json = " {\"command\":\"" + cmd + "\"} "
    let response = Http.Request(
        "http://127.0.0.1:8080/twitter",
        httpMethod = "POST",
        headers = [ ContentType HttpContentTypes.Json ],
        body = TextRequest json
        )
    let response = string response.Body
    response

let mutable auto = false


let myActUserConnect (mailbox: Actor<_>) = 
    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        | username,password ->
            while not auto do
                Thread.Sleep(500)
            let cmd = "querying, ," + username + "," + password + ", , , , , "
            let response = sendCommand cmd
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
        let response = sendCommand cmd
        printfn "[Reply]%s" response
        printfn "%s" ""
        return! loop()     
    }
    loop ()

let clientUser = spawn mysystem "clientUser" userActor

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    printfn "------------------------------------------------- \n " 
    printfn "Please type functions to use:   " 
    clientUser <? "go" |>ignore
    Thread.Sleep(1000000)
    mysystem.Terminate() |> ignore

    0 
