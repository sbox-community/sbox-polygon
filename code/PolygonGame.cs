using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using SWB_Base;
public partial class PolygonGame : Sandbox.Game
{
    public static polygonData polygonOwner = new();
    public static bool polygonIsInUse { get { return polygonOwner.active; } }
    public static long timeLeft = 0;
    public const int freezetime = 4;
    public static int coolDown = 180; //3min cooldown, it should be calculated for per map 
    public static long curTime => DateTimeOffset.Now.ToUnixTimeSeconds();
    public static long curTimeMS => DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public static bool notSupported = false;
    public static Entity startbutton;
    public static Entity stopbutton;
    public static List<Entity> enemyTargets = new();
    public static List<Entity> friendTargets = new();
    public static List<Entity> startDoors = new();
    public static List<Entity> finishDoors = new();
    public static float maxHeight = 0f;
    public static float minHeight = 0f;
    public static Vector3? startPos;
    private static StandardOutputDelegate sbcall = startButtonCallback;
    private static StandardOutputDelegate stbcall = stopButtonCallback;
    private static StandardOutputDelegate enemytargetcb = enemyTargetBreakCallback;
    private static StandardOutputDelegate friendtargetcb = friendTargetBreakCallback;
    public List<top10val> top10 = new();
    [Net] public string startMusic { get; set; } //clients can't get the entities value of the map?

    public struct polygonData
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

    [Serializable]
    public record struct top10val
    {
        public string name { get; set; }
        public long date { get; set; }
        public float score { get; set; }
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

        sendTop10Data(client);
    }

    public override void ClientDisconnect(Client client, NetworkDisconnectionReason reason)
    {
        finishPolygon(client.Pawn, true);

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
        if (polygonIsInUse)
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

            if (ent.ClassName == "ent_polygon_music")
                (Current as PolygonGame).startMusic = (ent as PolygonMusic).Music;

            if (ent.ClassName == "ent_polygon_minheight")
                minHeight = ent.Position.z;

            if (ent.ClassName == "ent_polygon_maxheight")
                maxHeight = ent.Position.z;

            if (ent.Name == "polygon_start_door") //optional
                startDoors.Add(ent);

            if (ent.Name == "polygon_stop_door") //optional
                finishDoors.Add(ent);
        }

        if (startbutton == null || stopbutton == null || startPos == null || string.IsNullOrEmpty((Current as PolygonGame).startMusic) || minHeight == 0f || maxHeight == 0f)
        {
            notSupported = true;
            Log.Error($"This map is not supported! {(startbutton == null ? "Start button" : (stopbutton == null ? "Stop button" : (startPos == null ? "Start Position" : (string.IsNullOrEmpty((Current as PolygonGame).startMusic) ? "Start Music Entity" : (minHeight == 0f ? "Min. Height Entity" : (maxHeight == 0f ? "Max. Height Entity" : "Something?"))))))} is not found..");
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

        if (notSupported)
        {
            ply.information(To.Single(activator), "Map is not supported.");
            return;
        }

        if (polygonIsInUse)
        {
            ply.information(To.Single(activator), "Polygon is in use.");
            return;
        }

        if (ply.ActiveChild == null)
        {
            ply.information(To.Single(activator), "You can not start polygon without a weapon.");
            return;
        }

        RespawnEntities();
        findTargets(activator);

        polygonOwner = new polygonData() { active = true, polygonPlayer = activator, initialEnemyTargets = enemyTargets.Count, initialFriendlyTargets = friendTargets.Count, cheated = false, timeStart = 0, shootedEnemyTargets = 0, shootedFriendlyTargets = 0 };

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
        var ply = activator.Client.Pawn as PolygonPlayer;

        if (notSupported)
        {
            ply.information(To.Single(activator), "Map is not supported.");
            return;
        }

        if (polygonIsInUse)
        {
            if (activator != null)
            {
                if (polygonOwner.polygonPlayer != activator)
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

                    if (succeed)
                    {
                        _ = SubmitGlobalScore(polygonOwner.polygonPlayer.Client, Convert.ToInt32(score));
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
    public static string getMapIdent()
    {
        string mapIdent = Map.Name.Substring(Map.Name.IndexOf(".") + 1);
        return $"{char.ToUpper(mapIdent[0])}{mapIdent.Substring(1)}";
    }
    private static async Task SubmitGlobalScore(Client cl, int score)
    {
        //if there is any map with the same name, there will be conflict
        if (await Leaderboard.FindOrCreate(getMapIdent(), true) is { } mapLb) //is there bug about comparing the last score as ascending? that is happen after submitting
        {
            if (await mapLb.GetScore(cl.PlayerId) is { } clScores)
                if (clScores.Score < score)
                    return;

            Log.Info($"[Polygon] Submitted {cl.Name} ({cl.PlayerId})'s '{score / 1000}' score for '{getMapIdent()}': {await mapLb.Submit(cl, score, true)}"); //false, TODO: Celebrate that..
        }
    }
    private static void RespawnEntities()
    {
        Map.Reset(DefaultCleanupFilter);
        findMapEntities();
        temporaryFixForOldFactory();
    }
    private void checkCheat()
    {
        if ((polygonOwner.polygonPlayer.IsValid() && (polygonOwner.polygonPlayer.Position.z > maxHeight || polygonOwner.polygonPlayer.Position.z < minHeight)) || Global.TimeScale != 1)
            polygonOwner.cheated = true;
    }
    private void checkTimeLeft()
    {
        if (curTime > timeLeft)
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
            foreach (var value in val)
                top10.Add(new top10val { score = value.score, date = value.date, name = value.name });

        sendTop10Data();
    }
    public static void recordServerScore(Client cl, float score)
    {
        var scores = FileSystem.Data.ReadJson<Dictionary<string, List<top10val>>>("server/top10.dat");

        if (!scores.TryGetValue(Map.Name, out var val))
            scores.Add(Map.Name, new());

        scores[Map.Name].Add(new top10val { score = score / 1000f, date = curTime, name = cl.Name });

        scores[Map.Name] = scores[Map.Name].OrderBy(x => x.score).ToList();

        if (scores[Map.Name].Count > 10)
            scores[Map.Name].RemoveRange(10, scores[Map.Name].Count - 10);

        (Current as PolygonGame).top10.Clear();
        (Current as PolygonGame).top10 = new List<top10val>();

        if (scores.TryGetValue(Map.Name, out var data))
            foreach (var value in data)
                (Current as PolygonGame).top10.Add(new top10val { score = value.score, date = value.date, name = value.name });

        FileSystem.Data.WriteJson("server/top10.dat", scores);

        (Current as PolygonGame).sendTop10Data();
    }

    [ConCmd.Server]
    public static void forceFinish()
    {
        var client = ConsoleSystem.Caller;

        finishPolygon(activator: client.Pawn, forcefailed: true);
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

    private void sendTop10Data(Client cl = null)
    {
        var count = top10.Count;

        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(top10.Count);
                for (var i = 0; i < count; i++)
                {
                    writer.Write(top10[i].name);
                    writer.Write(top10[i].date);
                    writer.Write(top10[i].score.ToString());
                }
                receiveTop10Data(cl is null ? To.Everyone : To.Single(cl), stream.ToArray());
            }
        }
    }

    [ClientRpc]
    public static void receiveTop10Data(byte[] data)
    {
        var game = (Current as PolygonGame);

        if (game == null)
            return;

        game.top10.Clear();

        using (var stream = new MemoryStream(data))
        using (var reader = new BinaryReader(stream))
        {
            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
                game.top10.Add(new top10val { name = reader.ReadString(), date = reader.ReadInt64(), score = float.Parse(reader.ReadString()) });
        }
    }

    public static void temporaryFixForOldFactory()
    {
        foreach (var ent in Prop.All)
            if (ent is Prop prop && prop.GetModelName() == "models/rust_props/fuel_tank/fuel_tank_a_600.vmdl")
            {
                prop.PhysicsClear();
                break;
            }
    }
}
