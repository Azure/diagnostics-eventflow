
module AirTrafficControl {
    export class Position {
        constructor(
            public Latitude: number,
            public Longitude: number,
            public Altitude?: number
        ) { }
    }

    export class Fix {
        constructor(
            public Location: Position
        ) { }
    }

    export class AirplaneState {
        constructor(
            public ID: string,
            public StateDescription: string,
            public Location: Position,
            public Heading: number,
            public EnrouteFrom?: Fix,
            public EnrouteTo?: Fix
        ) { }
    }

    export class FlightStatusModel {
        constructor(
            public AirplaneStates: AirplaneState[],
            public EstimatedNextStatusUpdateDelayMsec: number
        ) { }
    }
}