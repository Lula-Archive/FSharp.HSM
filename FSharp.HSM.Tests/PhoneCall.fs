﻿module PhoneCallTest

open FSharp.HSM
open System
open NUnit.Framework
open FsUnit
open TestHelpers

type State = 
    | OffHook
    | Ringing
    | Connected //composite
    | InCall
    | OnHold

type Trigger =
    | CallDialed 
    | HungUp 
    | CallConnected 
    | PlacedOnHold
    | TakenOffHold

let mutable timerOn = false
let startTimer() = 
    printfn "%A connected" DateTime.Now
    timerOn <- true
let stopTimer() = 
    printfn "%A ended" DateTime.Now
    timerOn <- false

let newPhoneCall() = 
  [ configure OffHook
      |> on CallDialed Ringing
    configure Ringing
      |> on CallConnected Connected
    configure Connected
      |> onEntry startTimer
      |> onExit stopTimer
      |> transitionTo InCall
      |> on HungUp OffHook
    configure InCall
      |> substateOf Connected
      |> on PlacedOnHold OnHold
    configure OnHold
      |> substateOf Connected
      |> on TakenOffHold InCall ] 
  |> create

let check phoneCall trueStates falseStates (timer: bool) = 
    timerOn |> should equal timer
    trueStates |> List.iter (isInState phoneCall)
    falseStates |> List.iter (isNotInState phoneCall)

[<Test>]
let ``Dial -> Connect -> HangUp``() =
    let call = newPhoneCall()
    attachShow call
    call.Init OffHook
    fire call CallDialed
    check call [ Ringing ] [Connected; OnHold; OffHook ] false
      
    fire call CallConnected
    check call [ Connected ] [Ringing; OnHold; OffHook ] true

    fire call HungUp
    check call [ OffHook; ] [Ringing; Connected; OnHold ] false

[<Test>]
let ``Dial -> Connect -> Hold -> HangUp``() =
    let call = newPhoneCall()
    attachShow call
    call.Init OffHook
    fire call CallDialed
    check call [ Ringing ] [Connected; OnHold; OffHook; InCall ] false
      
    fire call CallConnected
    check call [ Connected; InCall ] [Ringing; OnHold; OffHook ] true
      
    fire call PlacedOnHold
    check call [ Connected; OnHold ] [Ringing; OffHook; InCall ] true
      
    //i should be able to hang up here based on Connected state
    fire call HungUp
    check call [ OffHook; ] [Ringing; Connected; OnHold; InCall ] false

[<Test>]
let ``Dial -> Connect -> Hold -> UnHold -> HangUp``() =      
    let call = newPhoneCall()
    attachShow call
    call.Init OffHook
    fire call CallDialed
    check call [ Ringing ] [Connected; OnHold; OffHook; InCall ] false

    fire call CallConnected
    check call [ Connected; InCall ] [Ringing; OnHold; OffHook ] true

    fire call PlacedOnHold
    check call [ Connected; OnHold ] [Ringing; OffHook; InCall ] true

    fire call TakenOffHold
    check call [ Connected; InCall] [Ringing; OnHold; OffHook ] true

    fire call HungUp
    check call [ OffHook; ] [Ringing; Connected; OnHold; InCall ] false

[<Test; ExpectedException(typeof<NoTransition>)>]
let ``Hold -> error``() =      
    let call = newPhoneCall()
    attachShow call
    call.Init OffHook
    fire call PlacedOnHold
      