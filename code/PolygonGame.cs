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
using System.Text.Json;

public partial class PolygonGame : Sandbox.Game
{
    public static polygonData polygonOwner = new();
    public static bool polygonIsInUse { get { return polygonOwner.active; } }
    public static long timeLeft = 0;
    public const int freezetime = 4;
    public static int coolDown = 180; //3min cooldown, it should be calculated for per map 
    public static long curTime => DateTimeOffset.Now.ToUnixTimeSeconds();
    public static long curTimeMS => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static Entity startbutton;
    public static Entity stopbutton;
    public static List<Entity> enemyTargets = new();
    public static List<Entity> friendTargets = new();
    public static List<Entity> startDoors = new();
    public static List<Entity> finishDoors = new();
    public static Vector3? startPos;
    private static StandardOutputDelegate sbcall = startButtonCallback;
    private static StandardOutputDelegate stbcall = stopButtonCallback;
    private static StandardOutputDelegate enemytargetcb = enemyTargetBreakCallback;
    private static StandardOutputDelegate friendtargetcb = friendTargetBreakCallback;
    [Net] public List<top10val> top10 { get; set; } = new();
    public record struct polygonData
    {
        public Entity polygonPlayer { get; init; }
        public long timeStart { get; set; }
        public int initialEnemyTargets { get; set; }
        public ushort shootedEnemyTargets { get; set; }
        public int initialFriendlyTargets { get; set; }
        public ushort shootedFriendlyTargets { get; set; }
        public bool cheated { get; set; } //firedbulletcount, weapontype, targethitpos
        public bool active { get; init; }
    }

    public partial class top10val : BaseNetworkable
    {
        [Net] public string name { get; set; }
        [Net] public long date { get; set; }
        [Net] public float score { get; set; }
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
                polygonOwner.shootedEnemyTargets += 1;
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
                polygonOwner.shootedFriendlyTargets += 1;
            else
                polygonOwner.cheated = true;
        }
        return new();
    }

    public static void findMapEntities()
    {
        startDoors.Clear();
        finishDoors.Clear();

        foreach (var ent in All)
        {
            if (ent.Name == "polygon_start")
                startbutton = ent;

            if (ent.Name == "polygon_stop")
                stopbutton = ent;

            if (ent.Name == "polygon_startpos")
                startPos = ent.Position;

            
            if (ent.Name == "polygon_start_door") //optional
                startDoors.Add(ent);

            if (ent.Name == "polygon_stop_door") //optional
                finishDoors.Add(ent);
        }

        if(startbutton == null || stopbutton == null || startPos == null)
        { 
            Log.Error($"This map is not supported! {(startbutton == null ? "Start button" : (stopbutton == null ? "Stop button" : (startPos == null ?  "Start Position" : "Something?")))} is not found..");
            return;
        }
        
        startbutton.AddOutputEvent("OnPressed", sbcall);
        stopbutton.AddOutputEvent("OnPressed", stbcall);
        
    }

    public static void findTargets(Entity owner = null)
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
        {
            ent.AddOutputEvent("OnBreak", enemytargetcb);
            ent.Owner = owner;
        }

        foreach (var ent in friendTargets)
        {
            ent.AddOutputEvent("OnBreak", friendtargetcb);
            ent.Owner = owner;
        }

    }
    public static void breakStartDoors()
    {
        foreach (var door in startDoors)
            door.Delete();//door.FireInput("Break",null); //gibs have collision, should be nocollide with player
    }
    public static void breakFinishDoors()
    {
        foreach (var door in finishDoors)
            door.Delete();
    }
    private static void startPolygon(ref Entity activator)
    {

        var ply = activator.Client.Pawn as PolygonPlayer;

        if (polygonIsInUse)
        {
            ply.information(To.Single(activator), "Polygon is in use.");
            return;
        }

        if(ply.ActiveChild == null)
        {
            ply.information(To.Single(activator), "You can not start polygon without a weapon.");
            return;
        }

        RespawnEntities();
        findTargets(activator);

        polygonOwner = new polygonData() { active = true, polygonPlayer = activator, initialEnemyTargets = enemyTargets.Count, initialFriendlyTargets= friendTargets.Count,cheated = false, timeStart = 0, shootedEnemyTargets = 0, shootedFriendlyTargets = 0};

        timeLeft = curTime + coolDown; 

        ply.startInfo(To.Single(activator), freezetime);

        ply.Tags.Set("nocollide", true);

        if (ply.ActiveChild is WeaponBase wep)
            wep.Primary.Ammo = wep.BulletCocking ? wep.Primary.ClipSize + 1 : wep.Primary.ClipSize;

        _ = ply.playerWaitUntilStartPolygon(freezetime);

        if (startPos is Vector3 pos)
        {
            activator.Velocity = 0;
            activator.Position = pos;
        }
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
                    ply.information(To.Single(activator), "Polygon is in use.");
                    return;
                }
                else
                {
                    ply.polygonTime = 0;

                    var succeed = !(polygonOwner.cheated) && !forcefailed && polygonOwner.shootedFriendlyTargets == 0 && polygonOwner.shootedEnemyTargets == polygonOwner.initialEnemyTargets;
                    var score = curTimeMS - polygonOwner.timeStart;
                    ply.statistics(To.Single(activator), succeed, polygonOwner.cheated, score, $"{polygonOwner.shootedEnemyTargets}/{polygonOwner.initialEnemyTargets}", $"{polygonOwner.shootedFriendlyTargets}/{polygonOwner.initialFriendlyTargets}");

                    if(succeed)
                    {
                        _ = GameServices.UpdateLeaderboard(polygonOwner.polygonPlayer.Client.PlayerId, score / 1000f);
                        recordServerScore(polygonOwner.polygonPlayer.Client, score);
                    }

                    if (forcefailed)
                        if (startPos is Vector3 pos)
                        {
                            ply.Velocity = 0;
                            ply.Position = pos;
                        }
                }
            }
            breakFinishDoors();
            ply.Tags.Set("nocollide", false);
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
        if ((polygonOwner.polygonPlayer.IsValid() && polygonOwner.polygonPlayer.Position.z > 180f) || Global.TimeScale != 1)
            polygonOwner.cheated = true;
    }
    private void checkTimeLeft()
    {
        if(curTime > timeLeft)
            finishPolygon(polygonOwner.polygonPlayer, forcefailed: true);
    }

    public void initializeServerScores()
    {
        var scores = new Dictionary<string, List<top10val>>();

        if (!FileSystem.Data.DirectoryExists("server"))
            FileSystem.Data.CreateDirectory("server");

        if (!FileSystem.Data.FileExists("server/top10.dat"))
            FileSystem.Data.WriteJson("server/top10.dat", scores);
        else
            scores = FileSystem.Data.ReadJson<Dictionary<string, List<top10val>>>("server/top10.dat");

        if (scores.TryGetValue(Map.Name, out var val))
            foreach(var value in val)
                top10.Add(new top10val { score = value.score, date = value.date, name = value.name });
    }
    public static void recordServerScore(Client cl, float score)
    {
        var scores = FileSystem.Data.ReadJson<Dictionary<string, List<top10val>>>("server/top10.dat");

        if (!scores.TryGetValue(Map.Name, out var val))
            scores.Add(Map.Name, new());

        scores[Map.Name].Add(new top10val { score = score / 1000f, date = curTime, name = cl.Name});

        scores[Map.Name] = scores[Map.Name].OrderBy(x => x.score).ToList();

        if (scores[Map.Name].Count > 10)
            scores[Map.Name].RemoveRange(10, scores[Map.Name].Count - 10);

        (Current as PolygonGame).top10.Clear();
        (Current as PolygonGame).top10 = new List<top10val>();

        if (scores.TryGetValue(Map.Name, out var data))
            foreach (var value in data)
                (Current as PolygonGame).top10.Add(new top10val { score = value.score, date = value.date, name = value.name });

        FileSystem.Data.WriteJson("server/top10.dat", scores);
    }

    [ConCmd.Server]
    public static void forceFinish()
    {
        var client = ConsoleSystem.Caller;

        finishPolygon(activator: client.Pawn, forcefailed:true);
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
