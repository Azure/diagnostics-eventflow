
module AirTrafficControl {
    export class Position {
        constructor(
            public Latitude: number,
            public Longitude: number,
            public Altitude?: number
        ) { }
    }

    export class AirplaneState {
        constructor(
            public ID: string,
            public StateDescription: string,
            public Location: Position,
            public Heading: number
        ) { }
    }
}