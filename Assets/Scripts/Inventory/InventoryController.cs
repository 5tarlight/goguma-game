﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inventory {
  public class InventoryController : MonoBehaviour {
    public static InventoryController Instance { get; private set; }

    public KeyCode openKey = KeyCode.I;
    
    public bool activeInventory { get; private set; } = false;

    [Header("UI Object")]
    [SerializeField]
    private GameObject panel;
    
    [SerializeField]
    private Transform content;
    
    [Header("Slot")]
    [SerializeField]
    private Slot slotPrefab;    
    
    [SerializeField]
    private byte slotCount = 28;

    private List<Slot> slots = new List<Slot>();
    
    
    private void Awake() {
      if (Instance == null) Instance = this;
      else Destroy(this);
      // DontDestroyOnLoad(gameObject);
      
      panel.SetActive(activeInventory);
      SetCount(slotCount);
    }

    private void Update() {
      if (Input.GetKeyDown(openKey)) {
        ToggleActive();
      }
    }

    private void ToggleActive() {
      activeInventory = !activeInventory;
      panel.SetActive(activeInventory);
    }

    public void SetCount(byte count) {
      slotCount = count;
      ClearSlot();

      for (var i = 0; i < slotCount; i++) {
        var slot = Instantiate(slotPrefab, content);
        slots.Add(slot);
      }
    }

    private void ClearSlot() {
      if (slots.Count == 0) return;
      foreach (var slot in slots) {
        Destroy(slot.gameObject);
      }
      slots.Clear();
    }
  }
}