// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#I @"build"
#r "Neo4j.Queryblock"
#r "Neo4jClient"

open System
open Neo4jClient
open Neo4j.Queryblock

[<Literal>]
let connectionstring = @"http://localhost:7474/db/data"
[<Literal>]
let user = "neo4j" 
[<Literal>]
let pwd = "password"
let db = 
    let db = new Neo4jClient.GraphClient(Uri(connectionstring),user ,pwd)
    db.Connect()
    db

type Neo4jDB =
    static member People : Query<Person> = NA
    static member Movies : Query<Movie> = NA

let q = 
  neo4j { 
    let m = "(m:Movie)"
    return m
  }


