using Sandbox;
using System.Threading.Tasks;
using System.Collections.Generic;
using SandboxEditor;
using System;
using System.ComponentModel;
using SWB_Base;
using Sandbox.UI.Construct;
using System.Threading;
using System.Linq;

public partial class PolygonGame : Sandbox.Game
{
    public static polygonData polygonOwner = new();
  
    public static bool polygonIsInUse { get { return polygonOwner.active; } }
    public static long timeLeft = 0;
    public static long curTime => DateTimeOffset.Now.ToUnixTimeSeconds();
    public static long curTimeMS => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static Entity startbutton;
    public static Entity stopbutton;
    public static List<Entity> enemyTargets = new();
    public static List<Entity> friendTargets = new();
    public static List<Entity> breakableDoors = new();
    public static Vector3 startPos;
    private static StandardOutputDelegate sbcall = startButtonCallback;
    private static StandardOutputDelegate stbcall = stopButtonCallback;
    private static StandardOutputDelegate enemytargetcb = enemyTargetBreakCallback;
    private static StandardOutputDelegate friendtargetcb = friendTargetBreakCallback;

    //Not supported
    //[Net] public List<top10val> top10 { get; set; } = new();
    [Net] public List<string> top10names { get; set; } = new();
    [Net] public List<long> top10dates { get; set; } = new();
    [Net] public List<float> top10scores { get; set; } = new();

    //map only
    public static Dictionary<long, Dictionary<string, List<PolygonPlayer.ScoreData>>> ServerScores = new();
    public record struct polygonData
    {
        public Entity polygonPlayer { get; init; }
        public long timeStart { get; set; }
        public int initialEnemyTargets { get; set; }
        public ushort shootedEnemyTargets { get; set; }
        public int initialFriendlyTargets { get; set; }
        public ushort shootedFriendlyTargets { get; set; }
        public bool cheated { get; set; }
        public bool active { get; init; }

        //firedbulletcount, weapontype, targethitzone, score?
    }

    public struct top10val
    {
        public string name { get; init; }
        public long date { get; init; }
        public float score { get; init; }
    }

    public PolygonGame()
    {
        if (IsServer)
        {
            new DeathmatchHud();
            initializeServerScores();
        }
    }

    public override void PostLevelLoaded()
    {
        base.PostLevelLoaded();
        findMapEntities();
        
    }

    public override void ClientJoined(Client client)
    {
        base.ClientJoined(client);

        var player = new PolygonPlayer(client);
        player.Respawn();

        client.Pawn = player;
        loadServerScores(ref client);
    }

    public override void ClientDisconnect(Client client, NetworkDisconnectionReason reason)
    {
        ServerScores.Remove(client.PlayerId);
        base.ClientDisconnect(client, reason);
    }
        private static ValueTask startButtonCallback(Entity activator, float delay)
    {
        startPolygon(ref activator);
        return new();
    }

    private static ValueTask stopButtonCallback(Entity activator, float delay)
    {
        finishPolygon(activator);
        return new();
    }

    private static ValueTask enemyTargetBreakCallback(Entity activator, float delay)
    {
        if(polygonIsInUse)
        {
            if (activator != null && polygonOwner.polygonPlayer == activator)
            {
                polygonOwner.shootedEnemyTargets += 1;
                (activator.Client.Pawn as PolygonPlayer).hitTarget();
            }
            else
                polygonOwner.cheated = true;
        }
        return new();
    }

    private static ValueTask friendTargetBreakCallback(Entity activator, float delay)
    {
        if (polygonIsInUse)
        {
            if (activator != null && polygonOwner.polygonPlayer == activator)
            { 
                polygonOwner.shootedFriendlyTargets += 1;
                (activator.Client.Pawn as PolygonPlayer).hitTarget(true);
            }
            else
                polygonOwner.cheated = true;
        }
        return new();
    }

    public static void findMapEntities()
    {
        breakableDoors.Clear();

        foreach (var ent in All)
        {
            if (ent.Name == "polygon_start")
                startbutton = ent;

            if (ent.Name == "polygon_stop")
                stopbutton = ent;

            if (ent.Name == "polygon_start_door")
                breakableDoors.Add(ent);

            if (ent.Name == "polygon_stop_door")
                breakableDoors.Add(ent);

            if (ent.Name == "polygon_startpos")
                startPos = ent.Position;
        }

        if (startbutton != null)
            startbutton.AddOutputEvent("OnPressed", sbcall);
        else
            Log.Error("This map not supported!");
        
        if(stopbutton != null)
            stopbutton.AddOutputEvent("OnPressed", stbcall);
    }

    public static void findTargets()
    {
        enemyTargets.Clear();
        friendTargets.Clear();

        foreach (var ent in All)
        {
            if (ent.Tags.Has("enemy"))
                enemyTargets.Add(ent);
            else if (ent.Tags.Has("friend"))
                friendTargets.Add(ent);
        }

        foreach (var ent in enemyTargets)
            ent.AddOutputEvent("OnBreak", enemytargetcb);

        foreach (var ent in friendTargets)
            ent.AddOutputEvent("OnBreak", friendtargetcb);
    }
    public static void breakAllDoors()
    {
        foreach(var door in breakableDoors)
            door.FireInput("Break",null);
    }
    private static void startPolygon(ref Entity activator)
    {

        var ply = activator.Client.Pawn as PolygonPlayer;

        if (polygonIsInUse)
        {
            ply.information("Polygon is in use.");
            return;
        }

        if(ply.ActiveChild == null)
        {
            ply.information("You can not start polygon without a weapon.");
            return;
        }
        
        var freezetime = 4;


        RespawnEntities();
        findTargets();

        polygonOwner = new polygonData() { active = true, polygonPlayer = activator, initialEnemyTargets = enemyTargets.Count, initialFriendlyTargets= friendTargets.Count,cheated = false, timeStart = 0, shootedEnemyTargets = 0, shootedFriendlyTargets = 0};

        timeLeft = curTime + 180; //3min cooldown, it should be for per map

        ply.startInfo(freezetime);

        _ = ply.playerWaitUntilStartPolygon(freezetime);

        activator.Position = startPos;
    }

    public static void finishPolygon(Entity activator = null, bool forcefailed = false)
    {
        if ( polygonIsInUse )
        {
            var ply = activator.Client.Pawn as PolygonPlayer;
            
            if ( activator != null )
            {
                if(polygonOwner.polygonPlayer != activator)
                {
                    ply.information("Polygon is in use.");
                    return;
                }
                else
                {
                    ply.InPolygon = 0;

                    var succeed = !(polygonOwner.cheated) && !forcefailed && polygonOwner.shootedFriendlyTargets == 0 && polygonOwner.shootedEnemyTargets == polygonOwner.initialEnemyTargets;
                    var score = curTimeMS - polygonOwner.timeStart;
                    ply.statistics(succeed, polygonOwner.cheated, score, $"{polygonOwner.shootedEnemyTargets}/{polygonOwner.initialEnemyTargets}", $"{polygonOwner.shootedFriendlyTargets}/{polygonOwner.initialFriendlyTargets}");

                    if(succeed)
                    {
                        _ = GameServices.SubmitScore(polygonOwner.polygonPlayer.Client.PlayerId, (curTimeMS - polygonOwner.timeStart) / 1000f);
                        recordServerScore(polygonOwner.polygonPlayer.Client, score);
                    }
                    //PlayerGameRank.LeaderboardFacet
                }
            }
            polygonOwner = new();
        }
    }
    private static void RespawnEntities()
    {
        Map.Reset(DefaultCleanupFilter);
        findMapEntities();
    }
    private void checkCheat()
    {
        //temporarily..
        if (polygonOwner.polygonPlayer.Position.z > 180f)
            polygonOwner.cheated = true;
    }
    private void checkTimeLeft()
    {
        if(curTime > timeLeft)
            finishPolygon(polygonOwner.polygonPlayer, forcefailed: true);
    }
    public void initializeServerScores()
    {
        if (!FileSystem.Data.DirectoryExists("server"))
            FileSystem.Data.CreateDirectory("server");
    }
    public void loadServerScores(ref Client cl)
    {
        var filename = $"server/{cl.PlayerId}.dat";
        var scores = new Dictionary<string, List<PolygonPlayer.ScoreData>>();

        if (!FileSystem.Data.FileExists(filename))
            FileSystem.Data.WriteJson(filename, scores);

        scores = FileSystem.Data.ReadJson<Dictionary<string, List<PolygonPlayer.ScoreData>>>(filename);

        var data = new List<PolygonPlayer.ScoreData>();
        
        if(scores.TryGetValue(Map.Name,out var val))
        {
            foreach(var value in val)
            {
                if (value.map == Map.Name)
                    data.Add(value);
            }
        }

        scores.Clear();
        scores.Add(Map.Name, data);

        ServerScores.Add(cl.PlayerId, scores);
        computeTop10();
    }

    public static void recordServerScore(Client cl, float score)
    {
        var filename = $"server/{Local.PlayerId}.dat";

        var data = ServerScores[cl.PlayerId][Map.Name];
        data.Add(new PolygonPlayer.ScoreData() { score = score / 1000f, date = curTime, map = Map.Name });
         
        data = data.OrderBy(x => x.score).ToList();

        if (data.Count > 10)
            data.RemoveRange(10, data.Count - 10);

        ServerScores[cl.PlayerId][Map.Name] = data;

        var scores = FileSystem.Data.ReadJson<Dictionary<string, List<PolygonPlayer.ScoreData>>>(filename);
        scores.Remove(Map.Name);
        scores.Add(Map.Name, data);
        FileSystem.Data.WriteJson(filename, scores);
        (Game.Current as PolygonGame).computeTop10();
    }

    private static Client clientFromSteamID64(long sid64)
    {
        return Client.All.FirstOrDefault(cl => cl.PlayerId == sid64, null);
    }
    public void computeTop10()
    {
        var allscores = new List<top10val>();
        foreach(var ply in ServerScores )
        {
            foreach (var maps in ply.Value)
            {
                if (maps.Key != Map.Name)
                    continue;

                foreach(var score in maps.Value)
                {
                    allscores.Add(new top10val() { name = clientFromSteamID64(ply.Key).Name,date = score.date,score = score.score });
                }
            }
        }
        allscores = allscores.OrderBy(x => x.score).ToList();
        if (allscores.Count > 10)
            allscores.RemoveRange(10, allscores.Count - 10);

        top10names.Clear();
        top10dates.Clear();
        top10scores.Clear();
        foreach(var values in allscores)
        {
            top10names.Add( values.name );
            top10dates.Add( values.date );
            top10scores.Add( values.score );
        }
        
    }

    [Event.Tick.Server]
    private void Tick()
    {
        if (polygonIsInUse)
        {
            checkCheat();
            checkTimeLeft();

        }
    }
}
