module BglUtils

open System.IO

let keepOffset<'t> (reader: BinaryReader) (op: unit -> 't) =
    let currentOffset = reader.BaseStream.Position
    let t = op()
    reader.BaseStream.Position <- currentOffset
    t

let readIcao (icao: uint32) =
    let charFromDigit (digit: uint32) =
        if digit = 0u then ' '
        elif digit < 12u then '0' + (char)(digit - 2u)
        else 'A' + (char)(digit - 12u)

    let rec digits (current: uint32): char list =
        if current < 38u then [charFromDigit current]
        else (charFromDigit (current % 38u)) :: (digits (current / 38u))

    new string(icao >>> 5 |> digits |> Array.ofList |> Array.rev)

let readLongitude (lon: uint32) =
    float lon * 360.0 / float (3 * 0x10000000) - 180.0

let readLatitude (lat: uint32) =
    90.0 - float lat * (180.0 / float (2 * 0x10000000))

let readStringZ (reader: BinaryReader): string =
    let rec loop (acc: string): string =
        let next = reader.ReadByte()
        if next = 0uy then acc else loop (acc + string (char next))

    loop ""

let readString (reader: BinaryReader) length: string =
    reader.ReadBytes(length) |> System.Text.Encoding.ASCII.GetString 
    