

namespace ProjectivePlane
{
    using System;
    using System.Text;
    using System.Collections.Generic;

    public class ProjectivePlanePoint<Line> where Line : IComparable<Line>
    {
        public ProjectivePlanePoint()
        {
            this.Lines = new HashSet<Line>();
        }

        public ISet<Line> Lines { get; private set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            bool isFirst = true;
            builder.Append("{");

            foreach (var line in this.Lines)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }

                builder.Append(line);
                isFirst = false;
            }

            builder.Append("}");

            return builder.ToString();
        }

        public ISet<Pair<Line>> GetPairsWith(Line line)
        {
            if (line == null)
            {
                throw new ArgumentNullException("line");
            }

            var pairs = new HashSet<Pair<Line>>();
            foreach (var curLine in this.Lines)
            {
                if (curLine.CompareTo(line) == 0)
                {
                    continue;
                }

                var pair = new Pair<Line>(curLine, line);
                pairs.Add(pair);
            }

            return pairs;
        }

        public ISet<Pair<Line>> GetAllPairs()
        {
            var pairs = new HashSet<Pair<Line>>();
            var lineList = new List<Line>(this.Lines);

            for (int iLeft = 0; iLeft < this.Lines.Count; ++iLeft)
            {
                var left = lineList[iLeft];
                for (int iRight = iLeft + 1; iRight < this.Lines.Count; ++iRight)
                {
                    var right = lineList[iRight];
                    pairs.Add(new Pair<Line>(left, right));
                }
            }

            return pairs;
        }
    }
}
