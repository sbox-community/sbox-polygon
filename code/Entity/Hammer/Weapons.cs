using SandboxEditor;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using SWB_Base;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Sandbox
{
	[Library( "ent_polygon_weapon" )]
	[HammerEntity]//SupportsSolid
    [EditorModel("weapons/swb/smgs/ump45/w_ump45.vmdl")]
    //[Model( Archetypes = ModelArchetype.animated_model )]
	[VisGroup( VisGroup.Dynamic )]//RenderFields,
    [Title( "Polygon Weapon" ), Category( "Gameplay" ), Icon( "door_front" )]
	public partial class PolygonWeapon : ModelEntity, IUse
    {
        public enum Weapons
        {
            Deagle,
            Fal,
            Spas12,
            Ump45
        }

        List<string> WeaponWordModels = new List<string>()
        {
            "weapons/swb/pistols/deagle/w_deagle.vmdl",
            "weapons/swb/rifles/fal/w_fal.vmdl",
            "weapons/swb/shotguns/spas/w_spas12.vmdl",
            "weapons/swb/smgs/ump45/w_ump45.vmdl"
        };
       
		/// <summary>
		/// Which weapon.
		/// </summary>
		[Property( "weapon_type", Title = "Weapon Type" )]
		public Weapons WeaponType { get; set; } = Weapons.Ump45;

		public override void Spawn()
		{
			base.Spawn();
            SetModel(WeaponWordModels[((int)WeaponType)]);
            SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true ); //the Static has a problem
            PhysicsBody.MotionEnabled = false;
        }
        private long dontspam = 0;
        public bool OnUse(Entity user)
        {
            if (Owner != null)
                return false;

            if (!user.IsValid())
                return false;

            var ply = user.Client.Pawn as PolygonPlayer;

            if (dontspam > DateTimeOffset.Now.ToUnixTimeSeconds())
                return false;

            dontspam = DateTimeOffset.Now.ToUnixTimeSeconds()+1;

            ply.ClearAmmo();
            ply.Inventory.DeleteContents();
            if (WeaponType == Weapons.Deagle)
                ply.Inventory.Add(new SWB_WEAPONS.DEAGLE(), true);
            else if (WeaponType == Weapons.Spas12)
                ply.Inventory.Add(new SWB_WEAPONS.SPAS12(), true);
            else if (WeaponType == Weapons.Ump45)
                ply.Inventory.Add(new SWB_WEAPONS.UMP45(), true);
            else if (WeaponType == Weapons.Fal)
                ply.Inventory.Add(new SWB_WEAPONS.FAL(), true);


            ply.GiveAmmo(AmmoTypes.SMG, 100);
            ply.GiveAmmo(AmmoTypes.Pistol, 60);
            ply.GiveAmmo(AmmoTypes.Revolver, 60);
            ply.GiveAmmo(AmmoTypes.Rifle, 60);
            ply.GiveAmmo(AmmoTypes.Shotgun, 60);

            ply.ActiveChildInput = ply.Children.Select(x => x as WeaponBase).Where(x => x.IsValid() && x.IsUsable()).FirstOrDefault();
            Log.Info(ply.Children.Select(x => x as WeaponBase).Where(x => x.IsValid() && x.IsUsable()).FirstOrDefault());
            ply.ActiveChild = ply.Children.Select(x => x as WeaponBase).Where(x => x.IsValid() && x.IsUsable()).FirstOrDefault();
  

            return false;
        }

        public virtual bool IsUsable(Entity user)
        {
            return true;
        }
    }
}
