[<AutoOpen>]
module Fake.MailBoxHelper

type MailboxMessage =
| Die
| Message of TraceData
| ProcessAll of AsyncReplyChannel<unit>

let internal buffer = MailboxProcessor.Start (fun inbox ->
    let rec loop () = 
        async {
            let! msg = inbox.Receive()
            match msg with
            | Die -> return ()
            | Message x -> 
                listeners.ForEach (fun listener -> listener.Write x)
                return! loop ()
            | ProcessAll reply ->
                reply.Reply()
                return! loop () }

    loop ())

let postMessage msg = buffer.Post (Message msg)

/// Checks if the current message queue is empty
let MessageBoxIsEmpty() = buffer.CurrentQueueLength = 0