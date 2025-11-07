using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(Weapon), menuName = "Recusant/Data/" + nameof(Weapon))]
    public class Weapon : BaseScriptableObject
    {
        public Color WeaponColor;
        public Weapon NextWeapon;

        public override void Precache()
        {

        }
    }
}
