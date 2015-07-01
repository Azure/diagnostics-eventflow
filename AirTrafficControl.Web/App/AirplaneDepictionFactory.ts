
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />

module AirTrafficControl {
    import Maps = Microsoft.Maps;

    export class AirplaneDepictionFactory {
        private static ScalingLattitudeFactor: number = 5000.0;
        private static ScalingLongitudeFactor: number = 4000.0;

        private static GetAirplaneOutline(): Maps.Point[]{
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

            return retval;
        }

        static GetAirplaneDepiction(map: Maps.Map, location: Maps.Location, heading: number, currentMapZoom: number): Maps.EntityCollection {
            var collectionOptions: Maps.EntityCollectionOptions = {bubble: true, visible:true, zIndex:100};
            var collection = new Maps.EntityCollection(collectionOptions);

            var airplaneOutlinePoints = AirplaneDepictionFactory.GetAirplaneOutline();
            var vertices: Maps.Location[] = [];

            var scalingLonFactor = map.getBounds().width / AirplaneDepictionFactory.ScalingLongitudeFactor;
            var scalingLatFactor = map.getBounds().height / AirplaneDepictionFactory.ScalingLattitudeFactor;

            // TODO: rotate the airplane to point in "direction" direction
            for (var i = 0; i < airplaneOutlinePoints.length; i++) {
                vertices.push(new Maps.Location(location.latitude + airplaneOutlinePoints[i].y * scalingLatFactor, location.longitude + airplaneOutlinePoints[i].x * scalingLonFactor));
            }

            var options = { strokeColor: new Microsoft.Maps.Color(255, 0, 0, 255), strokeThickness: 3 };
            var polygon = new Maps.Polygon(vertices, options);
            // TODO: add Infobox with airplane id

            collection.push(polygon);

            return collection;
        }
    }

}