module BglAirport

open System.IO
open BglUtils
open Model

type SubRecord =
    | Name of string
    | Runway of Runway
    | Unknown

let readRunway (reader: BinaryReader): Runway =
    let surface = LanguagePrimitives.EnumOfValue<uint16, SurfaceType>(reader.ReadUInt16() &&& 0x07fus)
    let number = reader.ReadByte()
    let desig = reader.ReadByte()
    reader.ReadByte() |> ignore // secondary runway number
    reader.ReadByte() |> ignore // secondary runway designator
    reader.ReadUInt32() |> ignore // ICAO primary ILS
    reader.ReadUInt32() |> ignore // ICAO secondary ILS
    reader.ReadUInt32() |> ignore // longitude
    reader.ReadUInt32() |> ignore // latitude
    reader.ReadUInt32() |> ignore // elevation
    let length = reader.ReadSingle()
    let width = reader.ReadSingle()
    let heading = reader.ReadSingle()
    reader.ReadSingle() |> ignore // pattern altitude
    reader.ReadUInt16() |> ignore // marking flags
    let lights = reader.ReadUInt16() // lights flags
    let isLighted = lights &&& 0x0fus > 0us

    { 
        surface = surface
        number = number
        designator = desig
        length = length
        width = width
        heading = heading
        isLighted = isLighted
    }

let readSubRecords (reader: BinaryReader) nextRecord: SubRecord seq =
    let rec loop (acc: SubRecord seq): SubRecord seq =
        if reader.BaseStream.Position >= nextRecord then acc
        else
            let pos = reader.BaseStream.Position
            let id = reader.ReadUInt16()
            let size = reader.ReadUInt32()
            let nextSubrecord = pos + (int64 size)

            let subrecord = 
                match id with
                    | 0x19us -> Name(readString reader (int size - 6))
                    | 0xceus -> Runway(readRunway reader)
                    | _ -> Unknown

            reader.BaseStream.Position <- nextSubrecord
            Seq.append acc [subrecord] |> loop

    loop Seq.empty

let readAirport (reader: BinaryReader): Airport =
    let pos = reader.BaseStream.Position

    let id = reader.ReadUInt16()
    let size = reader.ReadUInt32()
    let numRunways = reader.ReadByte()
    let numComs = reader.ReadByte()
    let numStarts = reader.ReadByte()
    let numApproaches = reader.ReadByte()
    let numAprons = reader.ReadByte()
    let numHelipads = reader.ReadByte()
    let long = reader.ReadUInt32() |> readLongitude
    let lat = reader.ReadUInt32() |> readLatitude
    let alt = reader.ReadUInt32() // in 1/1000 meters
    reader.BaseStream.Seek(16L, SeekOrigin.Current) |> ignore
    let icao = reader.ReadUInt32() |> readIcao
    reader.ReadUInt32() |> ignore
    let gas = reader.ReadUInt32()
    let hasAvGas = gas &&& 0x40000000u <> 0u
    let hasJetFuel = gas &&& 0x80000000u <> 0u
    reader.ReadByte() |> ignore
    reader.ReadByte() |> ignore
    reader.ReadUInt16() |> ignore
    reader.BaseStream.Seek(12L, SeekOrigin.Current) |> ignore
    let nextRecord = pos + (int64 size)

    let runways = 
        readSubRecords reader nextRecord 
            |> Seq.collect (fun sr -> match sr with 
                                        | Runway(r) -> [r]
                                        | _ -> [])

    reader.BaseStream.Position <- nextRecord

    { 
        id = id
        size = size
        numRunways = numRunways
        numComs = numComs
        numStarts = numStarts
        numApproaches = numApproaches
        numAprons = numAprons
        numHelipads = numHelipads
        longitude = long
        latitude = lat
        altitude = alt
        icao = icao
        hasAvGas = hasAvGas
        hasJetFuel = hasJetFuel
        runways = runways
    }
