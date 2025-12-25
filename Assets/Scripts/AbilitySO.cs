using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability Data")]
public class AbilitySO : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public GameObject abilityPrefab;
}