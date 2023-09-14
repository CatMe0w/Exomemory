module Exomemory.Server.Models

type Message =
    { Id: string
      SenderId: string
      Username: string
      Content: string
      Code: string option
      Timestamp: string
      Date: string
      Role: string option
      File: string option
      Files: string
      Time: int64
      ReplyMessage: string option
      At: string option
      Deleted: bool option
      System: bool option
      Mirai: string option
      Reveal: bool option
      Flash: bool option
      Title: string option
      RoomId: int64
      AnonymousId: string option
      AnonymousFlag: string option
      Hide: bool option
      BubbleId: int64 option
      SubId: int64 option }

type Room =
    { RoomId: string
      RoomName: string
      Index: int64
      UnreadCount: int64
      Priority: int64
      Utime: int64
      Users: string
      LastMessage: string
      At: string option
      AutoDownload: bool option
      DownloadPath: string option }
