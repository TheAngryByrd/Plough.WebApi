﻿[<AutoOpen>]
module Plough.WebApi.Server.Builder

open System.Globalization
open Plough.WebApi.Server


// ---------------------------
// Globally useful functions
// ---------------------------

/// <summary>
/// The warbler function is a <see cref="HttpHandler"/> wrapper function which prevents a <see cref="HttpHandler"/> to be pre-evaluated at startup.
/// </summary>
/// <param name="f">A function which takes a HttpFunc * HttpContext tuple and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="next"></param>
/// <param name="ctx"></param>
/// <param name="builder"></param>
/// <example>
/// <code>
/// warbler(fun _ -> someHttpHandler)
/// </code>
/// </example>
/// <returns>Returns a <see cref="HttpHandler"/> function.</returns>
let inline warbler f (next : HttpFunc<'context>) (ctx : 'context) (builder : #ServerBuilder<'context>) =
    builder.warbler f next ctx

/// <summary>
/// Use skipPipeline to shortcircuit the <see cref="HttpHandler"/> pipeline and return None to the surrounding <see cref="HttpHandler"/> or the Giraffe middleware (which would subsequently invoke the next middleware as a result of it).
/// </summary>
let inline skipPipeline (builder : #ServerBuilder<'context>) : HttpFuncResult<'context> =
    builder.skipPipeline

/// <summary>
/// Use earlyReturn to shortcircuit the <see cref="HttpHandler"/> pipeline and return Some HttpContext to the surrounding <see cref="HttpHandler"/> or the Giraffe middleware (which would subsequently end the pipeline by returning the response back to the client).
/// </summary>
let inline earlyReturn (builder : #ServerBuilder<'context>) : HttpFunc<'context> =
    builder.earlyReturn

// ---------------------------
// Convenience Handlers
// ---------------------------

/// <summary>
/// The handleContext function is a convenience function which can be used to create a new <see cref="HttpHandler"/> function which only requires access to the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object.
/// </summary>
/// <param name="contextMap">A function which accepts a <see cref="Microsoft.AspNetCore.Http.HttpContext"/> object and returns a <see cref="HttpFuncResult"/> function.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline handleContext (contextMap : 'context -> HttpFuncResult<'context>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.handleContext contextMap

// ---------------------------
// Default Combinators
// ---------------------------

/// <summary>
/// Combines two <see cref="HttpHandler"/> functions into one.
/// Please mind that both <see cref="HttpHandler"/>  functions will get pre-evaluated at runtime by applying the next <see cref="HttpFunc"/> parameter of each handler.
/// You can also use the fish operator `>=>` as a more convenient alternative to compose.
/// </summary>
/// <param name="handler1"></param>
/// <param name="handler2"></param>
/// <param name="builder"></param>
/// <returns>A <see cref="HttpFunc"/>.</returns>
let inline compose (handler1 : (#ServerBuilder<'context> -> HttpHandler<'context>)) (handler2 : (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.compose (handler1 builder) (handler2 builder)

/// <summary>
/// Combines two <see cref="HttpHandler"/> functions into one.
/// Please mind that both <see cref="HttpHandler"/> functions will get pre-evaluated at runtime by applying the next <see cref="HttpFunc"/> parameter of each handler.
/// </summary>
let (>=>) = compose

/// <summary>
/// Iterates through a list of <see cref="HttpHandler"/> functions and returns the result of the first <see cref="HttpHandler"/> of which the outcome is Some HttpContext.
/// Please mind that all <see cref="HttpHandler"/> functions will get pre-evaluated at runtime by applying the next (HttpFunc) parameter to each handler.
/// </summary>
/// <param name="handlers"></param>
/// <param name="builder"></param>
/// <returns>A <see cref="HttpFunc"/>.</returns>
let inline choose (handlers : (#ServerBuilder<'context> -> HttpHandler<'context>) list) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder ->
        handlers
        |> List.map (fun f -> f builder)
        |> builder.choose
    
// ---------------------------
// Default HttpHandlers
// ---------------------------

let inline GET      (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.GET
let inline POST     (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.POST
let inline PUT      (builder : ServerBuilder<'context>) : HttpHandler<'context> = builder.PUT
let inline PATCH    (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.PATCH
let inline DELETE   (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.DELETE
let inline HEAD     (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.HEAD
let inline OPTIONS  (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.OPTIONS
let inline TRACE    (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.TRACE
let inline CONNECT  (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.CONNECT
let inline GET_HEAD (builder : #ServerBuilder<'context>) : HttpHandler<'context> = builder.GET_HEAD

/// <summary>
/// Clears the current <see cref="Microsoft.AspNetCore.Http.HttpResponse"/> object.
/// This can be useful if a <see cref="HttpHandler"/> function needs to overwrite the response of all previous <see cref="HttpHandler"/> functions with its own response (most commonly used by an <see cref="ErrorHandler"/> function).
/// </summary>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline clearResponse (builder : #ServerBuilder<'context>) : HttpHandler<'context> =
    builder.clearResponse

/// <summary>
/// Sets the HTTP status code of the response.
/// </summary>
/// <param name="statusCode">The status code to be set in the response. For convenience you can use the static <see cref="Microsoft.AspNetCore.Http.StatusCodes"/> class for passing in named status codes instead of using pure int values.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline setStatusCode (statusCode : int) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.setStatusCode statusCode
    
/// <summary>
/// Adds or sets a HTTP header in the response.
/// </summary>
/// <param name="key">The HTTP header name. For convenience you can use the static <see cref="Microsoft.Net.Http.Headers.HeaderNames"/> class for passing in strongly typed header names instead of using pure string values.</param>
/// <param name="value">The value to be set. Non string values will be converted to a string using the object's ToString() method.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline setHttpHeader (key : string) (value : obj) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.setHttpHeader key value

/// <summary>
/// Filters an incoming HTTP request based on the accepted mime types of the client (Accept HTTP header).
/// If the client doesn't accept any of the provided mimeTypes then the handler will not continue executing the next <see cref="HttpHandler"/> function.
/// </summary>
/// <param name="mimeTypes">List of mime types of which the client has to accept at least one.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline mustAccept (mimeTypes : string list) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.mustAccept mimeTypes

/// <summary>
/// Redirects to a different location with a `302` or `301` (when permanent) HTTP status code.
/// </summary>
/// <param name="permanent">If true the redirect is permanent (301), otherwise temporary (302).</param>
/// <param name="location">The URL to redirect the client to.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline redirectTo (permanent : bool) (location : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.redirectTo permanent location

// ---------------------------
// Model binding functions
// ---------------------------

/// <summary>
/// Parses a JSON payload into an instance of type 'T.
/// </summary>
/// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline bindJson(f : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.bindJson<'T> (fun s -> f s builder)

/// <summary>
/// Parses a XML payload into an instance of type 'T.
/// </summary>
/// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline bindXml (f : 'T -> HttpHandler<'context>) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.bindXml<'T> f

/// <summary>
/// Parses a HTTP form payload into an instance of type 'T.
/// </summary>
/// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
/// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline bindForm (culture : CultureInfo option) (f : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.bindForm<'T> culture (fun x -> f x builder)

/// <summary>
/// Tries to parse a HTTP form payload into an instance of type 'T.
/// </summary>
/// <param name="parsingErrorHandler">A <see cref="System.String"/> -> <see cref="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> parameter holds the parsing error message.</param>
/// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
/// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline tryBindForm (parsingErrorHandler : string -> (#ServerBuilder<'context> -> HttpHandler<'context>))
                       (culture             : CultureInfo option)
                       (successHandler      : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>))
                       : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.tryBindForm<'T> (fun s -> parsingErrorHandler s builder) culture (fun s -> successHandler s builder)

/// <summary>
/// Parses a HTTP query string into an instance of type 'T.
/// </summary>
/// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
/// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline bindQuery (culture : CultureInfo option) (f : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.bindQuery<'T> culture (fun s -> f s builder)

/// <summary>
/// Tries to parse a query string into an instance of type `'T`.
/// </summary>
/// <param name="parsingErrorHandler">A <see href="HttpHandler"/> function which will get invoked when the model parsing fails. The <see cref="System.String"/> input parameter holds the parsing error message.</param>
/// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
/// <param name="successHandler">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline tryBindQuery (parsingErrorHandler : string -> (#ServerBuilder<'context> -> HttpHandler<'context>))
                        (culture             : CultureInfo option)
                        (successHandler      : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>))
                        : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.tryBindQuery<'T> (fun s -> parsingErrorHandler s builder) culture (fun s -> successHandler s builder)

/// <summary>
/// Parses a HTTP payload into an instance of type 'T.
/// The model can be sent via XML, JSON, form or query string.
/// </summary>
/// <param name="culture">An optional <see cref="System.Globalization.CultureInfo"/> element to be used when parsing culture specific data such as float, DateTime or decimal values.</param>
/// <param name="f">A function which accepts an object of type 'T and returns a <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline bindModel (culture : CultureInfo option) (f : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.bindModel<'T> culture (fun s -> f s builder)

// ---------------------------
// Response writing functions
// ---------------------------

/// **Description**
///
/// Writes a byte array to the body of the HTTP response and sets the HTTP `Content-Length` header accordingly.
///
/// **Parameters**
///
/// `bytes`: The byte array to be send back to the client.
///
/// **Output**
///
/// A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.

/// <summary>
/// Writes a byte array to the body of the HTTP response and sets the HTTP Content-Length header accordingly.
/// </summary>
/// <param name="bytes">The byte array to be send back to the client.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline setBody (bytes : byte array) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.setBody bytes

/// <summary>
/// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly.
/// </summary>
/// <param name="str">The string value to be send back to the client.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline setBodyFromString (str : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.setBodyFromString str

/// <summary>
/// Writes an UTF-8 encoded string to the body of the HTTP response and sets the HTTP Content-Length header accordingly, as well as the Content-Type header to text/plain.
/// </summary>
/// <param name="str">The string value to be send back to the client.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline text (str : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.text str

/// <summary>
/// Serializes an object to JSON and writes the output to the body of the HTTP response.
/// It also sets the HTTP Content-Type header to application/json and sets the Content-Length header accordingly.
/// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
/// </summary>
/// <param name="dataObj">The object to be send back to the client.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline json (dataObj : 'T) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.json dataObj

/// <summary>
/// Serializes an object to JSON and writes the output to the body of the HTTP response using chunked transfer encoding.
/// It also sets the HTTP Content-Type header to application/json and sets the Transfer-Encoding header to chunked.
/// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Json.ISerializer"/>.
/// </summary>
/// <param name="dataObj">The object to be send back to the client.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline jsonChunked (dataObj : 'T) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.jsonChunked dataObj

/// <summary>
/// Serializes an object to XML and writes the output to the body of the HTTP response.
/// It also sets the HTTP Content-Type header to application/xml and sets the Content-Length header accordingly.
/// The JSON serializer can be configured in the ASP.NET Core startup code by registering a custom class of type <see cref="Xml.ISerializer"/>.
/// </summary>
/// <param name="dataObj">The object to be send back to the client.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline xml (dataObj : obj) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.xml dataObj

/// <summary>
/// Reads a HTML file from disk and writes its contents to the body of the HTTP response.
/// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
/// </summary>
/// <param name="filePath">A relative or absolute file path to the HTML file.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline htmlFile (filePath : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.htmlFile filePath

/// <summary>
/// Writes a HTML string to the body of the HTTP response.
/// It also sets the HTTP header Content-Type to text/html and sets the Content-Length header accordingly.
/// </summary>
/// <param name="html">The HTML string to be send back to the client.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler" /> function which can be composed into a bigger web application.</returns>
let inline htmlString (html : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.htmlString html
    
// ---------------------------
// Routing functions
// ---------------------------
/// <summary>
/// Filters an incoming HTTP request based on the port.
/// </summary>
/// <param name="fns">List of port to <see cref="HttpHandler"/> mappings</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routePorts (fns : (int * (#ServerBuilder<'context> -> HttpHandler<'context>)) list) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> fns |> List.map (fun (i, p) -> i, p builder) |> builder.routePorts

/// <summary>
/// Filters an incoming HTTP request based on the request path (case sensitive).
/// </summary>
/// <param name="path">Request path.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline route (path : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.route path

/// <summary>
/// Filters an incoming HTTP request based on the request path (case insensitive).
/// </summary>
/// <param name="path">Request path.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routeCi (path : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeCi path

/// <summary>
/// Filters an incoming HTTP request based on the request path using Regex (case sensitive).
/// </summary>
/// <param name="path">Regex path.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routex (path : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routex path

/// <summary>
/// Filters an incoming HTTP request based on the request path using Regex (case insensitive).
/// </summary>
/// <param name="path">Regex path.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routeCix (path : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeCix path

/// <summary>
/// Filters an incoming HTTP request based on the request path (case sensitive).
/// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
///
/// Supported format chars**
///
/// %b: bool
/// %c: char
/// %s: string
/// %i: int
/// %d: int64
/// %f: float/double
/// %O: Guid
/// </summary>
/// <param name="path">A format string representing the expected request path.</param>
/// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routef path (fun s -> routeHandler s builder)

/// <summary>
/// Filters an incoming HTTP request based on the request path.
/// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
///
/// Supported format chars**
///
/// %b: bool
/// %c: char
/// %s: string
/// %i: int
/// %d: int64
/// %f: float/double
/// %O: Guid
/// </summary>
/// <param name="path">A format string representing the expected request path.</param>
/// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routeCif (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeCif path (fun s -> routeHandler s builder)

/// <summary>
/// Filters an incoming HTTP request based on the request path (case insensitive).
/// If the route matches the incoming HTTP request then the parameters from the string will be used to create an instance of 'T and subsequently passed into the supplied routeHandler.
/// </summary>
/// <param name="route">A string representing the expected request path. Use {propertyName} for reserved parameter names which should map to the properties of type 'T. You can also use valid Regex within the route string.</param>
/// <param name="routeHandler">A function which accepts a tuple 'T of the parsed parameters and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
/// <param name="builder"></param>
/// <typeparam name="'T"></typeparam>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routeBind (route : string) (routeHandler : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeBind route (fun s -> routeHandler s builder)

/// <summary>
/// Filters an incoming HTTP request based on the beginning of the request path (case sensitive).
/// </summary>
/// <param name="subPath">The expected beginning of a request path.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routeStartsWith (subPath : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeStartsWith subPath

/// <summary>
/// Filters an incoming HTTP request based on the beginning of the request path (case insensitive).
/// </summary>
/// <param name="subPath">The expected beginning of a request path.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let inline routeStartsWithCi (subPath : string) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeStartsWithCi subPath

/// <summary>
/// Filters an incoming HTTP request based on the beginning of the request path (case sensitive).
/// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
///
/// Supported format chars**
///
/// %b: bool
/// %c: char
/// %s: string
/// %i: int
/// %d: int64
/// %f: float/double
/// %O: Guid
/// </summary>
/// <param name="path">A format string representing the expected request path.</param>
/// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let routeStartsWithf (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeStartsWithf path (fun s -> routeHandler s builder)

/// <summary>
/// Filters an incoming HTTP request based on the beginning of the request path (case insensitive).
/// If the route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
///
/// Supported format chars**
///
/// %b: bool
/// %c: char
/// %s: string
/// %i: int
/// %d: int64
/// %f: float/double
/// %O: Guid
/// </summary>
/// <param name="path">A format string representing the expected request path.</param>
/// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let routeStartsWithCif (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.routeStartsWithCif path (fun s -> routeHandler s builder)

/// <summary>
/// Filters an incoming HTTP request based on a part of the request path (case sensitive).
/// Subsequent route handlers inside the given handler function should omit the already validated path.
/// </summary>
/// <param name="path">A part of an expected request path.</param>
/// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let subRoute (path : string) (handler : (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.subRoute path (handler builder)

/// <summary>
/// Filters an incoming HTTP request based on a part of the request path (case insensitive).
/// Subsequent route handlers inside the given handler function should omit the already validated path.
/// </summary>
/// <param name="path">A part of an expected request path.</param>
/// <param name="handler">A Giraffe <see cref="HttpHandler"/> function.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let subRouteCi (path : string) (handler : (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.subRouteCi path (handler builder)

/// <summary>
/// Filters an incoming HTTP request based on a part of the request path (case sensitive).
/// If the sub route matches the incoming HTTP request then the arguments from the <see cref="Microsoft.FSharp.Core.PrintfFormat"/> will be automatically resolved and passed into the supplied routeHandler.
///
/// Supported format chars
///
/// %b: bool
/// %c: char
/// %s: string
/// %i: int
/// %d: int64
/// %f: float/double
/// %O: Guid
///
/// Subsequent routing handlers inside the given handler function should omit the already validated path.
/// </summary>
/// <param name="path">A format string representing the expected request sub path.</param>
/// <param name="routeHandler">A function which accepts a tuple 'T of the parsed arguments and returns a <see cref="HttpHandler"/> function which will subsequently deal with the request.</param>
/// <param name="builder"></param>
/// <returns>A Giraffe <see cref="HttpHandler"/> function which can be composed into a bigger web application.</returns>
let subRoutef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> (#ServerBuilder<'context> -> HttpHandler<'context>)) : (#ServerBuilder<'context> -> HttpHandler<'context>) =
    fun builder -> builder.subRoutef path (fun s -> routeHandler s builder)