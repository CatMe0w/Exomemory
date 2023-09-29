module Exomemory.Server.Queries

open Npgsql.FSharp
open Exomemory.Server.Models

let makeMessageModel =
    fun (r: RowReader) ->
        { Id = r.text "_id"
          SenderId = r.text "senderId"
          Username = r.text "username"
          Content = r.text "content"
          Code = r.textOrNone "code"
          Timestamp = r.string "timestamp"
          Date = r.text "date"
          Role = r.textOrNone "role"
          File = r.textOrNone "file"
          Files = r.text "files"
          Time = r.int64 "time"
          ReplyMessage = r.textOrNone "replyMessage"
          At = r.textOrNone "at"
          Deleted = r.boolOrNone "deleted"
          System = r.boolOrNone "system"
          Mirai = r.textOrNone "mirai"
          Reveal = r.boolOrNone "reveal"
          Flash = r.boolOrNone "flash"
          Title = r.textOrNone "title"
          RoomId = r.int64 "roomId"
          AnonymousId = r.textOrNone "anonymousId"
          AnonymousFlag = r.textOrNone "anonymousflag"
          Hide = r.boolOrNone "hide"
          BubbleId = r.int64OrNone "bubble_id"
          SubId = r.int64OrNone "subid" }

let makeRoomModel =
    fun (r: RowReader) ->
        { RoomId = r.text "roomId"
          RoomName = r.text "roomName"
          Index = r.int64 "index"
          UnreadCount = r.int64 "unreadCount"
          Priority = r.int64 "priority"
          Utime = r.int64 "utime"
          Users = r.text "users"
          LastMessage = r.text "lastMessage"
          At = r.textOrNone "at"
          AutoDownload = r.boolOrNone "autoDownload"
          DownloadPath = r.textOrNone "downloadPath" }

let matchRoomIdSql roomId =
    match roomId with
    | Some _ -> "AND \"roomId\" = @roomId"
    | None -> ""

let matchRoomIdSqlProps roomId : (string * SqlValue) list =
    match roomId with
    | Some roomId -> [ "roomId", Sql.int64 roomId ]
    | None -> []

let retrieveGlobalLatestMessages connectionString =
    connectionString
    |> Sql.connect
    |> Sql.query
        "SELECT *
        FROM messages
        ORDER BY time DESC
        LIMIT 101
        OFFSET 0;"
    |> Sql.execute makeMessageModel

let listAllRooms connectionString =
    connectionString
    |> Sql.connect
    |> Sql.query
        "SELECT *
        FROM rooms
        ORDER BY utime DESC;"
    |> Sql.execute makeRoomModel

let inspectRoom connectionString roomId =
    connectionString
    |> Sql.connect
    |> Sql.query
        "SELECT *
        FROM rooms
        WHERE \"roomId\" = @roomId;"
    |> Sql.parameters [ "roomId", Sql.text (string roomId) ]
    |> Sql.executeRow makeRoomModel

let searchUsernames connectionString ftsConfig keyword page timeAfter timeBefore roomId =
    connectionString
    |> Sql.connect
    |> Sql.query
        $"SELECT *
        FROM messages
        WHERE to_tsvector('%s{ftsConfig}', username) @@ to_tsquery('%s{ftsConfig}', @keyword)
        AND time >= @timeAfter
        AND time <= @timeBefore
        %s{matchRoomIdSql roomId}
        ORDER BY time DESC
        LIMIT 101
        OFFSET @page;"
    |> Sql.parameters (
        [ "keyword", Sql.text keyword
          "page", Sql.int64 ((page - 1L) * 100L)
          "timeAfter", Sql.int64 timeAfter
          "timeBefore", Sql.int64 timeBefore ]
        @ matchRoomIdSqlProps roomId
    )
    |> Sql.execute makeMessageModel

let searchMessages connectionString ftsConfig keyword page timeAfter timeBefore roomId =
    connectionString
    |> Sql.connect
    |> Sql.query
        $"SELECT *
        FROM messages
        WHERE to_tsvector('%s{ftsConfig}', content) @@ to_tsquery('%s{ftsConfig}', @keyword)
        AND time >= @timeAfter
        AND time <= @timeBefore
        %s{matchRoomIdSql roomId}
        ORDER BY time DESC
        LIMIT 101
        OFFSET @page;"
    |> Sql.parameters (
        [ "keyword", Sql.text keyword
          "page", Sql.int64 ((page - 1L) * 100L)
          "timeAfter", Sql.int64 timeAfter
          "timeBefore", Sql.int64 timeBefore ]
        @ matchRoomIdSqlProps roomId
    )
    |> Sql.execute makeMessageModel

let lookupUser connectionString id page timeAfter timeBefore roomId =
    connectionString
    |> Sql.connect
    |> Sql.query
        $"SELECT *
        FROM messages
        WHERE \"senderId\" = @id
        AND time >= @timeAfter
        AND time <= @timeBefore
        %s{matchRoomIdSql roomId}
        ORDER BY time DESC
        LIMIT 101
        OFFSET @page;"
    |> Sql.parameters(
        [ "id", Sql.text id
          "page", Sql.int64 ((page - 1L) * 100L)
          "timeAfter", Sql.int64 timeAfter
          "timeBefore", Sql.int64 timeBefore ]
        @ matchRoomIdSqlProps roomId
    )
    |> Sql.execute makeMessageModel

let lookupMessage connectionString messageId =
    connectionString
    |> Sql.connect
    |> Sql.query
        """WITH t AS (SELECT time, "roomId"
                   FROM messages
                   WHERE _id = @messageId)
        SELECT x.*
        FROM ((SELECT a.*
               FROM messages a
               WHERE time >= (SELECT time FROM t)
                 AND "roomId" = (SELECT "roomId" FROM t)
               ORDER BY time
               LIMIT 50)
              UNION
              (SELECT b.*
               FROM messages b
               WHERE time < (SELECT time FROM t)
                 AND "roomId" = (SELECT "roomId" FROM t)
               ORDER BY time DESC
               LIMIT 51)) x
        ORDER BY time;"""
    |> Sql.parameters [ "messageId", Sql.text messageId ]
    |> Sql.execute makeMessageModel

let lookupMessagePrevPage connectionString messageId =
    connectionString
    |> Sql.connect
    |> Sql.query
        """WITH t AS (SELECT time, "roomId"
                   FROM messages
                   WHERE _id = @messageId)
        SELECT x.*
        FROM (SELECT a.*
              FROM messages a
              WHERE time >= (SELECT time FROM t)
                AND "roomId" = (SELECT "roomId" FROM t)
              ORDER BY time DESC
              LIMIT 101) x
        ORDER BY time;"""
    |> Sql.parameters [ "messageId", Sql.text messageId ]
    |> Sql.execute makeMessageModel

let lookupMessageNextPage connectionString messageId =
    connectionString
    |> Sql.connect
    |> Sql.query
        """WITH t AS (SELECT time, "roomId"
                   FROM messages
                   WHERE _id = @messageId)
        SELECT a.*
        FROM messages a
        WHERE time < (SELECT time FROM t)
          AND "roomId" = (SELECT "roomId" FROM t)
        ORDER BY time
        LIMIT 101;"""
    |> Sql.parameters [ "messageId", Sql.text messageId ]
    |> Sql.execute makeMessageModel

let lookupRoom connectionString roomId page timeAfter timeBefore =
    connectionString
    |> Sql.connect
    |> Sql.query
        "SELECT *
        FROM messages
        WHERE \"roomId\" = @roomId
        AND time >= @timeAfter
        AND time <= @timeBefore
        ORDER BY time DESC
        LIMIT 101
        OFFSET @page;"
    |> Sql.parameters
        [ "roomId", Sql.int64 roomId
          "page", Sql.int64 ((page - 1L) * 100L)
          "timeAfter", Sql.int64 timeAfter
          "timeBefore", Sql.int64 timeBefore ]
    |> Sql.execute makeMessageModel
