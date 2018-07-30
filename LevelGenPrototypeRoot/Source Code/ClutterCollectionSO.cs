using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ClutterCollection", menuName = "PCG System/Clutter Collection Object", order = 2)]
public class ClutterCollectionSO : ScriptableObject {

    [SerializeField]
    public List<GameObject> clutter;

    [Range(0,1)]
    [SerializeField]
    public float[] associatedChances;
}
