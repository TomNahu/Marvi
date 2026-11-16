using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Marvi
{
    internal enum StudyMode { Pomodoro, Flowtime }
    internal enum StudyStage { Idle, Focus, ChooseBreak, Break, FocusDue, BreakDue, Summary }

    [DataContract]
    internal sealed class StudySession
    {
        [DataMember] public StudyMode Mode;
        [DataMember] public StudyStage Stage;
        [DataMember] public string Goal="", Note="", Outcome="";
        [DataMember] public long SessionStartUtc, StageStartUtc, DueUtc;
        [DataMember] public long FocusTicks, BreakTicks, LastStretchTicks;
        [DataMember] public bool NoticeShown;

        public bool Active { get { return Stage!=StudyStage.Idle && Stage!=StudyStage.Summary; } }
        public bool Pending { get { return Stage==StudyStage.FocusDue || Stage==StudyStage.BreakDue; } }

        public static StudySession Load(string file)
        {
            if(!File.Exists(file)) return new StudySession();
            using(var input=File.OpenRead(file))
            {
                var result=(StudySession)new DataContractJsonSerializer(typeof(StudySession)).ReadObject(input);
                if(result==null || !Enum.IsDefined(typeof(StudyStage),result.Stage) ||
                    !Enum.IsDefined(typeof(StudyMode),result.Mode)) throw new InvalidDataException("Sesión inválida.");
                return result;
            }
        }

        public StudySession Copy() { return (StudySession)MemberwiseClone(); }

        private long Elapsed(long now)
        {
            long stop=DueUtc==0 ? now : Math.Min(now,DueUtc);
            return Math.Max(0,stop-StageStartUtc);
        }

        public long FocusTotal(long now) { return FocusTicks+(Stage==StudyStage.Focus ? Elapsed(now) : 0); }
        public long BreakTotal(long now) { return BreakTicks+(Stage==StudyStage.Break ? Elapsed(now) : 0); }
        public long ClockTicks(long now) { return DueUtc==0 ? Elapsed(now) : Math.Max(0,DueUtc-now); }
        public int SuggestedMinutes
        {
            get { return Math.Max(1,(int)Math.Round(TimeSpan.FromTicks(LastStretchTicks).TotalMinutes/5,
                MidpointRounding.AwayFromZero)); }
        }

        private void SetStage(StudyStage next,long now,int minutes)
        {
            Stage=next; StageStartUtc=now;
            DueUtc=minutes==0 ? 0 : now+TimeSpan.FromMinutes(minutes).Ticks;
            NoticeShown=false;
        }

        private void CountUntil(long now)
        {
            if(Stage==StudyStage.Focus) { LastStretchTicks=Elapsed(now); FocusTicks+=LastStretchTicks; }
            if(Stage==StudyStage.Break) BreakTicks+=Elapsed(now);
        }

        public bool Expire(long now)
        {
            if(DueUtc==0 || now<DueUtc || (Stage!=StudyStage.Focus && Stage!=StudyStage.Break)) return false;
            long end=DueUtc;
            CountUntil(end);
            SetStage(Stage==StudyStage.Focus ? StudyStage.FocusDue : StudyStage.BreakDue,end,0);
            return true;
        }

        public void Start(StudyMode mode,string goal,long now)
        {
            Mode=mode; Goal=goal.Trim(); Note=""; Outcome="";
            SessionStartUtc=now; FocusTicks=BreakTicks=LastStretchTicks=0;
            SetStage(StudyStage.Focus,now,mode==StudyMode.Pomodoro ? 25 : 0);
        }

        public void Pause(long now)
        {
            CountUntil(now);
            bool flow=Mode==StudyMode.Flowtime;
            SetStage(flow ? StudyStage.ChooseBreak : StudyStage.Break,now,flow ? 0 : 5);
        }

        public void StartBreak(int minutes,long now) { SetStage(StudyStage.Break,now,minutes); }
        public void Extend(long now) { SetStage(StudyStage.Focus,now,5); }
        public void Resume(long now)
        {
            CountUntil(now);
            SetStage(StudyStage.Focus,now,Mode==StudyMode.Pomodoro ? 25 : 0);
        }
        public void Finish(long now) { CountUntil(now); SetStage(StudyStage.Summary,now,0); }
        public void Complete(string outcome,long now) { Outcome=outcome; SetStage(StudyStage.Idle,now,0); }
    }
}
