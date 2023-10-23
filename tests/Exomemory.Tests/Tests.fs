module Tests

open System
open System.IO
open System.Net
open System.Net.Http
open Microsoft.Extensions.Configuration
open Xunit
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection

// ---------------------------------
// Helper functions (extend as you need)
// ---------------------------------

let createHost () =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> Exomemory.Server.App.configureApp)
        .ConfigureAppConfiguration(Exomemory.Server.App.configureAppConfiguration)
        .ConfigureServices(Action<IServiceCollection> Exomemory.Server.App.configureServices)

let runTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let authorize (username: string) (password: string) (client: HttpClient) =
    let authBase64 =
        $"%s{username}:%s{password}"
        |> Text.Encoding.UTF8.GetBytes
        |> Convert.ToBase64String

    client.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Basic", authBase64)
    client

let httpGet (path: string) (client: HttpClient) = path |> client.GetAsync |> runTask

let isStatus (code: HttpStatusCode) (response: HttpResponseMessage) =
    Assert.Equal(code, response.StatusCode)
    response

let ensureSuccess (response: HttpResponseMessage) =
    if not response.IsSuccessStatusCode then
        // Since we don't have any content for non-successful responses, we just fail with the status code description
        response.StatusCode.ToString() |> failwithf "%A"
        
        // response.Content.ReadAsStringAsync() |> runTask |> failwithf "%A"
    else
        response

let readText (response: HttpResponseMessage) =
    response.Content.ReadAsStringAsync() |> runTask

let shouldEqual (expected: string) (actual: string) = Assert.Equal(expected, actual)

let shouldContain (expected: string) (actual: string) = Assert.True(actual.Contains expected)

// ---------------------------------
// Tests
// ---------------------------------

[<Fact>]
let ``Route /overview returns a Room list and a Message list`` () =
    use server = new TestServer(createHost ())
    use client = server.CreateClient()

    let settings = server.Host.Services.GetService<IConfiguration>()
    let username = settings["Authentication:Username"]
    let password = settings["Authentication:Password"] // XXX

    client
    |> authorize username password
    |> httpGet "/overview"
    |> ensureSuccess
    |> readText
    |> shouldContain "\"rooms\":" // TODO

[<Fact>]
let ``Route without authorization returns 401 Unauthorized`` () =
    use server = new TestServer(createHost ())
    use client = server.CreateClient()

    client
    |> httpGet "/overview"
    |> isStatus HttpStatusCode.Unauthorized

[<Fact>]
let ``Route which doesn't exist returns 404 Page not found`` () =
    use server = new TestServer(createHost ())
    use client = server.CreateClient()

    let settings = server.Host.Services.GetService<IConfiguration>()
    let username = settings["Authentication:Username"]
    let password = settings["Authentication:Password"]

    client
    |> authorize username password
    |> httpGet "/route/which/does/not/exist"
    |> isStatus HttpStatusCode.NotFound
