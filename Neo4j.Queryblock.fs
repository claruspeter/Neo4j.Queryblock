module Neo4j.Queryblock

open System
open System.Collections.Generic
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
//open Neo4jClient

type Query<'T> = NA


type SimpleNeo4jQueryBuilder() =
    member x.For(tz:Query<'T>, f:'T -> Query<'R>) : Query<'R> = NA
    member x.Yield(t:'T) : Query<'T> = NA

    member x.Quote(e:Expr<_>) = e

    [<CustomOperation("where", MaintainsVariableSpace=true)>]
    member x.Where ( source:Query<'T>, [<ProjectionParameter>] f:'T -> bool ) : Query<'T> = NA
    
    [<CustomOperation("selectAttrs")>]
    member x.SelectAttrs ( source:Query<'T>, [<ProjectionParameter>] f:'T -> 'R) : Query<'R> = NA
    
    [<CustomOperation("selectCount")>]
    member x.SelectCount(source:Query<'T>) : int = failwith "Never executed"

    [<CustomOperation("match")>]
    member x.Match(source:Query<'T>, [<ProjectionParameter>] f:'T -> 'R) : Query<'R> = NA


let neo4j = SimpleNeo4jQueryBuilder()

/// Defines a single condition of the form
/// p.<Property> <op> <Constant>
type QueryCondition = 
  { Property : string
    Operator : string 
    Constant : obj }

/// Specifies what kind of projection happens at the 
/// end of query (count or list of projected attributes)
type QueryProjection =
  | SelectAttributes of string list
  | SelectProps of string list
  | SelectCount

/// A query consits of source (table) name, a list of
/// filters specified using `where` and an optional 
/// projection at the end.
type Query = 
  { Source : string
    tTyp: Type
    rTyp: Type
    Where : QueryCondition list
    Select : QueryProjection option }

let private translateWhere = function
  | Lambda(var1, Call (None, op, [left; right])) ->
    match left, right with
    | PropertyGet(Some (Var var2), prop, []), Value(value, _) when 
        var1.Name = var2.Name && op.Name.StartsWith("op_") ->
        // We got 'where' that we understand. Build QueryCondition!
        { Property = prop.Name
          Operator = op.Name.Substring(3)
          Constant = value }
    | e -> 
      // 'where' with not supported format
      // (this can happen so report more useful error)
      failwithf 
        "%s\nGot: %A" 
        ( "Only expressions of the form " +
          "'p.Prop <op> <value>' are supported!") e

  // This should not happen - the parameter is always lambda!
  | something -> failwith (sprintf "Expected lambda expression but got %A" something)

/// Translate property access (may be in a tuple or not)
let private translatePropGet varName = function
  | PropertyGet(Some(Var v), prop, []) 
      // The body is simple projection
      when v.Name = varName -> prop.Name
  | e -> 
    // Too complex expression in projection
    failwithf 
      "%s\nGot: %A" 
      ( "Only expressions of the form " + 
        "'p.Prop' are supported!" ) e

/// Translate projection - this handles both of the forms 
/// (with or without tuple) and calls `translatePropGet`
let private translateProjection e =
  match e with
  | Lambda(var1, NewTuple args) -> 
      // Translate all tuple items
      List.map (translatePropGet var1.Name) args
  | Lambda(var1, arg) -> 
      // There is just one body expression
      [translatePropGet var1.Name arg]
  | _ -> failwith "Expected lambda expression"

let rec translateQuery e = 
  match e with
  // simpleq.SelectAttrs(<source>, fun p -> p.Name, p.Age)
  | SpecificCall <@@ neo4j.SelectAttrs @@> 
        (builder, [tTyp; rTyp], [source; proj]) -> 
      let q = translateQuery source
      let s = translateProjection proj
      { q with Select = Some(SelectAttributes s) }

  // // simpleq.Select(<source>, fun p -> p )
  // | SpecificCall <@@ neo4j.Select @@> 
  //       (builder, [tTyp; rTyp], [source; proj]) -> 
  //     let q = translateQuery source
  //     let s = translateProjection proj
  //     { q with Select = Some(SelectProps s) }
  
  // simpleq.SelectCount(<source>)
  | SpecificCall <@@ neo4j.SelectCount @@> 
        (builder, [tTyp], [source]) -> 
      let q = translateQuery source
      { q with Select = Some SelectCount }

  // simpleq.Where(<source>, fun p -> p.Age > 10)
  | SpecificCall <@@ neo4j.Where @@> 
        (builder, [tTyp], [source; cond]) -> 
      let q = translateQuery source
      let w = translateWhere cond
      { q with Where = w :: q.Where }

  // simpleq.For(DB.People, <...>)
  | SpecificCall <@@ neo4j.For @@> 
        (builder, [tTyp; rTyp], [source; body]) -> 
      let source = 
        // Extract the table name from 'DB.People'
        match source with
        | PropertyGet(None, prop, [])
            // when prop.DeclaringType = typeof<Neo4jDB> 
            -> prop.Name
        | _ -> failwith "Only sources of the form 'DB.<Prop>' are supported!"
      { Source = source; Where = []; Select = None; tTyp=tTyp; rTyp = rTyp }

  // simpleq.Match(DB.People, <...>)
  | SpecificCall <@@ neo4j.Match @@> 
        (builder, [tTyp; rTyp], [source; body]) -> 
      let source = 
        // Extract the table name from 'DB.People'
        match source with
        | PropertyGet(None, prop, [])
            // when prop.DeclaringType = typeof<Neo4jDB> 
            -> prop.Name
        | _ -> failwith "Only sources of the form 'DB.<Prop>' are supported!"
      { Source = source; Where = []; Select = None; tTyp=tTyp; rTyp = rTyp }

  // This should never happen
  | e -> failwithf "Unsupported query operation: %A" e


[<CLIMutable>]
type Person = { name: string; born: int;}
[<CLIMutable>]
type Movie = { title: string; released: int; tagline: string; }

// type SimpleNeo4jQueryBuilder with
//   member x.Run(source:Expr<Query<Movie>>) : Movie = 
//     let translation = source |> translateQuery
//     {Movie.released = 1969; title="My Movie"; tagline="fall into success!" } 
//   member x.Run(source:Expr<Query<Person>>) : Person = 
//     let translation = source |> translateQuery
//     {Person.name = "Peter"; Person.born=1973}
//   member x.Run(source:Expr<Query<Person * Movie>>) : Person * Movie = 
//     let translation = source |> translateQuery
//     {Person.name = "Peter"; Person.born=1973} , {Movie.released = 1969; title="My Movie"; tagline="fall into success!" } 
//   member x.Run(source:Expr<Query<'T>>) : 'T = 
//     let translation = source |> translateQuery
//     Unchecked.defaultof<'T>


