using System.Linq;
using Sandbox;
using SWB_Base;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public partial class PolygonPlayer : PlayerBase
{
    [Net, Local] public long polygonTime { get; set; } = 0;
    [Net, Local] public bool Freeze { get; set; } = false;
    public Sound PolygonMusic = new();
    public Sound PolygonFinalSound = new();
    
    //cl only
    public List<ScoreData> LocalScores = new();
    private long curDemoID=0;
    public bool tutorialCompleted = false;

    //from cs:z deleted scenes
    private static List<string> SucceedSoundList = new()
    {
        "alamo_success",
        "brecon_success",
        "downed_success",
        "hank_success",
        "hr_success",
        "jungle_success",
        "lost_success",
        "miami_success",
        "motor_success",
        "pipe_success",
        "recoil_success",
        "run_success",
        "sand_success",
        "silo_success",
        "thinice_success",
        "train_success",
        "truth_success",
        "turncrank_success",
    };

    //from cs:z deleted scenes
    private static List<string> FailedSoundList = new()
    {
        "failure1",
        "failure2",
        "failure3",
        "failure4",
    };

    [Serializable]
    public struct ScoreData
    {
        public float score { get; set; }
        public long date { get; set; }
        public string map { get; set; }
        public long demoid { get; set; }
    };

    public bool SupressPickupNotices { get; set; }

    //TimeSince timeSinceDropped;

    public ClothingContainer Clothing = new();

    public PolygonPlayer() : base()
    {
        Inventory = new PolygonInventory(this);

        if (IsClient)
        {
            loadLocalScores();
            tutorialStart();
        }
    }

    public PolygonPlayer(Client client) : this()
    {
        // Load clothing from client data
        Clothing.LoadFromClient(client);
    }

    public override void Respawn()
    {
        base.Respawn();

        SetModel("models/citizen/citizen.vmdl");
        Clothing.DressEntity(this);

        Controller = new PlayerWalkController();
        Animator = new PlayerBaseAnimator();
        CameraMode = new FirstPersonCamera();

        EnableAllCollisions = true;
        EnableDrawing = true;
        EnableHideInFirstPerson = true;
        EnableShadowInFirstPerson = true;

        Health = 100;
    }

    //TODO: build freeze as serverside 
    public override void BuildInput(InputBuilder input)
    {
        if (Freeze)
            input.Clear();

        base.BuildInput(input);
    }

    public override void Simulate(Client cl)
    {
        base.Simulate(cl);

        // Input requested a weapon switch
        if (Input.ActiveChild != null)
        {
            ActiveChild = Input.ActiveChild;
        }

        if (LifeState != LifeState.Alive)
            return;

        TickPlayerUse();

        if (Input.Pressed(InputButton.View))
        {
            if (CameraMode is ThirdPersonCamera)
            {
                CameraMode = new FirstPersonCamera();
            }
            else
            {
                CameraMode = new ThirdPersonCamera();
            }
        }

        /*if (Input.Pressed(InputButton.Drop))
        {
            var dropped = Inventory.DropActive();
            if (dropped != null)
            {
                if (dropped.PhysicsGroup != null)
                {
                    dropped.PhysicsGroup.Velocity = Velocity + (EyeRotation.Forward + EyeRotation.Up) * 300;
                }

                timeSinceDropped = 0;
                SwitchToBestWeapon();
            }
        }*/

        SimulateActiveChild(cl, ActiveChild);

        //
        // If the current weapon is out of ammo and we last fired it over half a second ago
        // lets try to switch to a better wepaon
        //
        if (ActiveChild is WeaponBase weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f)
        {
            SwitchToBestWeapon();
        }
    }

    /*public override void StartTouch(Entity other)
    {
        if (timeSinceDropped < 1) return;

        base.StartTouch(other);
    }*/

    public override void OnKilled()
    {
        base.OnKilled();

       //Inventory.DropActive();
        Inventory.DeleteContents();

        BecomeRagdollOnClient( Velocity, LastDamage.Flags, LastDamage.Position, LastDamage.Force, GetHitboxBone(LastDamage.HitboxIndex ) );

        Controller = null;
        CameraMode = new SpectateRagdollCamera();

        EnableAllCollisions = false;
        EnableDrawing = false;
    }

    public void SwitchToBestWeapon()
    {
        var best = Children.Select(x => x as WeaponBase)
            .Where(x => x.IsValid() && x.IsUsable())
            .OrderByDescending(x => x.BucketWeight)
            .FirstOrDefault();

        if (best == null) return;

        ActiveChild = best;
    }
    public async Task playerWaitUntilStartPolygon(int freezetime)
    {
        Freeze = true;
        await Task.Delay(freezetime*1000);
        if (this != null && Client != null)
        {
            polygonTime = PolygonGame.curTimeMS;
            PolygonGame.polygonOwner.timeStart = polygonTime;
            Freeze = false;
            PolygonGame.breakStartDoors();
            startSound(PolygonGame.startbutton);
        }
        else
            PolygonGame.finishPolygon(null);
    }
    public void stopSounds()
    {
        if (!PolygonFinalSound.Finished)
            PolygonFinalSound.Stop();
        if (!PolygonMusic.Finished)
            PolygonMusic.Stop();
    }

    [ClientRpc]
    public void information(string info)
    {
        PolygonHUD.infoMenu(ref info);
    }

    [ClientRpc]
    public void statistics(bool status, bool cheat, long time, string enemytarget, string friendlytarget)
    {
        //not working, because of engine command
        ConsoleSystem.Run("stop");

        stopSounds();
        PolygonFinalSound = PlaySound((status ? SucceedSoundList : FailedSoundList)[(new Random()).Next((status ? SucceedSoundList : FailedSoundList).Count)]).SetVolume(0.4f);

        if (status)
            recordLocalScore(time);

        PolygonHUD.resultMenu(ref status, ref cheat, ref time, ref enemytarget, ref friendlytarget);
    }

    [ClientRpc]
    public void startInfo(int freezetime)
    {
        //not working, because of engine commands
        ConsoleSystem.Run("stop");
        curDemoID = PolygonGame.curTimeMS;
        ConsoleSystem.Run($"demo {curDemoID}");

        stopSounds();
        PolygonHUD.startInfoPanelBuild();

        PolygonHUD.startInfoActive = PolygonGame.curTime + freezetime;
    }

    [ClientRpc]
    public static void hitTarget(Vector3 pos)
    {
        Sound.FromWorld("bell_impact", pos);
    }
    [ClientRpc]
    public static void startSound(Entity ent)
    {
        if(ent != null && ent.IsValid)
            Sound.FromEntity("alarm_bell_trimmed", ent);
    }

    public void loadLocalScores()
    {
        var filename = $"{Local.PlayerId}.dat";
        var scores = new Dictionary<string,List<ScoreData>>();

        if (!FileSystem.Data.FileExists(filename))
            FileSystem.Data.WriteJson(filename, scores);

        scores = FileSystem.Data.ReadJson<Dictionary<string, List<ScoreData>>>(filename);

        if (scores.TryGetValue(Map.Name, out var data))
            LocalScores = data.ToList();
    }
    public void recordLocalScore(float score)
    {
        var filename = $"{Local.PlayerId}.dat";
        LocalScores.Add(new ScoreData() { score = score / 1000f, date = PolygonGame.curTime, map = Map.Name, demoid = curDemoID });
        LocalScores = LocalScores.OrderBy(x => x.score).ToList();

        if (LocalScores.Count > 10)
            LocalScores.RemoveRange(10, LocalScores.Count - 10);

        var scores = FileSystem.Data.ReadJson<Dictionary<string, List<ScoreData>>>(filename);
        scores.Remove(Map.Name);
        scores.Add(Map.Name, LocalScores);
        FileSystem.Data.WriteJson(filename, scores);
    }

    //TODO
    public void tutorialStart()
    {
        /*tutorialCompleted = !FileSystem.Data.FileExists("tutorial.completed");
        PolygonGame.findMapEntities();
        new TutorialHUD();*/
    }

    //TODO
    public void tutorialComplete()
    {
        /*if (!tutorialCompleted)
        {
            FileSystem.Data.WriteAllText("tutorial.completed","");
            tutorialCompleted = true;
            TutorialHUD.self.DeleteAll();
        }*/
    }
    public bool isNewHighRecord(float newscore) => LocalScores.Any(x => x.score == newscore);
    public float worstRecord() => LocalScores.Count > 0 ? LocalScores.Last().score : PolygonGame.coolDown;

}
