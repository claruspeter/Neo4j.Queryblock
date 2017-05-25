// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#I @"build"
#r "Neo4j.Queryblock"
#r "Neo4jClient"

open System
open Neo4jClient
open Neo4j.Queryblock

let b=2