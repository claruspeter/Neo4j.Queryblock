// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#I @"build"
#r "Neo4j.Queryblock"
#r "Neo4jClient"
#r "neo4jtypeprovider"

open System
open Neo4jClient
open Neo4j.Queryblock
open Neo4j.TypeProvider

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

type schema = Neo4j.TypeProvider.Schema<connectionstring, user, pwd>

type Neo4jDB =
    static member People : Query<schema.Person> = NA
    static member Movies : Query<schema.Movie> = NA

type qqq = { paras: string list; clause: string  }

let (-->) a b =
   (a,b)// { paras = [a;b]; clause= sprintf "(%s)-->(%s)" a b }


let q = 
  neo4j { 
    // let! p, m = db.Cypher.Match "(p:Person)-->(m:Movie)" |> (fun _ -> (1,{Movie.title=""; released=1973; tagline="hello!"}))
    // let! m = {Movie.title=""; released=1973; tagline="hello!"}
    // let! p = 42
    // printfn "hello"
    let! p,m = "p" --> "m"
    yield! p
  }


