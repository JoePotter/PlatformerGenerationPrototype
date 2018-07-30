using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClutterCollection_Theme", menuName = "PCG System/Clutter Collection (Entire Theme)", order = 1)]
public class ClutterCollectionTheme : ScriptableObject
{
    [SerializeField]
    public ClutterCollectionSO groundClutter;

    [SerializeField]
    public ClutterCollectionSO ceilingClutter;

    [SerializeField]
    public ClutterCollectionSO leftWallClutter;

    [SerializeField]
    public ClutterCollectionSO rightWallClutter;
}