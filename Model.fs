module Model

type SurfaceType =
    | Concrete = 0x00us
    | Grass = 0x001us
    | Water = 0x002us
    | Asphalt = 0x004us
    | Clay = 0x007us
    | Snow = 0x008us
    | Ice = 0x009us
    | Dirt = 0x00cus
    | Coral = 0x00dus
    | Gravel = 0x00eus
    | OilTreated = 0x00fus
    | SteelMats = 0x010us
    | Bituminous = 0x011us
    | Brick = 0x012us
    | Macadam = 0x013us
    | Planks = 0x014us
    | Sand = 0x015us
    | Shale = 0x016us
    | Tarmac = 0x017us

type Runway = {
    surface: SurfaceType
    number: uint8
    designator: uint8
    length: float32
    width: float32
    heading: float32
    isLighted: bool
}

type Airport = {
    id: uint16
    size: uint32
    numRunways: byte
    numComs: byte
    numStarts: byte
    numApproaches: byte
    numAprons: byte
    numHelipads: byte
    longitude: float
    latitude: float
    altitude: uint32
    icao: string
    hasAvGas: bool
    hasJetFuel: bool
    runways: Runway seq
}
