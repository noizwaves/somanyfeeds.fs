module Server.Program

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.Data


module Views =
    open GiraffeViewEngine

    let layout (content : XmlNode list) =
        html [] [
            head [] [
                meta [ _charset "utf-8" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]


module App =
    let private downloadFunction (url : string) : Result<string, string> =
        try
            Result.Ok <| Http.RequestString url
        with
        | ex -> Result.Error <| String.Format("There was an error downloading the feed. {0}", ex.ToString())

    let private processFeeds =
        ArticlesProcessing.processFeeds
            ArticlesData.Repository.updateAll
            [ Processors.Rss.processFeed downloadFunction
            ; Processors.Atom.processFeed downloadFunction
            ]
            Feeds.Repository.findAll


    let backgroundProcessing =
        let tenMinutes = 10 * 60 * 1000

        async {
            while true do
                processFeeds
                do! Async.Sleep tenMinutes
        }


    let private articlesListHandler =
        ArticlesHandlers.list Views.layout ArticlesData.Repository.findAll


    let handler =
        choose [
            GET >=>
                choose [
                    route "/" >=> articlesListHandler
                ]
            setStatusCode 404 >=> text "Not Found"
        ]


let errorHandler (ex : Exception) (logger : ILogger) : HttpHandler =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message


let configureApp (app : IApplicationBuilder) =
    (app.UseGiraffeErrorHandler errorHandler)
        .UseStaticFiles()
        .UseGiraffe(App.handler)


let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore


let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Warning

    builder
        .AddFilter(filter)
        .AddConsole()
        .AddDebug() |> ignore


[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")

    Async.Start App.backgroundProcessing

    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()

    0
