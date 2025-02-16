using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Recusant/Data/Weapon")]
public class Weapon : BaseScriptableObject
{
    public Color WeaponColor;
    public Weapon NextWeapon;

    public override void Precache()
    {
        
    }
}
