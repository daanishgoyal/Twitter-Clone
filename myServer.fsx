#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration


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
                    port = 8777
                    hostname = localhost
                }
            }
        }")

let mysystem = ActorSystem.Create("RemoteFSharp", myConfig)



type Tweet(tweet_id:string, text:string, is_re_tweet:bool) =
    member this.tweet_id = tweet_id
    member this.text = text
    member this.is_re_tweet = is_re_tweet

    override this.ToString() =
      let mutable res = ""
      if is_re_tweet then
        res <- sprintf "[retweet][%s]%s" this.tweet_id this.text
      else
        res <- sprintf "[%s]%s" this.tweet_id this.text
      res


type User(user_name:string, password:string) =
    let mutable subscribes = List.empty: User list
    let mutable tweets = List.empty: Tweet list
    member this.user_name = user_name
    member this.password = password
    member this.addSubscribe x =
        subscribes <- List.append subscribes [x]
    member this.getSubscribes() =
        subscribes
    member this.addTweet x =
        tweets <- List.append tweets [x]
    member this.getTweets() =
        tweets
    override this.ToString() = 
       this.user_name
       

type Twitter() =
    let mutable tweets = new Map<string,Tweet>([])
    let mutable users = new Map<string,User>([])
    let mutable hashtags = new Map<string, Tweet list>([])
    let mutable mentions = new Map<string, Tweet list>([])
    member this.AddTweet (tweet:Tweet) =
        tweets <- tweets.Add(tweet.tweet_id,tweet)
    member this.AddUser (user:User) =
        users <- users.Add(user.user_name, user)
    member this.AddToHashTag hashtag tweet =
        let key = hashtag
        let mutable map = hashtags
        if map.ContainsKey(key)=false
        then
            let l = List.empty: Tweet list
            map <- map.Add(key, l)
        let value = map.[key]
        map <- map.Add(key, List.append value [tweet])
        hashtags <- map
    member this.AddToMention mention tweet = 
        let key = mention
        let mutable map = mentions
        if map.ContainsKey(key)=false
        then
            let l = List.empty: Tweet list
            map <- map.Add(key, l)
        let value = map.[key]
        map <- map.Add(key, List.append value [tweet])
        mentions <- map
    member this.register username password =
        let mutable res = ""
        if users.ContainsKey(username) then
            res <- "error, username already exist"
        else
            let user = new User(username, password)
            this.AddUser user
            user.addSubscribe user
            res <- "Register success username: " + username + "  password: " + password
        res
    member this.SendTweet username password text is_re_tweet =
        let mutable res = ""
        if not (this.authentication username password) then
            res <- "error, authentication fail"
        else
            if users.ContainsKey(username)=false then
                res <-  "error, no this username"
            else
                let tweet = new Tweet(System.DateTime.Now.ToFileTimeUtc() |> string, text, is_re_tweet)
                let user = users.[username]
                user.addTweet tweet
                this.AddTweet tweet
                let idx1 = text.IndexOf("#")
                if not (idx1 = -1) then
                    let idx2 = text.IndexOf(" ",idx1)
                    let hashtag = text.[idx1..idx2-1]
                    this.AddToHashTag hashtag tweet
                let idx1 = text.IndexOf("@")
                if not (idx1 = -1) then
                    let idx2 = text.IndexOf(" ",idx1)
                    let mention = text.[idx1..idx2-1]
                    this.AddToMention mention tweet
                res <-  "[success] sent twitter: " + tweet.ToString()
        res
    member this.authentication username password =
            let mutable res = false
            if not (users.ContainsKey(username)) then
                printfn "%A" "error, no this username"
            else
                let user = users.[username]
                if user.password = password then
                    res <- true
            res
    member this.getUser username = 
        let mutable res = new User("","")
        if not (users.ContainsKey(username)) then
            printfn "%A" "error, no this username"
        else
            res <- users.[username]
        res
    member this.subscribe username1 password username2 =
        let mutable res = ""
        if not (this.authentication username1 password) then
            res <- "error, authentication fail"
        else
            let user1 = this.getUser username1
            let user2 = this.getUser username2
            user1.addSubscribe user2
            res <- "[success] " + username1 + " subscribe " + username2
        res
    member this.reTweet username password text =
        let res = "[retweet]" + (this.SendTweet username password text true)
        res
    member this.queryTweetsSubscribed username password =
        let mutable res = ""
        if not (this.authentication username password) then
            res <- "error, authentication fail"
        else
            let user = this.getUser username
            let res1 = user.getSubscribes() |> List.map(fun x-> x.getTweets()) |> List.concat |> List.map(fun x->x.ToString()) |> String.concat "\n"
            res <- "[success] queryTweetsSubscribed" + "\n" + res1
        res
    member this.queryHashTag hashtag =
        let mutable res = ""
        if not (hashtags.ContainsKey(hashtag)) then
            res <- "error, no this hashtag"
        else
            let res1 = hashtags.[hashtag] |>  List.map(fun x->x.ToString()) |> String.concat "\n"
            res <- "[success] queryHashTag" + "\n" + res1
        res
    member this.queryMention mention =
        let mutable res = ""
        if not (mentions.ContainsKey(mention)) then
            res <- "error, no this mention"
        else
            let res1 = mentions.[mention] |>  List.map(fun x->x.ToString()) |> String.concat "\n"
            res <-  "[success] queryMention" + "\n" + res1
        res
    override this.ToString() =
        "print the entire Twitter"+ "\n" + tweets.ToString() + "\n" + users.ToString() + "\n" + hashtags.ToString() + "\n" + mentions.ToString()
        
    
let twitter = new Twitter()
type MessagePack_reg = MessagePack1 of  string  * string  * string* string
type MessagePack_send = MessagePack2 of  string  * string  * string* string* bool
type MessagePack_subscribe = MessagePack3 of  string  * string  * string* string 
type MessagePack_retweets = MessagePack4 of  string  * string  * string * string
type MessagePack_queryTweetsSubscribed = MessagePack5 of  string  * string  * string 
type MessagePack_hashtag = MessagePack6 of  string  * string   
type MessagePack_at = MessagePack7 of  string  * string  


let registerActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MessagePack1(POST,register,username,password) ->
            let res = twitter.register username password
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let sendActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        let sender_path = mailbox.Sender().Path.ToStringWithAddress()
        match message  with
        |   MessagePack2(POST,username,password,tweet_content,false) -> 
            let res = twitter.SendTweet username password tweet_content false
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let subscribeActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MessagePack3(POST,username,password,target_username) -> 
            let res = twitter.subscribe username password target_username
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let retweetActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MessagePack4(POST,username,password,tweet_content) -> 
            let res = twitter.reTweet  username password tweet_content
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let queryActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MessagePack5(POST,username,password ) -> 
            let res = twitter.queryTweetsSubscribed  username password
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let hashtagActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MessagePack6(POST,queryhashtag) -> 
            let res = twitter.queryHashTag  queryhashtag
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let atrateActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MessagePack7(POST,at) -> 
            let res = twitter.queryMention  at
            sender <? res |> ignore
        | _ ->  failwith "unknown message"
        return! loop()     
    }
    loop ()

let mutable opt= "reg" 
let mutable POST="POST"
let mutable username="user2"
let mutable password="123"
let mutable register="register"
let mutable target_username="user1"
let mutable tweet_content="Today is a bad day!"
let mutable queryhashtag="#Trump"
let mutable at="@Biden"

type MessagePack_processor = MessagePack8 of  string  * string * string* string* string * string* string* string * string

let myRegAct = spawn mysystem "processor1" registerActor
let mySendAct = spawn mysystem "processor2" sendActor
let mySubAct = spawn mysystem "processor3" subscribeActor
let myRtAct = spawn mysystem "processor4" retweetActor
let myQryAct = spawn mysystem "processor5" queryActor 
let myHshTgAct = spawn mysystem "processor6" hashtagActor
let myatAct = spawn mysystem "processor7" atrateActor


let msgReceiveActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match box message with
        | :? string   ->
            if message="" then
                return! loop() 
            printfn "%s" ""
            printfn "[message received] %s" message
            let result = message.Split ','
            let mutable opt= result.[0]
            let mutable POST=result.[1]
            let mutable username=result.[2]
            let mutable password=result.[3]
            let mutable target_username=result.[4]
            let mutable tweet_content=result.[5]
            let mutable queryhashtag=result.[6]
            let mutable at=result.[7]
            let mutable register=result.[8]
            let mutable task = myRegAct <? MessagePack1("","","","")

            if opt= "reg" then
                printfn "[Register] username:%s password: %s" username password
                task <- myRegAct <? MessagePack1(POST,register,username,password)

            if opt= "send" then
                printfn "[send] username:%s password: %s tweet_content: %s" username password tweet_content
                task <- mySendAct <? MessagePack2(POST,username,password,tweet_content,false)

            if opt= "subscribe" then
                printfn "[subscribe] username:%s password: %s subscribes username: %s" username password target_username
                task <- mySubAct <? MessagePack3(POST,username,password,target_username )

            if opt= "retweet" then
                printfn "[retweet] username:%s password: %s tweet_content: %s" username password tweet_content
                task <- myRtAct <? MessagePack4(POST,username,password,tweet_content)

            if opt= "querying" then
                printfn "[querying] username:%s password: %s" username password
                task <- myQryAct <? MessagePack5(POST,username,password )

            if opt= "#" then
                printfn "[#Hashtag] %s: " queryhashtag
                task <- myHshTgAct <? MessagePack6(POST,queryhashtag )

            if opt= "@" then
                printfn "[@mention] %s" at
                task <- myatAct <? MessagePack7(POST,at )
            let response = Async.RunSynchronously (task, 1000)
            sender <? response |> ignore
            printfn "[Result]: %s" response
        return! loop()     
    }
    loop ()
let act_msgReceived = spawn mysystem "EchoServer" msgReceiveActor


act_msgReceived <? "" |> ignore
printfn "------------------------------------------------- \n " 
printfn "Twitter Server is running...   " 



Console.ReadLine() |> ignore

printfn "-----------------------------------------------------------\n" 
0