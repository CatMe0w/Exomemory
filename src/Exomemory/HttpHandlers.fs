module Exomemory.HttpHandlers

open System
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Http
open FSharp.Json
open Giraffe
open Exomemory.Models
open Exomemory.Dtos
open Exomemory.Queries

let jsonConfig = JsonConfig.create (jsonFieldNaming = Json.lowerCamelCase)

let veryLongTime = 4070908800000L // 2099-01-01 00:00:00 UTC

type Pagination =
    | Prev
    | Next
    | Current

let makeMessageDtos (messages: Message list) =
    messages
    |> List.map (fun m ->
        { Id = m.Id
          SenderId = m.SenderId
          Username = m.Username
          Content = m.Content
          Code = m.Code
          Role = m.Role
          Files = m.Files |> Json.deserializeEx jsonConfig
          Time = m.Time
          ReplyMessage = m.ReplyMessage |> Option.map (Json.deserializeEx jsonConfig)
          IsDeleted = m.Deleted |> Option.defaultValue false
          IsSystemMessage = m.System |> Option.defaultValue false
          IsSelfDestruct = m.Flash |> Option.defaultValue false
          Title = m.Title
          RoomId = m.RoomId })

let makeRoomDto (room: Room) =
    { RoomId = room.RoomId |> int64
      Name = room.RoomName
      UnreadCount = room.UnreadCount
      LastMessageTime = room.Utime
      LastMessage = room.LastMessage |> Json.deserializeEx jsonConfig }

let authenticate =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let settings = ctx.GetService<IConfiguration>()
            let username = settings["Authentication:Username"]
            let password = settings["Authentication:Password"]

            let validateRequest (ctx: HttpContext) =
                match ctx.TryGetRequestHeader "Authorization" with
                | Some auth when auth.StartsWith "Basic " ->
                    try
                        let auth = auth.Split(' ')[1]
                        let decoded = auth |> Convert.FromBase64String |> Text.Encoding.UTF8.GetString
                        username = decoded.Split(':')[0] && password = decoded.Split(':')[1]
                    with _ -> false
                | _ -> false

            let accessDenied = setStatusCode 401

            return! authorizeRequest validateRequest accessDenied next ctx
        }

let handleGetOverview =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let connectionString = ctx.GetService<IConfiguration>()["Database:ConnectionString"]

            let response: OverviewDto =
                { Rooms = connectionString |> listAllRooms |> List.map makeRoomDto
                  Messages = connectionString |> retrieveGlobalLatestMessages |> makeMessageDtos }

            return! json response next ctx
        }

let handleGetInspectRoom (roomId: int64) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let connectionString = ctx.GetService<IConfiguration>()["Database:ConnectionString"]

            let response = inspectRoom connectionString roomId |> makeRoomDto
            return! json response next ctx
        }

let handleGetSearchUsernames =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let keyword =
                match ctx.TryGetQueryStringValue "keyword" with
                | Some k -> k
                | None -> ""

            let page =
                match ctx.TryGetQueryStringValue "page" with
                | Some p -> p |> int64
                | None -> 1L

            let timeAfter =
                match ctx.TryGetQueryStringValue "timeAfter" with
                | Some t -> t |> int64
                | None -> 0L

            let timeBefore =
                match ctx.TryGetQueryStringValue "timeBefore" with
                | Some t -> t |> int64
                | None -> veryLongTime

            let roomId = ctx.TryGetQueryStringValue "roomId" |> Option.map int64

            let settings = ctx.GetService<IConfiguration>()
            let connectionString = settings["Database:ConnectionString"]
            let ftsConfig = settings["Database:FtsConfig"]

            let response =
                searchUsernames connectionString ftsConfig keyword page timeAfter timeBefore roomId
                |> makeMessageDtos

            return! json response next ctx
        }

let handleGetSearchMessages =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let keyword =
                match ctx.TryGetQueryStringValue "keyword" with
                | Some k -> k
                | None -> ""

            let page =
                match ctx.TryGetQueryStringValue "page" with
                | Some p -> p |> int64
                | None -> 1L

            let timeAfter =
                match ctx.TryGetQueryStringValue "timeAfter" with
                | Some t -> t |> int64
                | None -> 0L

            let timeBefore =
                match ctx.TryGetQueryStringValue "timeBefore" with
                | Some t -> t |> int64
                | None -> veryLongTime

            let roomId = ctx.TryGetQueryStringValue "roomId" |> Option.map int64

            let settings = ctx.GetService<IConfiguration>()
            let connectionString = settings["Database:ConnectionString"]
            let ftsConfig = settings["Database:FtsConfig"]

            let response =
                searchMessages connectionString ftsConfig keyword page timeAfter timeBefore roomId
                |> makeMessageDtos

            return! json response next ctx
        }

let handleGetLookupUser =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let id =
                match ctx.TryGetQueryStringValue "id" with
                | Some id -> id
                | None -> ""

            let page =
                match ctx.TryGetQueryStringValue "page" with
                | Some p -> p |> int64
                | None -> 1L

            let timeAfter =
                match ctx.TryGetQueryStringValue "timeAfter" with
                | Some t -> t |> int64
                | None -> 0L

            let timeBefore =
                match ctx.TryGetQueryStringValue "timeBefore" with
                | Some t -> t |> int64
                | None -> veryLongTime

            let roomId = ctx.TryGetQueryStringValue "roomId" |> Option.map int64

            let connectionString = ctx.GetService<IConfiguration>()["Database:ConnectionString"]

            let response =
                lookupUser connectionString id page timeAfter timeBefore roomId
                |> makeMessageDtos

            return! json response next ctx
        }

let handleGetLookupMessage =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let messageId =
                match ctx.TryGetQueryStringValue "id" with
                | Some k -> k
                | None -> ""

            let pagination =
                match ctx.TryGetQueryStringValue "pagination" with
                | Some "prev" -> Prev
                | Some "next" -> Next
                | _ -> Current

            let connectionString = ctx.GetService<IConfiguration>()["Database:ConnectionString"]

            let response =
                match pagination with
                | Prev -> lookupMessagePrevPage connectionString messageId
                | Next -> lookupMessageNextPage connectionString messageId
                | Current -> lookupMessage connectionString messageId
                |> makeMessageDtos

            return! json response next ctx
        }

let handleGetLookupRoom =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let roomId =
                match ctx.TryGetQueryStringValue "id" with
                | Some id -> id |> int64
                | None -> 0L

            let page =
                match ctx.TryGetQueryStringValue "page" with
                | Some p -> p |> int64
                | None -> 1L

            let timeAfter =
                match ctx.TryGetQueryStringValue "timeAfter" with
                | Some t -> t |> int64
                | None -> 0L

            let timeBefore =
                match ctx.TryGetQueryStringValue "timeBefore" with
                | Some t -> t |> int64
                | None -> veryLongTime

            let connectionString = ctx.GetService<IConfiguration>()["Database:ConnectionString"]

            let response =
                lookupRoom connectionString roomId page timeAfter timeBefore |> makeMessageDtos

            return! json response next ctx
        }
