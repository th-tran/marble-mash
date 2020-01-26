﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public GameObject clearFXPrefab;
    public GameObject breakFXPrefab;
    public GameObject doubleBreakFXPrefab;

    // Singleton pattern
    static ParticleManager _instance;
    public static ParticleManager Instance { get { return _instance; } }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void ClearMarbleFXAt(int x, int y, int z = 0)
    {
        if (clearFXPrefab != null)
        {
            GameObject clearFX = Instantiate(clearFXPrefab, new Vector3(x,y,z), Quaternion.identity) as GameObject;

            ParticlePlayer particlePlayer = clearFX.GetComponent<ParticlePlayer>();

            if (particlePlayer != null)
            {
                particlePlayer.Play();
            }
        }
    }

    public void BreakTileFXAt(int breakableValue, int x, int y, int z = 0)
    {
        GameObject breakFX = null;
        ParticlePlayer particlePlayer = null;

        if (breakableValue > 1)
        {
            if (doubleBreakFXPrefab != null)
            {
                breakFX = Instantiate(doubleBreakFXPrefab, new Vector3(x,y,z), Quaternion.identity) as GameObject;
            }
        }
        else
        {
            if (breakFXPrefab != null)
            {
                breakFX = Instantiate(breakFXPrefab, new Vector3(x,y,z), Quaternion.identity) as GameObject;
            }
        }

        if (breakFX != null)
        {
            particlePlayer = breakFX.GetComponent<ParticlePlayer>();

            if (particlePlayer != null)
            {
                particlePlayer.Play();
            }
        }
    }
}