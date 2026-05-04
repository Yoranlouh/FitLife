using SharedLibrary.DTOs.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SharedLibrary.Logic.Algorithm;

public static class ZoneCalculator
{
    public readonly record struct Zones(
        int TopStart, int TopEnd,
        int MidStart, int MidEnd,
        int BotStart, int BotEnd,
        int MidColStart, int MidColEnd
    );

    public static Zones Compute(int numRows, int maxCols)
    {
        int baseR = numRows / 3, extraR = numRows % 3;
        int baseC = maxCols / 3, extraC = maxCols % 3;
        int topR = baseR, midR = baseR + extraR;
        int leftC = baseC, midC = baseC + extraC;
        return new Zones(
            TopStart: 0, TopEnd: topR - 1,
            MidStart: topR, MidEnd: topR + midR - 1,
            BotStart: topR + midR, BotEnd: numRows - 1,
            MidColStart: leftC, MidColEnd: leftC + midC - 1
        );
    }

    public static int Category(int row, int virtualCol, in Zones z)
    {
        bool midRow = row >= z.MidStart && row <= z.MidEnd;
        bool topRow = row >= z.TopStart && row <= z.TopEnd;
        bool midCol = virtualCol >= z.MidColStart && virtualCol <= z.MidColEnd;
        return (midRow, topRow, midCol) switch
        {
            (true, _, true) => 1,
            (_, true, true) => 2,
            (_, _, true) => 3,
            (true, _, false) => 4,
            (_, true, false) => 5,
            _ => 6
        };
    }

    public static int MaxCols(IList<RowConfig> rows) => rows.Max(r => r.Seats);

    public static List<SeatInfo> BuildSeatMap(IList<RowConfig> rows)
    {
        int maxCols = MaxCols(rows), numRows = rows.Count;
        var zones = Compute(numRows, maxCols);
        var result = new List<SeatInfo>(rows.Sum(r => r.Seats));
        for (int r = 0; r < numRows; r++)
        {
            var cfg = rows[r];
            int offset = (maxCols - cfg.Seats) / 2;
            for (int c = 0; c < cfg.Seats; c++)
            {
                int vc = c + offset;
                bool wc = c < cfg.LeftWheelchair || c >= cfg.Seats - cfg.RightWheelchair;
                result.Add(new SeatInfo(r, c, vc, wc ? SeatType.Wheelchair : SeatType.Normal,
                    Category(r, vc, zones)));
            }
        }
        return result;
    }
}

