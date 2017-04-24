﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    // Set in editor
    public Material playerMat;
    public PlayerNum humanPlayer;
    public GameObject[] levels;
    public static bool playing = false; // Out of time - global state!
    public GameObject startScreenUI;
    public GameObject victoryScreenUI;
    public GameObject defeatScreenUI;
    public GameObject finalVictoryScreenUI;

    private GameObject currentLevel;
    private int levelIndex;
    private Dictionary<Color, Material> sharedMats = new Dictionary<Color, Material>();
    private Base[] allBases = new Base[0];

	// Use this for initialization
	void Start () {
        /* Commenting this out while I'm not building levels
        // Make sure we didn't leave a level up
        foreach (Base bas in FindObjectsOfType<Base>()) {
            GameObject level = bas.transform.parent.gameObject;
            for (int i = 0; i < levels.Length; i++) {
                if (levels[i] == level) {
                    levelIndex = i;
                    level.SetActive(false);
                    break;
                }
            }
        }
        */

        startScreenUI.SetActive(true);
        victoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () {
        if (playing) {
            CheckVictory();
            CheckDefeat();
        } else if (Input.GetKeyDown(KeyCode.R) && defeatScreenUI.activeSelf) {
            RestartLevel();
        }
    }

    private void CheckVictory() {
        foreach (Base bas in allBases) {
            if (bas.owningPlayer != humanPlayer) {
                return;
            }
        }

        victoryScreenUI.SetActive(true);
        playing = false;
    }

    private void CheckDefeat() {
        foreach (Base bas in allBases) {
            if (bas.owningPlayer == humanPlayer) {
                return;
            }
        }

        defeatScreenUI.SetActive(true);
        playing = false;
    }

    public Material GetPlayerSharedMat(Color color) {
        if (!sharedMats.ContainsKey(color)) {
            Material sharedMat = new Material(playerMat);
            sharedMat.color = color;
            sharedMats[color] = sharedMat;
        }
        return sharedMats[color];
    }

    public Base[] GetAllBases() {
        return allBases;
    }

    public Pawn[] GetAllPawns() {
        return FindObjectsOfType<Pawn>();
    }

    ////////////////////
    //  UI METHODS
    ////////////////////
    public void StartGame() {
        RestartLevel();
    }

    public void NextLevel() {
        levelIndex++;
        RestartLevel();
    }

    public void RestartLevel() {
        // Clear existing level
        foreach (Pawn pawn in FindObjectsOfType<Pawn>()) {
            Destroy(pawn.gameObject);
        }
        Destroy(currentLevel);

        // Setup new level
        currentLevel = Instantiate(levels[levelIndex]);
        currentLevel.SetActive(true);
        allBases = currentLevel.GetComponentsInChildren<Base>();

        // Setup camera
        foreach (Base bas in allBases) {
            if (bas.owningPlayer == humanPlayer) {
                Camera.main.transform.position = bas.transform.position + bas.transform.up * 5;
                Camera.main.transform.rotation = Quaternion.identity;
                break;
            }
        }

        // Other state
        playing = true;
        startScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);
    }
}
