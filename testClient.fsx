#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote" 
#r "nuget: Akka.TestKit" 

open System
open System.Threading
open Akka.Actor
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
type MessagePack_processor = MessagePack8 of  string  * string * string* string* string * string* string* string * string

// number of user
let args : string array = fsi.CommandLineArgs |> Array.tail
let N= args.[0] |> int
let M = N
let mutable i = 0
let mutable ii = 0
let obj = new Object()
let addIIByOne() =
    Monitor.Enter obj
    ii<- ii+1
    Monitor.Exit obj
    
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
                    port = 8123
                    hostname = localhost
                }
            }
        }")
let mysystem = ActorSystem.Create("RemoteFSharp", myConfig)

let echoServer = mysystem.ActorSelection(
                            "akka.tcp://RemoteFSharp@localhost:8777/user/EchoServer")

let rand = System.Random(1)
let myactorUserRegister (mailbox: Actor<_>) = 
    let rec loop () = actor {
        let! message = mailbox.Receive()
        let idx = message
        let mutable opt = "reg"           
        let mutable POST = " "
        let mutable username = "user"+(string idx)
        let mutable password = "password" + (string idx)
        let mutable target_username = " "
        let mutable queryhashtag = " "
        let mutable at = " "
        let mutable tweet_content = " "
        let mutable register = " "
        let cmd = opt+","+POST+","+username+","+password+","+target_username+","+tweet_content+","+queryhashtag+","+at+","+register
        let task = echoServer <? cmd
        let response = Async.RunSynchronously (task, 1000)
        printfn "[command]%s" cmd
        printfn "[Reply]%s" (string(response))
        printfn "%s" ""
        addIIByOne()
        return! loop()
    }
    loop ()
let myactorClientSim (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        let idx = message
        match box message with
        | :? string   ->
            let mutable rand_num = Random( ).Next() % 7
            let mutable opt = "reg"           
            let mutable POST = "POST"
            let mutable username = "user"+(string idx)
            let mutable password = "password" + (string idx)
            let mutable target_username = "user"+rand.Next(N) .ToString()
            let mutable queryhashtag = "#topic"+rand.Next(N) .ToString()
            let mutable at = "@user"+rand.Next(N) .ToString()
            let mutable tweet_content = "tweet"+rand.Next(N) .ToString()+"... " + queryhashtag + "..." + at + " " 
            let mutable register = "register"
            if rand_num=0 then  opt <-"reg"
            if rand_num=1 then  opt <-"send"
            if rand_num=2 then  opt <-"subscribe"
            if rand_num=3 then  opt <-"retweet"
            if rand_num=4 then  opt <-"querying"
            if rand_num=5 then  opt <-"#"
            if rand_num=6 then  opt <-"@" 
            let cmd = opt+","+POST+","+username+","+password+","+target_username+","+tweet_content+","+queryhashtag+","+at+","+register
            let task = echoServer <? cmd
            let response = Async.RunSynchronously (task, 3000)
            printfn "[command]%s" cmd
            printfn "[Reply]%s" (string(response))
            printfn "%s" ""
            addIIByOne()
        return! loop()     
    }
    loop ()

let ClientUserRegister = spawn mysystem "ClientUserRegister" myactorUserRegister    
let ClientSimulator = spawn mysystem "ClientSimulator" myactorClientSim



printfn "------------------------------------------------- \n " 
printfn "Register Account...   " 

let stopWatch = System.Diagnostics.Stopwatch.StartNew()
i<-0
ii<-0
while i<N do
    ClientUserRegister <! string i |>ignore
    i<-i+1
while ii<N-1 do
    Thread.Sleep(50)
stopWatch.Stop()
let time_register = stopWatch.Elapsed.TotalMilliseconds





printfn "------------------------------------------------- \n " 
printfn "send tweet...   " 
let stopWatch1 = System.Diagnostics.Stopwatch.StartNew()
for i in 0..N-1 do
    for j in 0..10 do
        let cmd = "send, ,user"+(string i)+",password"+(string i)+", ,tweet+user"+(string i)+"_"+(string j)+"th @user"+(string (rand.Next(N)))+" #topic"+(string (rand.Next(N)))+" , , , "
        let task = echoServer <? cmd
        let response = Async.RunSynchronously (task, 3000)
        printfn "[command]%s" cmd
        printfn "[Reply]%s" (string(response))
        printfn "%s" ""
stopWatch1.Stop()
let time_send = stopWatch1.Elapsed.TotalMilliseconds





let mutable step = 1
let stopWatch2 = System.Diagnostics.Stopwatch.StartNew()
printfn "Zipf Subscribe --"  
for i in 0..N-1 do
    for j in 0..step..N-1 do
        if not (j=i) then
            let cmd = "subscribe, ,user"+(string j)+",password"+(string j)+",user"+(string i)+", , , , "
            let task = echoServer <? cmd
            let response = Async.RunSynchronously (task, 3000)
            printfn "[command]%s" cmd
            printfn "[Reply]%s" (string(response))
            printfn "%s" ""
        step <- step+1
stopWatch2.Stop()
let time_zipf_subscribe = stopWatch2.Elapsed.TotalMilliseconds
    


let stopWatch3 = System.Diagnostics.Stopwatch.StartNew()
for i in 0..N-1 do
    let cmd = "querying, ,user"+(string i)+",password"+(string i)+", , , , , "
    let task = echoServer <? cmd
    let response = Async.RunSynchronously (task, 5000)
    printfn "[command]%s" cmd
    printfn "[Reply]%s" (string(response))
    printfn "%s" ""
stopWatch3.Stop()
let time_query = stopWatch3.Elapsed.TotalMilliseconds



let stopWatch4 = System.Diagnostics.Stopwatch.StartNew()
for i in 0..N-1 do
    let cmd = "#, , , , , ,#topic"+(string (rand.Next(N)))+", ,"
    let task = echoServer <? cmd
    let response = Async.RunSynchronously (task, 3000)
    printfn "[command]%s" cmd
    printfn "[Reply]%s" (string(response))
    printfn "%s" ""
stopWatch4.Stop()
let time_hashtag = stopWatch4.Elapsed.TotalMilliseconds




let stopWatch5 = System.Diagnostics.Stopwatch.StartNew()
for i in 0..N-1 do
    let cmd = "@, , , , , , ,@user"+(string (rand.Next(N)))+","
    let task = echoServer <? cmd
    let response = Async.RunSynchronously (task, 3000)
    printfn "[command]%s" cmd
    printfn "[Reply]%s" (string(response))
    printfn "%s" ""
stopWatch5.Stop()
let time_mention = stopWatch5.Elapsed.TotalMilliseconds



printfn "------------------------------------------------- \n " 
printfn " %d Randon Ops and send tweet...   " M 
let stopWatch6 = System.Diagnostics.Stopwatch.StartNew()
i<-0
ii<-0
while i<M do
    ClientSimulator<! string (rand.Next(N)) |>ignore
    i <- i+1
while ii<M-1 do
    Thread.Sleep(50)
stopWatch6.Stop()
let time_random = stopWatch6.Elapsed.TotalMilliseconds


printfn "time to register %d users is %f" N time_register
printfn "time to send 10 tweets is %f" time_send
printfn "time to Zipf subscribe %d users is %f" N time_zipf_subscribe
printfn "time to query %d users is %f" N time_query
printfn "time to query %d hasgtag is %f" N time_hashtag
printfn "time to query %d mention is %f" N time_mention
printfn "time to %d random operations is %f" M time_random

printfn "Total Result: %f %f %f %f %f %f %f" time_register time_send time_zipf_subscribe time_query time_hashtag time_mention time_random


mysystem.Terminate() |> ignore
0 

