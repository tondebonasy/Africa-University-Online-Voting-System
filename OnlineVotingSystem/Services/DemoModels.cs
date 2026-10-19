using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVotingSystem.Services
{
    public sealed class DemoChoice
    {
        public int NominationId { get; set; }
        public string Name { get; set; }
        public int Votes { get; set; }
        public string Manifesto { get; set; }
    }
    public sealed class DemoWinner
    {
        public string Seat { get; set; }
        public string Name { get; set; }
        public int NominationId { get; set; }
    }
    public sealed class DemoAllocation
    {
        public List<DemoWinner> Winners { get; set; } = new List<DemoWinner>();
        public List<int> TiedNominees { get; set; } = new List<int>();
        public string PendingSeat { get; set; }
        public int PendingSeats { get; set; }
        public bool NoVotes { get; set; }
        public bool Resolved { get { return !NoVotes && TiedNominees.Count == 0; } }
    }
    public static class DemoRules
    {
        // IDs and names never break ties. Ordering is used only for unequal vote totals.
        public static DemoAllocation Allocate(IEnumerable<DemoChoice> input, int seats, string seatLabel)
        {
            var choices = input.OrderByDescending(c => c.Votes).ToList();
            var result = new DemoAllocation();
            if (choices.Count < seats || choices.Sum(c => c.Votes) == 0) { result.NoVotes = true; return result; }
            var labels = seats == 2 ? new[] { "President", "Vice President" } : new[] { seatLabel };
            for (var place = 0; place < seats; place++)
            {
                var group = choices.Where(c => c.Votes == choices[place].Votes).ToList();
                if (group.Count > 1)
                {
                    result.TiedNominees = group.Select(c => c.NominationId).ToList();
                    result.PendingSeats = seats - place;
                    result.PendingSeat = result.PendingSeats == 2 ? "President and Vice President" : labels[place];
                    return result;
                }
                result.Winners.Add(new DemoWinner { Seat = labels[place], Name = choices[place].Name, NominationId = choices[place].NominationId });
            }
            return result;
        }
    }
    public sealed class DemoContestView
    {
        public int SessionId { get; set; }
        public string Name { get; set; }
        public string Phase { get; set; }
        public int? ParentSessionId { get; set; }
        public int Seats { get; set; }
        public string SeatLabel { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int Participation { get; set; }
        public List<DemoChoice> Choices { get; set; } = new List<DemoChoice>();
        public DemoAllocation Allocation { get; set; }
    }
    public sealed class DemoDashboard
    {
        public bool Seeded { get; set; }
        public int Candidates { get; set; }
        public int Voters { get; set; }
        public int Eligible { get; set; }
        public int Ballots { get; set; }
        public bool Published { get; set; }
        public List<DemoContestView> Contests { get; set; } = new List<DemoContestView>();
    }
    public sealed class BallotOutcome
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
