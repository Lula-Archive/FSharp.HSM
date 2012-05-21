﻿module TestHSM 

open FSharp.HSM
open System
open NUnit.Framework
open FsUnit
    
type State = 
    | S0
    | S1
    | S11
    | S2
    | S21
    | S211

type Sig =
    | A 
    | B 
    | C 
    | D
    | E
    | F
    | G
    | H

type ComplexHSM() = 
  let mutable foo = false
  let hsm = 
    new StateMachine<State,Sig>(
        [ configure S0
            |> onEntry (fun _ -> printfn "Enter S0")
            |> onExit (fun _ -> printfn "Exit S0")
            |> transitionTo S1
            |> on E S211
          configure S1
            |> onEntry (fun _ -> printfn "Enter S1")
            |> onExit (fun _ -> printfn "Exit S1")
            |> substateOf S0
            |> transitionTo S11
            |> on A S1 
            |> on B S11
            |> on C S211 
            |> on D S0 
            |> on F S211 
          configure S11
            |> onEntry (fun _ -> printfn "Enter S11")
            |> onExit (fun _ -> printfn "Exit S11")
            |> substateOf S1
            |> on G S211 
            //removing for now
            //|> actionIf H (fun _ -> foo) (fun _ -> printfn "fooFal"; foo <- false;)
          configure S2
            |> onEntry (fun _ -> printfn "Enter S2")
            |> onExit (fun _ -> printfn "Exit S2")
            |> substateOf S0
            |> on C S1 
            |> on F S11 
          configure S21
            |> onEntry (fun _ -> printfn "Enter S21")
            |> onExit (fun _ -> printfn "Exit S21")
            |> substateOf S2
            |> transitionTo S211
            |> on B S211 
            |> handleIf H (fun _ -> not foo) (fun _ _ -> printfn "fooTru"; foo <- true; S21 )
          configure S211
            |> onEntry (fun _ -> printfn "Enter S211")
            |> onExit (fun _ -> printfn "Exit S211")
            |> substateOf S21
            |> on D S21
            |> on G S0 ] ) 
  member this.Hsm with get() = hsm

let fire (hsm:StateMachine<State,Sig>) signal = 
    printfn "fire %A" signal
    hsm.Fire(signal)

//todo:  add checks for state, entry, exits...
[<Test>]
let HsmTest() = 
    let hsm = (new ComplexHSM()).Hsm
    hsm.Init S0
    fire hsm A
    fire hsm E
    fire hsm E
    //error
    try
      fire hsm A
    with 
      | NoTransition -> ()
    fire hsm H
    //we should not exit S2 and S0 here... plus we should enter S21
    //foo should be set and not allow next transition...
    fire hsm H
    fire hsm G
    //not doing actions for now
    //fire hsm H
    //fire hsm H

      