using SharedLibrary.DTOs.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SharedLibrary.Logic.Algorithm;

public static class SeatFinder
{
    // ── Public entry point ────────────────────────────────────────────────

    public static List<SeatInfo>? FindBest(
        IList<RowConfig> rowConfigs,
        ISet<string> occupied,
        ReservationRequest request)
    {
        if (request.IsMixed)
            return FindMixed(rowConfigs, occupied, request.NormalCount, request.WheelchairCount);

        var type = request.WheelchairOnly ? SeatType.Wheelchair : SeatType.Normal;
        return FindSingleType(rowConfigs, occupied, request.Total, type);
    }

    // ── Single-type ───────────────────────────────────────────────────────

    private static List<SeatInfo>? FindSingleType(
        IList<RowConfig> rows, ISet<string> occupied, int count, SeatType type)
    {
        var ctx = new Ctx(rows);
        bool IsAvail(SeatInfo s) => s.Type == type && !occupied.Contains(Key(s));

        if (count == 1)
        {
            var s = ctx.AllSeats.Where(IsAvail)
                       .OrderBy(x => x.Category).ThenBy(ctx.Dist)
                       .FirstOrDefault();
            return s is null ? null : [s];
        }

        var result = MultiSeedBfs(ctx, count, IsAvail);

        // Bug 2 fix: if no contiguous group exists (e.g. WC seats spread across
        // the hall with no adjacent path between them), fall back to picking the
        // N best individual seats regardless of contiguity.
        if (result is null && count > 1)
            result = BestNIndividual(ctx, count, IsAvail);

        return result;
    }

    // ── Mixed-type ────────────────────────────────────────────────────────

    private static List<SeatInfo>? FindMixed(
        IList<RowConfig> rows, ISet<string> occupied, int normalCount, int wcCount)
    {
        var ctx = new Ctx(rows);
        bool IsWC(SeatInfo s) => s.Type == SeatType.Wheelchair && !occupied.Contains(Key(s));
        bool IsNorm(SeatInfo s) => s.Type == SeatType.Normal && !occupied.Contains(Key(s));

        // Enumerate all possible WC groups (contiguous first, individual fallback)
        var wcGroups = AllGroupsOfSize(ctx, wcCount, IsWC);
        if (wcGroups.Count == 0) return null;

        List<SeatInfo>? best = null;
        double bestSc = double.MaxValue;

        foreach (var wcGroup in wcGroups)
        {
            var normGroup = FindNormalForWc(ctx, wcGroup, normalCount, IsNorm);
            if (normGroup is null) continue;

            var combined = wcGroup.Concat(normGroup).ToList();
            double sc = Score(ctx, combined);
            if (sc < bestSc) { bestSc = sc; best = combined; }
        }

        return best;
    }

    private static List<SeatInfo>? FindNormalForWc(
        Ctx ctx, List<SeatInfo> wcGroup,
        int normalCount, Func<SeatInfo, bool> isNorm)
    {
        // Level 1: same-row, directly beside a WC seat
        var sameRowAdj = wcGroup
            .SelectMany(wc => SameRowNeighbours(ctx, wc).Where(isNorm))
            .DistinctBy(Key).ToList();

        if (sameRowAdj.Count > 0)
        {
            var g = GrowFromSeeds(ctx, sameRowAdj, normalCount, isNorm);
            if (g is not null) return g;
        }

        // Level 2: any direct neighbour (includes rows above/below)
        var anyAdj = wcGroup
            .SelectMany(wc => ctx.Neighbours(wc).Where(isNorm))
            .DistinctBy(Key).ToList();

        if (anyAdj.Count > 0)
        {
            var g = GrowFromSeeds(ctx, anyAdj, normalCount, isNorm);
            if (g is not null) return g;
        }

        // Level 3: closest normal seat by Manhattan distance to WC centroid
        double wcRow = wcGroup.Average(wc => wc.Row);
        double wcVc = wcGroup.Average(wc => wc.VirtualCol);
        var nearest = ctx.AllSeats
            .Where(isNorm)
            .OrderBy(s => Math.Abs(s.Row - wcRow) + Math.Abs(s.VirtualCol - wcVc))
            .FirstOrDefault();

        if (nearest is null) return null;
        return GrowFromSeeds(ctx, [nearest], normalCount, isNorm);
    }

    // ── Core: multi-seed BFS ──────────────────────────────────────────────

    /// <summary>
    /// For each category 1–6, seeds from all available seats of that category
    /// simultaneously, grows a blob, then selects the best 'count' seats with
    /// isolation repair. Returns the globally best candidate.
    /// </summary>
    private static List<SeatInfo>? MultiSeedBfs(
        Ctx ctx, int count, Func<SeatInfo, bool> isAvail)
    {
        List<SeatInfo>? best = null;
        double bestSc = double.MaxValue;

        for (int startCat = 1; startCat <= 6; startCat++)
        {
            var seeds = ctx.AllSeats.Where(s => isAvail(s) && s.Category == startCat).ToList();
            if (seeds.Count == 0) continue;

            var blob = GrowBlob(ctx, seeds, count, isAvail);
            if (blob is null) continue;

            var chosen = SelectWithIsolationRepair(ctx, blob, count);
            if (chosen != null && chosen.Count >= count)
            {
                // VOEG DIT TOE: Stop direct als de gewenste zone resultaat geeft.
                return chosen;
            }
        }

        //for (int startCat = 1; startCat <= 6; startCat++)
        //{
        //    var seeds = ctx.AllSeats
        //        .Where(s => isAvail(s) && s.Category == startCat)
        //        .ToList();
        //    if (seeds.Count == 0) continue;

        //    // Grow blob of size ≥ count from all seeds simultaneously
        //    var blob = GrowBlob(ctx, seeds, count, isAvail);
        //    if (blob is null) continue;

        //    // Select best 'count' from blob with isolation repair
        //    var chosen = SelectWithIsolationRepair(ctx, blob, count);
        //    if (chosen is null || chosen.Count < count) continue;

        //    // Side-by-side check
        //    if (count > 1 && !HasAdjacentPair(chosen)) continue;

        //    double sc = Score(ctx, chosen);
        //    if (sc < bestSc) { bestSc = sc; best = chosen; }
        //}

        return best;
    }

    /// <summary>
    /// Grows a blob from multiple seeds until it reaches at least 'count' seats.
    /// Always picks the best-scoring frontier seat next.
    /// </summary>
    private static List<SeatInfo>? GrowBlob(
        Ctx ctx, List<SeatInfo> seeds, int count, Func<SeatInfo, bool> isAvail)
    {
        var seated = new Dictionary<string, SeatInfo>(seeds.ToDictionary(Key));
        var list = new List<SeatInfo>(seeds);
        var frontier = new Dictionary<string, SeatInfo>();

        foreach (var s in seeds)
            foreach (var nb in ctx.Neighbours(s).Where(n => isAvail(n) && !seated.ContainsKey(Key(n))))
                frontier.TryAdd(Key(nb), nb);

        while (list.Count < count && frontier.Count > 0)
        {
            var next = frontier.Values
                .OrderBy(s => s.Category)
                .ThenBy(ctx.Dist)
                .First();

            frontier.Remove(Key(next));
            if (seated.ContainsKey(Key(next))) continue;

            seated[Key(next)] = next;
            list.Add(next);

            foreach (var nb in ctx.Neighbours(next).Where(n => isAvail(n) && !seated.ContainsKey(Key(n))))
                frontier.TryAdd(Key(nb), nb);
        }

        return list.Count >= count ? list : null;
    }

    /// <summary>
    /// Selects the best 'count' seats from a blob, then iteratively repairs
    /// isolation: if a seat is alone in its row and has a row-neighbour in the
    /// blob that is not yet selected, swap in the neighbour by removing the
    /// worst non-isolated seat.
    /// </summary>
    private static List<SeatInfo>? SelectWithIsolationRepair(
        Ctx ctx, List<SeatInfo> blob, int count)
    {

        double midCol = ctx.AllSeats.Max(s => s.VirtualCol) / 2.0;

        // 1. Probeer de hele groep op één rij (Best Fit)
        var singleRow = blob.GroupBy(s => s.Row)
            .Where(g => g.Count() >= count)
            .OrderBy(g => Math.Abs(g.Key - (ctx.AllSeats.Max(s => s.Row) / 2.0)))
            .Select(g => g.OrderBy(s => Math.Abs(s.VirtualCol - midCol)).Take(count).ToList())
            .FirstOrDefault();

        if (singleRow != null) return singleRow;

        // 2. Als 1 rij niet past: Verdeel over TWEE aangrenzende rijen (Split Fit)
        // We zoeken naar twee rijen die samen genoeg plek hebben en verdelen de groep 50/50 of 60/40
        var rows = blob.GroupBy(s => s.Row).OrderBy(g => g.Key).ToList();
        for (int i = 0; i < rows.Count - 1; i++)
        {
            var row1 = rows[i];
            var row2 = rows[i + 1];

            if (row1.Count() + row2.Count() >= count)
            {
                // Bereken een eerlijke verdeling (bijv. 6 wordt 3 om 3, 5 wordt 3 om 2)
                int countRow1 = (int)Math.Ceiling(count / 2.0);
                int countRow2 = count - countRow1;

                // Check of deze verdeling past in deze twee specifieke rijen
                if (row1.Count() >= countRow1 && row2.Count() >= countRow2)
                {
                    var part1 = row1.OrderBy(s => Math.Abs(s.VirtualCol - midCol)).Take(countRow1);
                    var part2 = row2.OrderBy(s => Math.Abs(s.VirtualCol - midCol)).Take(countRow2);
                    return part1.Concat(part2).ToList();
                }
            }
        }

        // 3. Absolute Fallback: Sorteer alles op 'compactheid' rondom het midden
        return blob.OrderBy(s => Math.Abs(s.VirtualCol - midCol))
                   .ThenBy(s => s.Row)
                   .Take(count)
                   .ToList();
    }

    // ── Group enumeration ─────────────────────────────────────────────────

    /// <summary>
    /// Returns all unique groups of exactly 'count' available seats by growing
    /// from every available seed. Contiguous groups are tried first.
    /// For WC-only with count > 1 and no contiguous path: falls back to best-N.
    /// </summary>
    private static List<List<SeatInfo>> AllGroupsOfSize(
        Ctx ctx, int count, Func<SeatInfo, bool> isAvail)
    {
        if (count == 1)
        {
            return ctx.AllSeats.Where(isAvail)
                .Select(s => new List<SeatInfo> { s })
                .ToList();
        }

        var seen = new HashSet<string>();
        var result = new List<List<SeatInfo>>();

        foreach (var seed in ctx.AllSeats.Where(isAvail))
        {
            var g = GrowFromSeeds(ctx, [seed], count, isAvail);
            if (g is null || g.Count < count) continue;

            var canonical = string.Join(",", g.Select(Key).OrderBy(k => k));
            if (seen.Add(canonical)) result.Add(g);
        }

        // Bug 2 fallback: no contiguous group found → best N individual seats
        if (result.Count == 0)
        {
            var best = BestNIndividual(ctx, count, isAvail);
            if (best is not null) result.Add(best);
        }

        return result;
    }

    /// <summary>Bug 2 fallback: picks the N best seats by score, ignoring contiguity.</summary>
    private static List<SeatInfo>? BestNIndividual(
        Ctx ctx, int count, Func<SeatInfo, bool> isAvail)
    {
        var seats = ctx.AllSeats.Where(isAvail)
            .OrderBy(s => s.Category)
            .ThenBy(ctx.Dist)
            .Take(count)
            .ToList();
        return seats.Count == count ? seats : null;
    }

    // ── Growth helpers ────────────────────────────────────────────────────

    private static List<SeatInfo>? GrowFromSeeds(
        Ctx ctx, List<SeatInfo> seeds, int target, Func<SeatInfo, bool> isAvail)
    {
        var availSeeds = seeds.Where(isAvail).DistinctBy(Key).ToList();
        if (availSeeds.Count == 0) return null;

        var seated = new Dictionary<string, SeatInfo>(availSeeds.ToDictionary(Key));
        var list = new List<SeatInfo>(availSeeds);
        var frontier = new Dictionary<string, SeatInfo>();

        foreach (var s in availSeeds)
            foreach (var nb in ctx.Neighbours(s).Where(n => isAvail(n) && !seated.ContainsKey(Key(n))))
                frontier.TryAdd(Key(nb), nb);

        while (list.Count < target && frontier.Count > 0)
        {
            var next = frontier.Values.OrderBy(s => s.Category).ThenBy(ctx.Dist).First();
            frontier.Remove(Key(next));
            if (seated.ContainsKey(Key(next))) continue;
            seated[Key(next)] = next;
            list.Add(next);
            foreach (var nb in ctx.Neighbours(next).Where(n => isAvail(n) && !seated.ContainsKey(Key(n))))
                frontier.TryAdd(Key(nb), nb);
        }

        return list.Count >= target ? list.Take(target).ToList() : null;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private static bool HasAdjacentPair(List<SeatInfo> seats)
    {
        var set = seats.Select(Key).ToHashSet();
        return seats.Any(s =>
            set.Contains($"{s.Row}-{s.Col - 1}") ||
            set.Contains($"{s.Row}-{s.Col + 1}"));
    }

    private static IEnumerable<SeatInfo> SameRowNeighbours(Ctx ctx, SeatInfo s)
    {
        if (ctx.ByPos.TryGetValue((s.Row, s.Col - 1), out var l)) yield return l;
        if (ctx.ByPos.TryGetValue((s.Row, s.Col + 1), out var r)) yield return r;
    }

    private static double Score(Ctx ctx, List<SeatInfo> g) =>
        g.Sum(s => s.Category) * 10_000 + g.Sum(ctx.Dist);

    public static string Key(SeatInfo s) => $"{s.Row}-{s.Col}";

    // ── Context ───────────────────────────────────────────────────────────

    private sealed class Ctx
    {
        public List<SeatInfo> AllSeats { get; }
        public Dictionary<(int, int), SeatInfo> ByPos { get; }
        private readonly double _cR, _cVC;
        private readonly int _numRows;

        public Ctx(IList<RowConfig> rows)
        {
            AllSeats = ZoneCalculator.BuildSeatMap(rows);
            ByPos = AllSeats.ToDictionary(s => (s.Row, s.Col));
            _numRows = rows.Count;
            _cR = (_numRows - 1) / 2.0;
            _cVC = (ZoneCalculator.MaxCols(rows) - 1) / 2.0;
        }

        public double Dist(SeatInfo s) =>
            Math.Abs(s.Row - _cR) + Math.Abs(s.VirtualCol - _cVC);

        public IEnumerable<SeatInfo> Neighbours(SeatInfo s)
        {
            if (ByPos.TryGetValue((s.Row, s.Col - 1), out var l)) yield return l;
            if (ByPos.TryGetValue((s.Row, s.Col + 1), out var r)) yield return r;

            foreach (int dr in new[] { -1, 1 })
            {
                int nr = s.Row + dr;
                if (nr < 0 || nr >= _numRows) continue;

                SeatInfo? best = null;
                int bestDiff = int.MaxValue;
                foreach (var ns in AllSeats.Where(x => x.Row == nr))
                {
                    int diff = Math.Abs(ns.VirtualCol - s.VirtualCol);
                    if (diff < bestDiff) { bestDiff = diff; best = ns; }
                }
                if (best is not null && bestDiff <= 1) yield return best;
            }
        }
    }
}

