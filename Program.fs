open BglParsing
open Model


let readAll() =
    let packagesDir = "D:\\games\\MSFS2020\\Packages"
    let bgls = readAllBgls (fun t -> t = SectionType.Airport (*|| t = SectionType.AirportSummary*)) packagesDir

    let airports = 
        bgls 
            |> Seq.collect extractAirports
            |> Seq.fold (fun (map: Map<string, Airport>) (ap: Airport) -> 
                match map.TryFind(ap.icao) with
                | None -> map.Add(ap.icao, ap)
                | Some(apOld) -> if apOld.numRunways = 0uy then map.Add(ap.icao, ap) else map
            ) (Map.empty<string, Airport>)


    printfn "map count: %i" airports.Count
    printfn "EDDF: %s" (string airports.["EDDF"])

let readKTEX() =
    let bgl = readBgl (fun t -> t = SectionType.Airport) "D:\\games\\MSFS2020\\Packages\\Official\\Steam\\asobo-airport-ktex-telluride\\scenery\\KTEX.bgl"
    printfn "BGL: %s" (string bgl)

    

[<EntryPoint>]
let main argv =

//    let db = new Db("meep.db")
//    db.Prepare()
    
//    if argv.Length > 0 && argv.[0] = "wipe" then
//        printfn "yay"

    readAll()

    0
