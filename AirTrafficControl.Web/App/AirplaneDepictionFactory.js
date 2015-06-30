/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var Maps = Microsoft.Maps;
    var Direction = (function () {
        function Direction(x, y) {
            this.x = x;
            this.y = y;
            // Normalize x and y components, so that the length of the 'Direction' vector is one.
            var len = Math.sqrt(x * x + y * y);
            this.XComponent = x / len;
            this.YComponent = y / len;
        }
        return Direction;
    })();
    AirTrafficControl.Direction = Direction;
    var AirplaneDepictionFactory = (function () {
        function AirplaneDepictionFactory() {
        }
        AirplaneDepictionFactory.GetScalingFactor = function (currentMapZoom) {
            var bigger = AirplaneDepictionFactory.ScalingFactors[Math.ceil(currentMapZoom)];
            var smaller = AirplaneDepictionFactory.ScalingFactors[Math.floor(currentMapZoom)];
            return (bigger + smaller) / 2.0;
        };
        AirplaneDepictionFactory.GetAirplaneOutline = function () {
            var retval = [];
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
            var tail = new Maps.Point(0, -165);
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
        };
        AirplaneDepictionFactory.GetAirplaneDepiction = function (location, direction, currentMapZoom) {
            var collectionOptions = { bubble: true, visible: true, zIndex: 100 };
            var collection = new Maps.EntityCollection(collectionOptions);
            var airplaneOutlinePoints = AirplaneDepictionFactory.GetAirplaneOutline();
            var vertices = [];
            var scalingFactor = AirplaneDepictionFactory.GetScalingFactor(currentMapZoom);
            console.log("Scaling factor is %f", scalingFactor);
            for (var i = 0; i < airplaneOutlinePoints.length; i++) {
                vertices.push(new Maps.Location(location.latitude + airplaneOutlinePoints[i].y * scalingFactor, location.longitude + airplaneOutlinePoints[i].x * scalingFactor));
            }
            var options = { strokeColor: new Microsoft.Maps.Color(255, 0, 0, 255), strokeThickness: 3 };
            var polygon = new Maps.Polygon(vertices, options);
            // TODO: add Infobox with airplane id
            collection.push(polygon);
            return collection;
        };
        AirplaneDepictionFactory.ScalingFactors = [
            0.08,
            0.04,
            0.02,
            0.016,
            0.008,
            0.004,
            0.002,
            0.001,
            0.0005,
            0.0002,
            0.0001,
            0.00005,
            0.000024,
            0.000014,
            0.000007,
            0.0000030,
            0.0000016,
            0.0000008,
            0.0000004,
            0.0000002
        ];
        return AirplaneDepictionFactory;
    })();
    AirTrafficControl.AirplaneDepictionFactory = AirplaneDepictionFactory;
})(AirTrafficControl || (AirTrafficControl = {}));
