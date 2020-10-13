module Db

open System.Data.SQLite
open Model

type Db(name: string) =
    let connection = new SQLiteConnection(sprintf "Data Source=%s;Version=3" name)
    do 
        connection.Open()
        connection.LoadExtension("mod_spatialite")

    let command(cmd: string) =
        let command = connection.CreateCommand()
        command.CommandText <- cmd
        command


    member this.InsertAirport(airport: Airport) =
        let cmd = command(@"
            INSERT INTO airports (icao, longitude, latitude, geom_wgs84) 
            SELECT
                $icao AS icao,
                $longitude AS longitude,
                $latitude AS latitude,
                MakePoint($longitude, $latitude, 4326) AS geom_wgs84
        ")
        cmd.Parameters.AddWithValue("$icao", airport.icao) |> ignore
        cmd.Parameters.AddWithValue("$longitude", airport.longitude) |> ignore
        cmd.Parameters.AddWithValue("$latitude", airport.latitude) |> ignore
        try
            cmd.ExecuteNonQuery()
        with
            | :? System.Data.SQLite.SQLiteException as ex -> 
                printfn "caught %s" ex.Message
                0

    member this.Prepare() =
        (command(@"
                SELECT InitSpatialMetadata(1)
        ").ExecuteScalar()) |> ignore

        command(@"
            CREATE TABLE IF NOT EXISTS airports (icao TEXT PRIMARY KEY, longitude DOUBLE, latitude DOUBLE);
        ").ExecuteNonQuery() |> ignore

        let r = (command(@"
                SELECT AddGeometryColumn('airports', 'geom_wgs84', 4326, 'POINT', 'XY', 1)
            ").ExecuteScalar())
        printfn "geom column %s" (string r)

        let r = (command(@"
                SELECT CreateSpatialIndex('airports', 'geom_wgs84')
            ").ExecuteScalar())
        printfn "index %s" (string r)

    member this.Close() =
        connection.Close()
