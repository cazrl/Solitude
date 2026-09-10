using Solitude;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class VariantChecks
{
    public static void Run(Action<string,Action> test)
    {
        void Assert(bool value,string message="Assertion failed"){if(!value)throw new Exception(message);}
        test("Every era exposes only its bundled card games",()=>
        {
            int count=0;
            foreach(var era in Enum.GetValues<Era>())foreach(var kind in Enum.GetValues<GameKind>())if(GameCatalog.Available(era,kind))count++;
            Assert(count==17);Assert(!GameCatalog.Available(Era.Windows98,GameKind.Spider));Assert(!GameCatalog.Available(Era.Windows2000,GameKind.Spider));
            Assert(GameCatalog.MaxDeal(Era.Windows95)==32000 && GameCatalog.MaxDeal(Era.WindowsXP)==1000000);
        });
        test("Every Klondike preset respects recycle scoring and Vegas pass limits",()=>
        {
            foreach(var era in Enum.GetValues<Era>())foreach(int draw in new[]{1,3})
            {
                var rules=GameCatalog.Defaults(era,GameKind.Klondike).Rules;rules.DrawCount=draw;rules.Timed=false;
                var g=new Game(rules,71);g.State.Score=500;
                for(int pass=1;pass<=4;pass++)
                {
                    while(g.State.Stock.Count>0)Assert(g.Draw());
                    int score=g.State.Score;Assert(g.Draw());
                    Assert(g.State.Score==score-(draw==1?100:pass>=3?20:0),$"{era} draw {draw}, recycle {pass}");
                    Game.Validate(g.State);
                }
                rules=rules.Clone();rules.Scoring=Scoring.Vegas;g=new Game(rules,71);
                for(int pass=1;pass<=(draw==1?1:3);pass++){while(g.State.Stock.Count>0)Assert(g.Draw());if(pass<(draw==1?1:3))Assert(g.Draw());}
                string before=JsonSerializer.Serialize(g.State);Assert(!g.Draw() && !g.CanRecycle && before==JsonSerializer.Serialize(g.State),era+" exceeded Vegas passes");
            }
        });
        test("Microsoft FreeCell deal 1 matches all eight columns",()=>
        {
            var g=new Game(new(){Kind=GameKind.FreeCell},1);
            string[] columns=["JD KD 2S 4C 3S 6D 6S","2D KC KS 5C TD 8S 9C","9H 9S 9D TS 4S 8D 2H","JC 5S QD QH TH QS 6H","5D AD JS 4H 8H 6C","7H QC AS AC 2C 3D","7C KH AH 4D JH 8C","5H 3H 3C 7S 7D TC"];
            string Code(Card c)=>(c.Rank switch{1=>"A",10=>"T",11=>"J",12=>"Q",13=>"K",_=>c.Rank.ToString()})+"CDHS"[(int)c.Suit];
            for(int i=0;i<8;i++)Assert(string.Join(" ",g.State.Tableau[i].Select(Code))==columns[i],"Deal 1 column "+(i+1));
            Game.Validate(g.State);
        });
        test("FreeCell numbered deals are bounded and repeatable",()=>
        {
            foreach(int seed in new[]{1,11982,32000,1000000}){var g=new Game(new(){Kind=GameKind.FreeCell},seed);Game.Validate(g.State);Assert(g.State.Tableau.Take(4).All(p=>p.Count==7)&&g.State.Tableau.Skip(4).All(p=>p.Count==6));}
            bool rejected=false;try{_ = new Game(GameCatalog.Defaults(Era.Windows95,GameKind.FreeCell).Rules,32001);}catch(ArgumentOutOfRangeException){rejected=true;}Assert(rejected);
        });
        test("Spider deals 104 unique cards, 54 on the table and five packets",()=>
        {
            foreach(int suits in new[]{1,2,4})for(int seed=1;seed<=100;seed++)
            {
                var g=new Game(new(){Kind=GameKind.Spider,SpiderSuits=suits},seed);Game.Validate(g.State);
                Assert(g.State.Stock.Count==50&&g.State.Tableau.Sum(p=>p.Count)==54&&g.State.Score==500);
                Assert(g.State.Tableau.All(p=>p.Count(c=>c.FaceUp)==1));Assert(g.State.Tableau.SelectMany(p=>p).Concat(g.State.Stock).Select(c=>c.Suit).Distinct().Count()==suits);
            }
        });
        test("Spider stock deals ten, undo restores cards and applies its penalty",()=>
        {
            var g=new Game(new(){Kind=GameKind.Spider},14);var before=g.State.Clone();
            Assert(g.Draw()&&g.State.Stock.Count==40&&g.State.Score==499&&g.State.Moves==1);
            Assert(g.Undo()&&g.State.Stock.Count==50&&g.State.Score==498&&g.State.Moves==2);
            Assert(g.State.Stock.SequenceEqual(before.Stock));Game.Validate(g.State);
        });
        Game SpiderFixture(int suits,List<Card>[] piles,List<Card>[]? completed=null)
        {
            var original=new Game(new(){Kind=GameKind.Spider,SpiderSuits=suits},1);
            var all=original.State.Stock.Concat(original.State.Tableau.SelectMany(p=>p)).Select(c=>c with{FaceUp=true}).ToList();
            var s=original.State.Clone();s.Stock.Clear();s.Tableau=Enumerable.Range(0,10).Select(_=>new List<Card>()).ToList();
            for(int i=0;i<piles.Length;i++)s.Tableau[i]=piles[i];
            if(completed!=null)for(int i=0;i<completed.Length;i++)s.Foundations[i]=completed[i];
            var used=s.Tableau.Concat(s.Foundations).SelectMany(p=>p).Select(c=>c.Key).ToHashSet();
            s.Tableau[9].AddRange(all.Where(c=>!used.Contains(c.Key)));
            return Game.Restore(original.Rules,s);
        }
        Card C(Suit suit,int rank,int copy=0)=>new((int)suit*13+rank-1,true,copy);
        test("Spider allows mixed-suit placement but only same-suit sequence moves",()=>
        {
            var g=SpiderFixture(4,[[C(Suit.Spades,9),C(Suit.Hearts,8)],[C(Suit.Spades,9,1)],[C(Suit.Hearts,10)]]);
            Assert(!g.CanPick(new(PileKind.Tableau,0,0)) && g.CanPick(new(PileKind.Tableau,0,1)));
            Assert(g.CanMove(new(PileKind.Tableau,0,1),new(PileKind.Tableau,1)));
            Assert(!g.CanMove(new(PileKind.Tableau,0,0),new(PileKind.Tableau,2)));
            Assert(g.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,3)));Game.Validate(g.State);
        });
        test("Spider completes King-to-Ace runs, exposes hidden cards and undoes both",()=>
        {
            var run=Enumerable.Range(2,12).Reverse().Select(r=>C(Suit.Spades,r)).ToList();
            run.Insert(0,C(Suit.Spades,7,1) with{FaceUp=false});
            var g=SpiderFixture(1,[run,[C(Suit.Spades,1)]]);
            Assert(g.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,0)));
            Assert(g.State.Foundations[0].Count==13 && g.State.Score==599 && g.State.Tableau[0].Count==1 && g.State.Tableau[0][0].FaceUp);
            Game.Validate(g.State);Assert(g.Undo());
            Assert(g.State.Foundations.All(p=>p.Count==0) && !g.State.Tableau[0][0].FaceUp && g.State.Tableau[0].Count==13);
            Game.Validate(g.State);
        });
        test("Spider wins only after all eight complete runs",()=>
        {
            var completed=Enumerable.Range(0,7).Select(copy=>Enumerable.Range(1,13).Reverse().Select(r=>C(Suit.Spades,r,copy)).ToList()).ToArray();
            var g=SpiderFixture(1,[Enumerable.Range(2,12).Reverse().Select(r=>C(Suit.Spades,r,7)).ToList(),[C(Suit.Spades,1,7)]],completed);
            Assert(!g.State.Won);Assert(g.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,0)) && g.State.Won);
            Assert(g.State.Foundations.Sum(p=>p.Count)==104 && !g.Draw());Game.Validate(g.State);
        });
        test("Spider cannot deal into an empty column and preserves the stock on rejection",()=>
        {
            var g=new Game(new(){Kind=GameKind.Spider},21);var s=g.State.Clone();
            s.Tableau[1].AddRange(s.Tableau[0].Select(c=>c with{FaceUp=true}));s.Tableau[0].Clear();
            g=Game.Restore(g.Rules,s);Assert(!g.CanDealSpider && !g.Draw() && g.State.Stock.Count==50 && g.State.Moves==0);
        });
        Game FreeFixture(List<Card>[] piles,List<Card>[]? cells=null,Rules? rules=null)
        {
            var s=new GameState{Kind=GameKind.FreeCell,Seed=1,Tableau=Enumerable.Range(0,8).Select(_=>new List<Card>()).ToList()};
            for(int i=0;i<piles.Length;i++)s.Tableau[i]=piles[i];
            if(cells!=null)for(int i=0;i<cells.Length;i++)s.FreeCells[i]=cells[i];
            var used=s.Tableau.Concat(s.FreeCells).SelectMany(p=>p).Select(c=>c.Id).ToHashSet();
            s.Tableau[7].AddRange(Enumerable.Range(0,52).Where(i=>!used.Contains(i)).Select(i=>new Card(i)));
            return Game.Restore(rules??new(){Kind=GameKind.FreeCell},s);
        }
        test("FreeCell cells hold one card and foundations cannot return cards",()=>
        {
            var g=FreeFixture([[C(Suit.Clubs,6)],[C(Suit.Hearts,7)],[C(Suit.Diamonds,1)]]);
            Assert(g.Move(new(PileKind.Tableau,0),new(PileKind.FreeCell,0)));
            Assert(!g.CanMove(new(PileKind.Tableau,1),new(PileKind.FreeCell,0)));
            Assert(g.State.Foundations.Any(p=>p.Count>0));
            int f=g.State.Foundations.FindIndex(p=>p.Count>0);
            Assert(!g.CanPick(new(PileKind.Foundation,f)) && !g.CanMove(new(PileKind.Foundation,f),new(PileKind.Tableau,0)));
            Game.Validate(g.State);
        });
        test("FreeCell safe auto-home waits for opposite colours and undoes as one move",()=>
        {
            var g=FreeFixture([[C(Suit.Diamonds,1)],[C(Suit.Diamonds,2)],[C(Suit.Diamonds,3)],[C(Suit.Clubs,9)]]);
            Assert(g.Move(new(PileKind.Tableau,3),new(PileKind.FreeCell,0)));
            Assert(g.State.Foundations.Sum(p=>p.Count)==2 && g.State.Tableau[2].Count==1);
            Assert(g.History.Count==1 && g.Undo());
            Assert(g.State.Tableau[0][0].Rank==1 && g.State.Tableau[1][0].Rank==2 && g.State.FreeCells[0].Count==0);
        });
        test("Classic FreeCell and Vista have different sequence capacities and undo limits",()=>
        {
            var oldRules=GameCatalog.Defaults(Era.WindowsXP,GameKind.FreeCell).Rules;
            var g=FreeFixture([[C(Suit.Clubs,8),C(Suit.Hearts,7),C(Suit.Spades,6)],[C(Suit.Hearts,9)]],[[C(Suit.Clubs,2)],[C(Suit.Clubs,3)],[C(Suit.Clubs,4)]],oldRules);
            Assert(g.FreeCellCapacity(2)==2 && !g.CanMove(new(PileKind.Tableau,0,0),new(PileKind.Tableau,2)));
            var vista=Game.Restore(GameCatalog.Defaults(Era.WindowsVista,GameKind.FreeCell).Rules,g.State);
            Assert(vista.FreeCellCapacity(2)>2 && vista.CanMove(new(PileKind.Tableau,0,0),new(PileKind.Tableau,2)));
            Assert(oldRules.UndoLimit==1 && vista.Rules.UndoLimit==0);
        });
        test("FreeCell chosen-card shortcuts and explicit collection move one card at a time",()=>
        {
            foreach(var era in Enum.GetValues<Era>().Where(e=>GameCatalog.Available(e,GameKind.FreeCell)))
            {
                var rules=GameCatalog.Defaults(era,GameKind.FreeCell).Rules;
                var g=FreeFixture([[C(Suit.Clubs,1)],[C(Suit.Diamonds,1)],[C(Suit.Spades,6)]],rules:rules);
                Assert(g.ToFoundation(new(PileKind.Tableau,0)) && g.State.Foundations.Sum(p=>p.Count)==1 && g.State.Tableau[1].Count==1,era.ToString());
                Assert(g.ToFreeCell(new(PileKind.Tableau,2)) && g.State.Foundations.Sum(p=>p.Count)==1,era.ToString());
                Assert(g.AutoStep() && g.State.Foundations.Sum(p=>p.Count)==2 && !g.AutoStep(),era.ToString());
                g=FreeFixture([[C(Suit.Clubs,1)],[C(Suit.Diamonds,1)],[C(Suit.Spades,6)]],rules:rules);
                Assert(g.Move(new(PileKind.Tableau,2),new(PileKind.FreeCell,0)) && g.State.Foundations.Sum(p=>p.Count)==2 && g.History.Count==1,"Normal moves must retain safe auto-home: "+era);
                Assert(g.Undo() && g.State.Foundations.All(p=>p.Count==0));
            }
        });
        test("Classic Spider undo stops at deals and completed runs, Vista can cross them",()=>
        {
            foreach(var era in new[]{Era.WindowsMe,Era.WindowsXP,Era.WindowsVista})
            {
                var rules=GameCatalog.Defaults(era,GameKind.Spider).Rules;bool vista=era==Era.WindowsVista;
                var g=new Game(rules,14);Assert(g.Draw() && g.CanUndo==vista,era+" deal barrier");
                var unrestricted=new Game(GameCatalog.Defaults(Era.WindowsVista,GameKind.Spider).Rules,14);unrestricted.Draw();
                Assert(Game.Restore(rules,unrestricted.State,unrestricted.History).CanUndo==vista,era+" restored deal barrier");
                var fixture=SpiderFixture(1,[Enumerable.Range(2,12).Reverse().Select(r=>C(Suit.Spades,r)).ToList(),[C(Suit.Spades,1)]]);
                var completed=Game.Restore(GameCatalog.Defaults(Era.WindowsVista,GameKind.Spider).Rules,fixture.State);completed.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,0));
                Assert(Game.Restore(rules,completed.State,completed.History).CanUndo==vista,era+" restored completion barrier");
                g=Game.Restore(rules,fixture.State);Assert(g.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,0)) && g.State.Score==599 && g.CanUndo==vista,era+" run barrier");
                if(vista)
                {
                    Assert(g.Undo() && g.State.Score==498 && g.State.Moves==2);
                    Assert(g.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,0)) && g.State.Score==597,"Recompleting a run must not create points");
                    Assert(g.Undo() && g.State.Score==496 && g.State.Moves==4);
                }
                Assert(Game.Restore(GameCatalog.Defaults(Era.WindowsXP,GameKind.Spider).Rules,fixture.State).History.Count==0);
            }
        });
        test("Repeated Spider undo retains all move costs and elapsed time",()=>
        {
            var g=SpiderFixture(4,[[C(Suit.Spades,8)],[C(Suit.Hearts,9)],[C(Suit.Clubs,10)]]);
            Assert(g.Move(new(PileKind.Tableau,0),new(PileKind.Tableau,1)));
            Assert(g.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,3)));
            g.State.Elapsed=99;Assert(g.State.Score==498 && g.Undo() && g.State.Score==497 && g.Undo() && g.State.Score==496 && g.State.Elapsed==99 && g.State.Moves==4 && !g.CanUndo);
        });
        test("Spider difficulty totals are separate, snapshots are independent and old totals remain",()=>
        {
            var stats=new Statistics{Played=10,Won=4};
            var easy=new GameState{Kind=GameKind.Spider,SpiderSuits=1,Score=1100,Elapsed=120};
            var hard=new GameState{Kind=GameKind.Spider,SpiderSuits=4,Score=999,Elapsed=360};
            stats.Record(easy,true,true);stats.Record(hard,false,true);stats.Record(easy,true,true);stats.Validate();
            Assert(stats.Played==13 && stats.Won==6 && stats.Difficulties[1].Played==2 && stats.Difficulties[1].Won==2 && stats.Difficulties[1].BestWinStreak==2);
            Assert(stats.Difficulties[4].Played==1 && stats.Difficulties[4].Won==0 && !stats.Difficulties.ContainsKey(2));
            var clone=stats.Clone();stats.Record(easy,false,true);Assert(clone.Difficulties[1].Played==2 && stats.Difficulties[1].Streak==-1);
            var json=JsonSerializer.Serialize(stats);var roundtrip=JsonSerializer.Deserialize<Statistics>(json)!;roundtrip.Validate();Assert(roundtrip.Difficulties[4].BestLossStreak==1);
        });
        test("Format 2 migration preserves current and stored deals and trims obsolete classic undo",()=>
        {
            var xp=GameCatalog.Defaults(Era.WindowsXP,GameKind.Spider);xp.QuickControls=true;xp.Rules.SpiderFullUndo=true;
            var g=new Game(xp.Rules,71);g.Draw();g.State.Score=500;
            var vista=GameCatalog.Defaults(Era.WindowsVista,GameKind.Spider);var other=new Game(vista.Rules,73);other.Draw();
            var file=new SaveFile{Format=2,Preferences=xp,Game=g.State,History=g.History,Statistics=new(){Played=5,Won=2},Sessions=new(){[GameCatalog.Key(Era.WindowsVista,GameKind.Spider)]=new(){Preferences=vista,Game=other.State,History=other.History,Statistics=new(){Played=7,Won=3}}}};
            string dir=Path.GetFullPath("artifacts/migrate-format2-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            string old=JsonSerializer.Serialize(file);File.WriteAllText(Path.Combine(dir,"solitude.json"),old);var store=new Store(dir);var migrated=store.Load();
            Assert(store.Warning==null && migrated.Format==6 && migrated.Game!.Score==499 && migrated.Game.Stock.SequenceEqual(g.State.Stock));
            Assert(migrated.Game!.Tableau.SelectMany(p=>p).SequenceEqual(g.State.Tableau.SelectMany(p=>p)) && migrated.History.Count==0 && !migrated.Preferences.Rules.SpiderFullUndo && !migrated.Preferences.QuickControls);
            Assert(migrated.Statistics.Played==5 && migrated.Statistics.Won==2);
            var session=migrated.Sessions.Single().Value;Assert(session.History.Count==1 && session.Preferences.Rules.SpiderFullUndo && session.Game!.Seed==73 && session.Statistics.Won==3);
            Assert(store.Save(migrated) && File.ReadAllText(store.FilePath+".bak")==old && store.Load().Format==6);
        });
        test("Variant save rejects duplicate copies, malformed runs and mismatched rules",()=>
        {
            int rejected=0;
            var g=new Game(new(){Kind=GameKind.Spider},1);var s=g.State.Clone();s.Stock[0]=s.Stock[1];
            try{Game.Validate(s);}catch(InvalidDataException){rejected++;}
            try{Game.Restore(new(){Kind=GameKind.FreeCell},g.State);}catch(InvalidDataException){rejected++;}
            s=g.State.Clone();s.FreeCells[0].Add(s.Stock[^1]);s.Stock.RemoveAt(s.Stock.Count-1);
            try{Game.Validate(s);}catch(InvalidDataException){rejected++;}
            Assert(rejected==3);
        });
        test("Version 0.2 saves migrate without losing the deal or statistics",()=>
        {
            var g=new Game(new(),321);g.Draw();g.Draw();
            var saved=new SaveFile{Format=1,Preferences=new(){Era=Era.WindowsXP,Rules=g.Rules},Game=g.State,History=g.History,Statistics=new(){Played=10,Won=4}};
            var json=JsonSerializer.SerializeToNode(saved)!.AsObject();json.Remove("Sessions");
            var preferences=json["Preferences"]!.AsObject();preferences.Remove("VistaBackground");
            var rules=preferences["Rules"]!.AsObject();
            foreach(string key in new[]{"Kind","SpiderSuits","UndoLimit","FreeCellSupermoves","DealMaximum","KeepVegasScore"})rules.Remove(key);
            void OldState(JsonObject state)
            {
                foreach(string key in new[]{"Kind","SpiderSuits","FreeCells","UndoCount"})state.Remove(key);
                foreach(string name in new[]{"Stock","Waste"})foreach(var card in state[name]!.AsArray())card!.AsObject().Remove("Copy");
                foreach(string name in new[]{"Tableau","Foundations"})foreach(var pile in state[name]!.AsArray())foreach(var card in pile!.AsArray())card!.AsObject().Remove("Copy");
            }
            OldState(json["Game"]!.AsObject());foreach(var state in json["History"]!.AsArray())OldState(state!.AsObject());
            string dir=Path.GetFullPath("artifacts/migration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            string old=json.ToJsonString();File.WriteAllText(Path.Combine(dir,"solitude.json"),old);
            var store=new Store(dir);var loaded=store.Load();
            Assert(store.Warning==null && loaded.Game!=null && JsonSerializer.Serialize(loaded.Game)==JsonSerializer.Serialize(g.State));
            Assert(loaded.Statistics.Played==10 && loaded.Statistics.Won==4 && loaded.Preferences.Rules.UndoLimit==1 && loaded.History.Count==1);
            loaded.Format=2;Assert(store.Save(loaded));Assert(File.ReadAllText(store.FilePath+".bak")==old);
        });
        test("Background saves keep the latest snapshot and flush before exit",()=>
        {
            string dir=Path.GetFullPath("artifacts/save-queue-"+Guid.NewGuid().ToString("N"));var store=new Store(dir);
            using var writer=new SaveCoordinator(store);
            Assert(writer.Flush(new(){Game=new Game(new(),0).State}));
            for(int i=1;i<=80;i++)writer.Queue(new(){Game=new Game(new(),i).State});
            Assert(writer.Flush(new(){Game=new Game(new(),999).State}));
            Assert(!writer.Busy && store.Load().Game!.Seed==999 && File.Exists(store.FilePath+".bak"));
        });
        test("Background save failure is reported and can recover",()=>
        {
            string root=Path.GetFullPath("artifacts/save-failure-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            string blocked=Path.Combine(root,"blocked");File.WriteAllText(blocked,"test obstruction");
            using var writer=new SaveCoordinator(new Store(blocked));
            Assert(!writer.Flush(new()) && writer.Error!=null);
            File.Move(blocked,blocked+".preserved");Assert(writer.Flush(new()) && writer.Error==null);
            Assert(File.ReadAllText(blocked+".preserved")=="test obstruction");
        });
        test("All variants preserve cards during 60,000 rule-driven actions",()=>
        {
            foreach(var kind in new[]{GameKind.FreeCell,GameKind.Spider})for(int seed=1;seed<=100;seed++)
            {
                var g=new Game(new(){Kind=kind,SpiderSuits=seed%3==0?4:seed%2==0?2:1},seed);
                for(int step=0;step<300&&!g.State.Won;step++)
                {
                    if(step%17==16&&g.CanUndo)g.Undo();
                    else if(g.Hint() is {} hint){if(hint.From.Kind==PileKind.Stock)g.Draw();else g.Move(hint.From,hint.To);}
                    else break;
                    Game.Validate(g.State);
                }
            }
        });
    }
}
