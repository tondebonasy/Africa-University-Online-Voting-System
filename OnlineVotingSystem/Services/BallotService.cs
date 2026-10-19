using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using OnlineVotingSystem.Models;

namespace OnlineVotingSystem.Services
{
    public sealed class BallotService
    {
        // Both the controller and simulator enter the same validated demo ballot path.
        internal static BallotOutcome CastDemoValidated(SqlConnection c,SqlTransaction t,string username,int nomination,int session)
        { return DemoElectionService.CastInside(c,t,username,nomination,session); }
        public async Task<BallotOutcome> CastAsync(string username,int nomination)
        {
            if(DemoEnvironment.Enabled) return new DemoElectionService().Cast(username,nomination);
            // Compatibility adapter: the real application's existing storage is unchanged.
            using(var db=new OnlineVotingSystemEntities()) using(var transaction=db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var now=DateTime.Now;
                var session=await db.VotingSessions.Where(s=>s.startDay<=now&&s.endDate>now).OrderByDescending(s=>s.Id).FirstOrDefaultAsync();
                if(session==null)return new BallotOutcome {Message="There is no election open for voting right now."};
                var candidate=await db.CandidateStructures.FindAsync(nomination);
                if(candidate==null)return new BallotOutcome {Message="This candidate nomination was not found."};
                await db.Database.ExecuteSqlCommandAsync("SELECT Id FROM dbo.VotingSession WITH (UPDLOCK,HOLDLOCK) WHERE Id=@p0",session.Id);
                if(await db.userVotes.AnyAsync(v=>v.sessionId==session.Id&&v.username==username&&v.CandidateStructure.structureId==candidate.structureId))return new BallotOutcome {Message="You have already voted for this position in this election."};
                db.userVotes.Add(new userVote {candidateStructureId=candidate.id,sessionId=session.Id,username=username});
                var tally=await db.Votes.FirstOrDefaultAsync(v=>v.sessionId==session.Id&&v.candidateId==candidate.candidateId);
                if(tally==null)db.Votes.Add(new Vote {candidateId=candidate.candidateId,sessionId=session.Id,voteCount=1});else tally.voteCount=(tally.voteCount??0)+1;
                await db.SaveChangesAsync(); transaction.Commit();
                return new BallotOutcome {Success=true,Message="Your vote has been recorded. You can continue voting for other positions."};
            }
        }
    }
}
