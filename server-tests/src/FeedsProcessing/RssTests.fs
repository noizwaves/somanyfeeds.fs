module ``Rss Processor Tests``

    open NUnit.Framework
    open FsUnit
    open System
    open System.IO

    open Server.Articles.Data
    open Server.SourceType
    open Server.FeedsProcessing.Download
    open Server.FeedsProcessing.Rss

    [<Test>]
    let ``processFeed with standard medium xml`` () =
        let downloadedFeed = DownloadedFeed <| File.ReadAllText("../../../../server/resources/medium.rss.xml")


        let result = processRssFeed Blog downloadedFeed


        match result with
        | Error _ -> Assert.Fail("Expected success")
        | Ok records ->
            let expectedTimeUtc = new DateTime(2016, 09, 20, 12, 54, 44, DateTimeKind.Utc)

            List.length records |> should equal 5
            List.head records |> should equal { Title = Some "First title!"
                                                Link = Some "https://medium.com/@its_damo/first"
                                                Content = "<p>This is the content</p>"
                                                Date = Some expectedTimeUtc
                                                Source = Blog
                                              }
