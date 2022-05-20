using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SteamShared.Models
{
    public class CsgoWeapon
    {
        /// <summary>
        /// Gets or sets the class name of this weapon.
        /// e.g. AK47 would be "weapon_ak47"
        /// </summary>
        public string? ClassName { get; set; }

        /// <summary>
        /// Gets or sets the base damage that the weapon deals, in health points.
        /// e.g. AK47 would have 36 base damage
        /// </summary>
        public int BaseDamage { get; set; } = -1;

        /// <summary>
        /// Gets or sets the armor penetration of this weapon as a percentage.
        /// In the files this is saved as a float from 0 (0% penetration) to 2 (100% penetration).
        /// </summary>
        public float ArmorPenetration { get; set; } = -1;

        /// <summary>
        /// Gets or sets the damage dropoff that this weapon has at 500 units distance. It's saved as a "RangeModifier" stat,
        /// where the modifier is a float value between 0 and 1 that contains the factor of the damage left.
        /// A value of 0.75 would mean, at 500 units the weapon loses 25% of its damage.
        /// The damage left can be calculated by: DAMAGE * RangeModifier^(DISTANCE / 500)
        /// </summary>
        public double DamageDropoff { get; set; } = -1;

        /// <summary>
        /// Gets or sets the maximum range a bullet of this weapon can travel in units. After this distance the "bullet" will just disappear.
        /// </summary>
        public int MaxBulletRange { get; set; } = -1;

        /// <summary>
        /// Gets or sets the multiplier of headshots by this weapon. At the point of writing this is "4" for all weapons except two.
        /// </summary>
        public float HeadshotModifier { get; set; } = -1;

        /// <summary>
        /// Gets or sets the type of damage done by this weapon.
        /// Used for damage multipliers, since shock damage deals the same damage on every hitgroup.
        /// </summary>
        public DamageType DamageType { get; set; }

        /// <summary>
        /// Gets or sets the fire rate of this weapon (60 / cycletime => because cycletime is the amount of seconds needed for the weapon to cycle)
        /// </summary>
        public double FireRate { get; set; } = -1;

        /// <summary>
        /// Gets or sets the maximum speed a player can run at while holding this weapon, in units per second. 
        /// </summary>
        public int RunningSpeed { get; set; } = -1;
    }

    public enum DamageType { Bullet, Shock }
}
