module BglParsing

open System.IO
open Model
open BglUtils
open BglAirport

type Header = { id: uint16; version: uint16; size: uint32; numSections: uint16 }

type SectionType =
    | None = 0x0u
    | Copyright = 0x1u
    | Guid = 0x2u
    | Airport = 0x03u
    | VorIls = 0x13u
    | Ndb = 0x17u
    | Marker = 0x18u
    | Boundary = 0x20u
    | Waypoint = 0x22u
    | Geopol = 0x23u
    | Scenery = 0x25u
    | Namelist = 0x27u
    | VorIlsIcaoIndex = 0x28u
    | NdbIcaoIndex = 0x29u
    | WaipointIcaoIndex = 0x2au
    | ModelData = 0x2bu
    | AirportSummary = 0x2cu
    | Exclusion = 0x2eu
    | Timezone = 0x2fu
    | AirportAlt = 0x3cu

type Record = 
    | AirportSummary of id: uint16 * longitude: float * latitude: float * altitude: uint32 * icao: string * runwayLength: float32 * runwayHeading: float32
    | Airport of airport: Airport

type Section = { sType: SectionType; subsections: Record array option array }

type Bgl = { header: Header; sections: Section array }

let private readAirportSummary (reader: BinaryReader): Record =
    let id = reader.ReadUInt16()
    reader.ReadUInt32() |> ignore
    reader.ReadUInt16() |> ignore
    let long = reader.ReadUInt32() |> readLongitude
    let lat = reader.ReadUInt32() |> readLatitude
    let alt = reader.ReadUInt32() // in 1/1000 meters
    let icao = reader.ReadUInt32() |> readIcao
    reader.ReadUInt32() |> ignore
    reader.ReadSingle() |> ignore
    let runwayLength = reader.ReadSingle()
    let runwayHeading = reader.ReadSingle()
    reader.ReadUInt32 |> ignore

    AirportSummary(id = id, longitude = long, latitude = lat, altitude = alt, icao = icao, runwayLength = runwayLength, runwayHeading = runwayHeading)

let private readSubsection (reader: BinaryReader) (sType: SectionType): Record array option =
    let id = reader.ReadUInt32()
    let numRecords = reader.ReadUInt32()
    let offset = reader.ReadUInt32()
    let size = reader.ReadUInt32()

    let readFunOpt = 
        match sType with
        | SectionType.AirportSummary -> Option.Some(fun unit -> readAirportSummary reader)
        | SectionType.Airport -> Option.Some(fun unit -> Airport(readAirport reader))
        | _ -> Option.None

    readFunOpt |> Option.map (fun readFun ->
        keepOffset reader (fun () ->
            reader.BaseStream.Position <- (int64) offset
            Seq.toArray(seq { for i in 1 .. (int) numRecords -> readFun() })
        )
    )


let private readSection (reader: BinaryReader, parseOnly: SectionType -> bool): Section =
    let sType = LanguagePrimitives.EnumOfValue<uint32, SectionType>(reader.ReadUInt32())
    reader.ReadInt32() |> ignore // unknown
    let numSubsections = reader.ReadUInt32()
    let offset = reader.ReadUInt32()
    let size = reader.ReadUInt32()

    let subsections = 
        if parseOnly sType then 
            keepOffset reader (fun () ->
                reader.BaseStream.Position <- (int64) offset
                Seq.toArray(seq { for i in 1 .. (int) numSubsections -> readSubsection reader sType }))
        else Array.empty

    { sType = sType; subsections = subsections }

let private readHeader (reader: BinaryReader): Header =
    let withoutSections = { id = reader.ReadUInt16(); version = reader.ReadUInt16(); size = reader.ReadUInt32(); numSections = 0us }
    reader.BaseStream.Seek(12L, SeekOrigin.Current) |> ignore

    { withoutSections with numSections = reader.ReadUInt16() }

let readBgl (parseOnly: SectionType -> bool) (filename: string): Bgl =
    use reader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))

    let header = readHeader reader
    reader.BaseStream.Seek((int64) header.size, SeekOrigin.Begin) |> ignore
    let sections = seq { for i in 1 .. (int) header.numSections -> (readSection(reader, parseOnly)) } |> Seq.toArray

    { header = header; sections = sections }

let readAllBgls (parseOnly: SectionType -> bool) packagesDir =
    let bglFiles = Directory.GetFiles(packagesDir, "*.bgl", SearchOption.AllDirectories)
    seq { for file in bglFiles -> readBgl parseOnly file }

let extractAirports (bgl: Bgl): Airport seq =
    let extractFromRecord (sub: Record array option) =
        match sub with
        | Option.Some(airports) -> Array.toSeq(airports)
        | Option.None -> Seq.empty

    let airports (record: Record) = 
        match record with
        | Airport(airport) -> [| airport |]
        | _ -> Array.empty

    bgl.sections 
        |> Seq.collect (fun section -> Seq.map extractFromRecord section.subsections)
        |> Seq.collect (fun x -> x)
        |> Seq.collect airports

