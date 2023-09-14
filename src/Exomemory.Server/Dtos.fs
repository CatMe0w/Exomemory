module Exomemory.Server.Dtos

open FSharp.Json

[<CLIMutable>]
type FileDto =
    { Type: string
      Size: int64 option
      Url: string
      Name: string option
      Fid: string option }

[<CLIMutable>]
type ReplyMessageDto =
    { [<JsonField("_id")>]
      Id: string
      Username: string
      Content: string
      Files: FileDto list }

[<CLIMutable>]
type MessageDto =
    { Id: string
      SenderId: string
      Username: string
      Content: string
      Code: string option
      Role: string option
      Files: FileDto list
      Time: int64
      ReplyMessage: ReplyMessageDto option
      IsDeleted: bool
      IsSystemMessage: bool
      IsSelfDestruct: bool
      Title: string option
      RoomId: int64 }

[<CLIMutable>]
type RoomLastMessageDto =
    { Content: string
      Username: string option }

[<CLIMutable>]
type RoomDto =
    { RoomId: int64
      Name: string
      UnreadCount: int64
      LastMessageTime: int64
      LastMessage: RoomLastMessageDto }

[<CLIMutable>]
type OverviewDto =
    { Rooms: RoomDto list
      Messages: MessageDto list }
