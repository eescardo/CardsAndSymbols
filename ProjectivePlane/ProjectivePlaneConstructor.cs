
namespace ProjectivePlane
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Uses Desarguesian projective plane construction to assign lines to points such that:
    /// 1) Every two points have exactly one line in common
    /// 2) Each possible line pair appears in at most one point (if the maximal number of points
    ///    possible for a given projective plane order is chosen, each possible line pair will
    ///    appear in exactly one point)
    /// </summary>
    public class ProjectivePlaneConstructor<Line> where Line : IComparable<Line>
    {
        /// <summary>
        /// Supported projective plane orders that will be used to perform distribution
        /// </summary>
        /// <remarks>
        /// Only prime number orders supported right now given the kind of construction we're doing.
        /// </remarks>
        private readonly int[] supportedOrders = { 1, 2, 3, 5, 7, 11, 13, 17, 19 };

        private readonly int order;
        private readonly List<Line> lines;
        private readonly List<ProjectivePlanePoint<Line>> fullPointSet;
        private readonly List<Direction> directions;
        private readonly int numRequestedPoints;

        private bool hasRun = false;
        private List<ProjectivePlanePoint<Line>> requestedPoints;

        public ProjectivePlaneConstructor(IList<Line> lines, int numPoints)
        {
            var maxPointsPerOrder = supportedOrders.Select(o => (o * o) + o + 1).ToArray();
            int iOrder = 0;
            for (; iOrder < supportedOrders.Length; ++iOrder)
            {
                if (numPoints <= maxPointsPerOrder[iOrder])
                {
                    break;
                }
            }

            if (iOrder >= supportedOrders.Length)
            {
                throw new ArgumentException(string.Format("Unable to support '{0}' points. Please specify a smaller number", numPoints), "numPoints");
            }

            var maxPoints = maxPointsPerOrder[iOrder];

            if (lines.Count < maxPoints)
            {
                throw new ArgumentException(string.Format("Unable to assign lines to points. Please specify at least {0} line objects", maxPoints), "lines");
            }

            if (lines.Count > maxPoints)
            {
                Trace.TraceWarning("Not all of the line objects will be used. Only {0} line objects are required for the specified number of points.", maxPoints);
            }

            this.order = supportedOrders[iOrder];
            this.fullPointSet = new List<ProjectivePlanePoint<Line>>(maxPoints);
            for (int iPoint = 0; iPoint < maxPoints; ++iPoint)
            {
                this.fullPointSet.Add(new ProjectivePlanePoint<Line>());
            }

            this.numRequestedPoints = numPoints;
            this.lines = lines.Take(maxPoints).ToList();

            this.directions = new List<Direction>(this.order + 1);
            this.directions.Add(new Direction { RowInc = 1, ColInc = 0 }); // Initialize vertical direction
            for (int i = 0; i < this.order; ++i)
            {
                // Initialize directions with horizontal movement
                this.directions.Add(new Direction { RowInc = i, ColInc = 1 });
            }
        }

        public IList<ProjectivePlanePoint<Line>> PlanePoints
        {
            get
            {
                if (this.requestedPoints == null)
                {
                    this.Run();

                    this.requestedPoints = this.fullPointSet.Take(this.numRequestedPoints).ToList();
                }

                return requestedPoints;
            }
        }

        /// <summary>
        /// Verify that each pair of points has exactly one pair of lines in common.
        /// </summary>
        /// <param name="points">
        /// List of points to verify
        /// </param>
        /// <returns>
        /// true if the set of points satisfies the constraint.
        /// false otherwise.
        /// </returns>
        public static bool VerifyPointLines(IList<ProjectivePlanePoint<Line>> points)
        {
            var assignedLinePairs = new HashSet<Pair<Line>>();

            for (int iLeft = 0; iLeft < points.Count; ++iLeft)
            {
                var left = points[iLeft];
                var leftPairs = left.GetAllPairs();

                foreach (var pair in leftPairs)
                {
                    // A line pair must only exist in a single point
                    if (assignedLinePairs.Contains(pair))
                    {
                        return false;
                    }
                    assignedLinePairs.Add(pair);
                }

                for (int iRight = iLeft + 1; iRight < points.Count; ++iRight)
                {
                    var right = points[iRight];
                    var intersectionCount = 0;

                    foreach (var rightLine in right.Lines)
                    {
                        if (left.Lines.Contains(rightLine))
                        {
                            ++intersectionCount;
                        }
                    }

                    // All point pairs must have exactly one line in common
                    if (intersectionCount != 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void Run()
        {
            if (this.hasRun)
            {
                return;
            }

            var projectionLine = this.GetProjectionLine();

            for (int iDirection = 0; iDirection < this.directions.Count; ++iDirection)
            {
                var direction = this.directions[iDirection];
                var projectionPoint = this.GetProjectionPoint(iDirection);

                for (int iteration = 0; iteration < this.order; ++iteration)
                {
                    var start = this.GetStartingCoord(iDirection, iteration);
                    var line = this.GetLine(iDirection, iteration);

                    for (int iPoint = 0; iPoint < this.order; ++iPoint)
                    {
                        var point = this.GetGridPoint(start.Row + (iPoint * direction.RowInc), start.Col + (iPoint * direction.ColInc));
                        point.Lines.Add(line);
                    }

                    projectionPoint.Lines.Add(line);
                }

                projectionPoint.Lines.Add(projectionLine);
            }

            this.hasRun = true;
        }

        private ProjectivePlanePoint<Line> GetGridPoint(int row, int col)
        {
            var adjustedRow = row % this.order;
            var adjustedCol = col % this.order;
            var pointIndex = (adjustedRow * this.order) + adjustedCol;
            return this.fullPointSet[pointIndex];
        }

        private ProjectivePlanePoint<Line> GetProjectionPoint(int iDirection)
        {
            var pointIndex = (this.order * this.order) + iDirection;
            return this.fullPointSet[pointIndex];
        }

        private Line GetLine(int iDirection, int iteration)
        {
            var lineIndex = (iDirection * this.order) + iteration;
            return this.lines[lineIndex];
        }

        private Line GetProjectionLine()
        {
            return this.lines[this.fullPointSet.Count - 1];
        }

        private Coord GetStartingCoord(int iDirection, int iteration)
        {
            // For vertical direction, increment row without incrementing column
            if (iDirection == 0)
            {
                return new Coord { Row = 0, Col = iteration };
            }

            // For all other directions, increment column without incrementing row
            return new Coord { Row = iteration, Col = 0 };
        }

        private struct Coord
        {
            public int Row { get; set; }
            public int Col { get; set; }
        }

        private struct Direction
        {
            public int RowInc { get; set; }
            public int ColInc { get; set; }
        }
    }
}
