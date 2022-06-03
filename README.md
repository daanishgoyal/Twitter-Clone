# Twitter-Clone


Video Description : https://youtu.be/30iD32zI3ek

## Part 1
Implemented a Twitter-like engine with the following functionality:
Register account
Send tweet. Tweets can have hashtags (e.g. #COP5615isgreat) and mentions (@bestuser)
Subscribe to user's tweets
Re-tweets (so that your subscribers get an interesting tweet you got by other means)
Allow querying tweets subscribed to, tweets with specific hashtags, tweets in which the user is mentioned (my mentions)
If the user is connected, deliver the above types of tweets live (without querying)


Implemented a tester/simulator to test the above
Simulate as many users as possible
Simulate periods of live connection and disconnection for users
Simulate a Zipf distribution on the number of subscribers. 
Other considerations:
The client part (send/receive tweets) and the engine (distribute tweets) have to be in separate processes.

## Part 2

Used WebSharper web framework to implement a WebSocket interface to the part I implementation.

Designed a JSON based API that represents all messages and their replies (including errors).
Re-written parts of engine using WebSharper to implement the WebSocket interface
Re-written parts of client to use WebSockets.


