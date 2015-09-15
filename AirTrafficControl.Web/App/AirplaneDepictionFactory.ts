
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />

module AirTrafficControl {
    import Maps = Microsoft.Maps;

    class Heading2ImageDictionaryEntry {
        constructor(public MinHeading: number, public MaxHeading: number, public ImageName: string) { }
    }

    export class AirplaneDepictionFactory { 
        private static ScalingLattitudeFactor: number = 10000.0;
        private static ScalingLongitudeFactor: number = 10000.0;
        private HeadingMap: Heading2ImageDictionaryEntry[];           

        constructor() {
            var pi8 = Math.PI / 8.0;
            this.HeadingMap = [
                new Heading2ImageDictionaryEntry(0, pi8, "airplane_N.svg"),
                new Heading2ImageDictionaryEntry(pi8, 3 * pi8, "airplane_NE.svg"),
                new Heading2ImageDictionaryEntry(3 * pi8, 5 * pi8, "airplane_E.svg"),
                new Heading2ImageDictionaryEntry(5 * pi8, Math.PI - pi8, "airplane_SE.svg"),
                new Heading2ImageDictionaryEntry(Math.PI - pi8, Math.PI + pi8, "airplane_S.svg"),
                new Heading2ImageDictionaryEntry(Math.PI + pi8, Math.PI + 3 * pi8, "airplane_SW.svg"),
                new Heading2ImageDictionaryEntry(Math.PI + 3 * pi8, Math.PI + 5 * pi8, "airplane_W.svg"),
                new Heading2ImageDictionaryEntry(Math.PI + 5 * pi8, 2 * Math.PI - pi8, "airplane_NW.svg"),
                new Heading2ImageDictionaryEntry(2 * Math.PI - pi8, 2 * Math.PI, "airplane_N.svg")
            ];
        }

        public GetAirplaneDepiction(map: Maps.Map, location: Maps.Location, heading: number): Maps.EntityCollection {
            return this.GetIconAirplaneDepiction(map, location, heading);            
        }

        private GetIconAirplaneDepiction(map: Maps.Map, location: Maps.Location, heading: number): Maps.EntityCollection {
            var collectionOptions: Maps.EntityCollectionOptions = { bubble: true, visible: true, zIndex: 100 };
            var collection = new Maps.EntityCollection(collectionOptions);

            var imageName: string;
            for (var i: number = 0; i < this.HeadingMap.length; i++) {
                if (heading >= this.HeadingMap[i].MinHeading && heading <= this.HeadingMap[i].MaxHeading) {
                    imageName = this.HeadingMap[i].ImageName;
                    break;
                }
            }

            if (!imageName) {
                console.log('WARNING: Could not find proper ariplane image for given heading');
                imageName = "airplane_N.svg";
            }

            var pin = new Microsoft.Maps.Pushpin(location, {
                htmlContent: '<img src="Content/' + imageName + '" height="110px" width="60px" style="padding: 80px 0px 0px 30px" />',
                height: 100,
                width: 100,
                draggable: false,
                visible: true
            }); 

            collection.push(pin);
            return collection;
        }

        private GetVectorAirplaneDepiction(map: Maps.Map, location: Maps.Location, heading: number): Maps.EntityCollection {
            var collectionOptions: Maps.EntityCollectionOptions = { bubble: true, visible: true, zIndex: 100 };
            var collection = new Maps.EntityCollection(collectionOptions);

            var airplaneOutlinePoints = this.GetAirplaneOutline();
            var vertices: Maps.Location[] = [];

            var scalingLonFactor = map.getBounds().width / AirplaneDepictionFactory.ScalingLongitudeFactor;
            var scalingLatFactor = map.getBounds().height / AirplaneDepictionFactory.ScalingLattitudeFactor;

            var cosh = Math.cos(heading);
            var sinh = Math.sin(heading);

            for (var i = 0; i < airplaneOutlinePoints.length; i++) {
                var point = airplaneOutlinePoints[i];                
                
                // We will interpret the heading as in aviation (45 degrees, or PI/4 radians is northeast), and not like the standard mathematical convention
                // where a positive turn is counterclockwise. Thus the formula computing rotated vertice coordinates is a bit different from the "book formula".
                // See http://stackoverflow.com/questions/3162643/proper-trigonometry-for-rotating-a-point-around-the-origin
                var rotatedPoint = new Maps.Point(point.x * cosh + point.y * sinh, point.y * cosh - point.x * sinh);

                vertices.push(new Maps.Location(location.latitude + rotatedPoint.y * scalingLatFactor, location.longitude + rotatedPoint.x * scalingLonFactor));
            }

            var options = { strokeColor: new Microsoft.Maps.Color(255, 0, 0, 255), strokeThickness: 3 };
            var polygon = new Maps.Polygon(vertices, options);
            // TODO: add Infobox with airplane id

            collection.push(polygon);

            return collection;
        }

        private GetAirplaneOutline(): Maps.Point[] {
            var retval: Maps.Point[] = [];

            var nose = new Maps.Point(0, 180);
            retval.push(nose);

            var rCockpit = new Maps.Point(20, 140);
            retval.push(rCockpit);

            var rWingRoot = new Maps.Point(20, 60);
            retval.push(rWingRoot);
            var rWingFwdTip = new Maps.Point(280, 0);
            retval.push(rWingFwdTip);
            var rWingBackTip = new Maps.Point(280, -30);
            retval.push(rWingBackTip);
            var rWingRootBack = new Maps.Point(20, -20);
            retval.push(rWingRootBack);

            var rHorizontalStabilizerRoot = new Maps.Point(20, -140);
            retval.push(rHorizontalStabilizerRoot);
            var rHorizontalStabilizerFwdTip = new Maps.Point(100, -150);
            retval.push(rHorizontalStabilizerFwdTip);
            var rHorizontalStabilizerBackTip = new Maps.Point(100, -170);
            retval.push(rHorizontalStabilizerBackTip);

            var tail = new Maps.Point(0, -160);
            retval.push(tail);

            var lHorizontalStabilizerBackTip = new Maps.Point(-100, -170);
            retval.push(lHorizontalStabilizerBackTip);
            var lHorizontalStabilizerFwdTip = new Maps.Point(-100, -150);
            retval.push(lHorizontalStabilizerFwdTip);
            var lHorizontalStabilizerRoot = new Maps.Point(-20, -140);
            retval.push(lHorizontalStabilizerRoot);

            var lWingRootBack = new Maps.Point(-20, -20);
            retval.push(lWingRootBack);
            var lWingBackTip = new Maps.Point(-280, -30);
            retval.push(lWingBackTip);
            var lWingFwdTip = new Maps.Point(-280, 0);
            retval.push(lWingFwdTip);
            var lWingRoot = new Maps.Point(-20, 60);
            retval.push(lWingRoot);

            var lCockpit = new Maps.Point(-20, 140);
            retval.push(lCockpit);

            retval.push(nose);

            return retval;
        }
    }

}