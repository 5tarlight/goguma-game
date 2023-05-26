﻿using Audio;
using Dialogue;
using Entity.Enemy;
using Entity.Item;
using Entity.Npc;
using Entity.Player.Attack;
using Inventory;
using Inventory.QuickSlot;
using UnityEngine;
using Window;

namespace Entity.Player {
  public class PlayerController : Entity {
    public override EntityType type => EntityType.Player;
    public static PlayerController Instance { get; private set; }

    // Components
    // private Rigidbody2D rb;
    private Animator anim;
    private PlayerMovement movement;
    // private AudioSource audioSrc;
    private HpBar hpBar;

    // Inspector Settings
    [SerializeField]
    private KeyCode[] attackKeys;

    [SerializeField]
    private KeyCode meetNpcKey = KeyCode.C;

    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private float checkNpcDistance = 3f;

    [SerializeField]
    private float pickupDistance;


    [SerializeField]
    private Sprite avatar;

    public QuickSlotController quickSlotCtrler;

    [Header("Hand")]
    [SerializeField]
    private SpriteRenderer[] hands;

    // Variables
    private float distanceY;
    public Dialogue.Speaker speakerData => new Dialogue.Speaker(entityName, avatar, AvatarPosition.Left);
    public bool isInputCooldown => movement.isInputCooldown;
    public PlayerStatus status;
    public Inventory.Inventory inventory;

    // Attack Vars
    private float curCoolTime;
    private float curEndTime;
    private float curKeepTime;
    private byte combo;
    private KeyCode skillType;
    private float tempCoolTime;
    private bool cooled;
    private bool isEnd;
    
    private Vector2 attackHitPos = Vector2.zero;
    private Vector2 attackHitSize = Vector2.zero;

    protected override void Awake() {
      if (Instance == null) Instance = this;
      else Destroy(gameObject);
      DontDestroyOnLoad(gameObject);

      // rb = GetComponent<Rigidbody2D>();
      anim = GetComponent<Animator>();
      movement = GetComponent<PlayerMovement>();
      // audioSrc = GetComponent<AudioSource>();
      col = GetComponent<BoxCollider2D>();
      hpBar = GetComponent<HpBar>();

      quickSlotCtrler.onSlotChanged += OnChangedSlot;

      distanceY = col.bounds.extents.y - 0.1f;
      inventory = new Inventory.Inventory(InventoryController.horizontalCount * 7);
      InventoryController.Instance.inventory = inventory;
      canDespawn = false;
      quickSlotCtrler.SetIndex(0);

      DisableAllHand();
    }

    private void Update() {
      DebugKey();
      if (curCoolTime > 0) curCoolTime -= Time.deltaTime;

      if (curEndTime > 0) curEndTime -= Time.deltaTime;
      else {
        if (isEnd) {
          curKeepTime = 0;
          combo = 0;
          isEnd = false;
        }

        movement.canFlip = true;
      }

      if (curKeepTime > 0) curKeepTime -= Time.deltaTime;
      else {
        anim.SetBool("isAttack", false);
        if (!cooled) {
          cooled = true;
          curCoolTime = tempCoolTime;
        }
      }

      if (movement.isInputCooldown || InputBoxWindow.isEnabled) return;

      TryInteract();
      CheckNpc();
      CheckItems();

      if (!Input.anyKeyDown) return;
      int slotIdx = Input.inputString switch {
        "1" => 0, "2" => 1, "3" => 2, "4" => 3, "5" => 4, "6" => 5, "7" => 6, "8" => 7, "9" => 8, _ => -1
      };
      if (slotIdx != -1)
        quickSlotCtrler.SetIndex((byte)slotIdx);
    }

    private void DebugKey() {
      if (Input.GetKeyDown(KeyCode.F6)) {
        Entity.SummonEnemy(new Vector2(position.x + 3f, 5f));
      } else if (Input.GetKeyDown(KeyCode.F7)) {
        Entity.SummonItem(new Vector2(position.x + 3f, 5f), ItemManager.GetInstance().GetWithCode("appleBuff"), 20);
      }
    }

    private void OnChangedSlot(byte slotIdx) {
      var item = quickSlotCtrler.GetItem(slotIdx);
      DisableAllHand();
      switch (item) {
        case null:
          anim.SetInteger("weaponType", 0);
          return;

        case WeaponItem weapon: {
          var hand = hands[(int)weapon.weaponType];
          hand.gameObject.SetActive(true);
          hand.sprite = weapon.weaponSprite;
          anim.SetInteger("weaponType", (int)weapon.weaponType);
          break;
        }

        default: {
          var hand = hands[0];
          hand.gameObject.SetActive(true);
          hand.sprite = item.sprite;
          anim.SetInteger("weaponType", 0);
          break;
        }
      }
    }

    private void DisableAllHand() {
      foreach (var hand in hands) {
        hand.gameObject.SetActive(false);
      }
    }

    private void Attack(Skill skill, KeyCode key) {
      var comboSkill = skill.GetComboSkill(key);

      if (!(curCoolTime <= 0) || !(curEndTime <= 0)) return;
      cooled = false;
      if (skillType == key && curKeepTime > 0) {
        combo++;
        if (combo < comboSkill.skills.Length) {
          var curSkill = comboSkill.skills[combo];
          StartAttack(skill.damage, curSkill, comboSkill.keepComboTime, comboSkill.coolTime);
        }

        if (combo + 1 >= comboSkill.skills.Length) {
          isEnd = true;
        }
      } else {
        skillType = key;
        StartAttack(skill.damage, comboSkill.skills[0], comboSkill.keepComboTime, comboSkill.coolTime);
        combo = 0;
      }
    }

    private void StartAttack(float weaponDmg, ComboSkill skill, float keepComboTime, float coolTime) {
      curKeepTime = keepComboTime;
      tempCoolTime = coolTime;
      curEndTime = skill.endTime;

      // Debug.Log(skill.animParameter);
      movement.canFlip = false;
      anim.SetInteger("attackType", skill.animParameter);
      anim.SetBool("isAttack", true);
      // anim.SetTrigger("attack");
      AudioManager.Play(skill.sound);

      attackHitPos = skill.hitBoxPos;
      attackHitSize = skill.hitBoxSize;
      attackHitPos.x *= (int)movement.currentDirection;

      var colliders = Physics2D.OverlapBoxAll(position + attackHitPos, attackHitSize, 0);
      foreach (var hitCol in colliders) {
        if (hitCol.CompareTag("Enemy")) {
          var enemy = hitCol.GetComponent<EnemyController>();
          enemy.Hit(weaponDmg * skill.damagePercent, position.x);
        }
      }
    }

    private void OnDrawGizmos() {
      Gizmos.DrawWireCube(transform.position + (Vector3)attackHitPos, attackHitSize);
    }

    private void TryInteract() {
      foreach (var key in attackKeys) {
        if (Input.GetKeyDown(key)) {
          var item = quickSlotCtrler.GetItem();

          switch (item) {
            case null:
              return;

            case WeaponItem weapon: {
              Attack(weapon.skill, key);
              break;
            }

            case UseableItem useable: {
              useable.OnQuickClick();
              break;
            }
            // default: {
            //   var hand = hands[0];
            //   
            //   break;
            // }
          }
        }
      }
    }

    private void Start() {
      GetComponent<NameTag>().OnGetEntity(this);
      hpBar.OnGetEntity(this);
      hpBar.maxHp = status.maxHp;
      hpBar.curHp = status.hp;

      var testNpc = (NpcController)EntityManager.Get(EntityType.Npc);
      testNpc.Initialize("TallCarrot", new Vector2(-4.3f, -2.2f));

      inventory.GainItem(ItemManager.Instance.GetWithCode("iron_sword"));
    }

    private void CheckNpc() {
      if (!Input.GetKeyDown(meetNpcKey)) return;

      var pos = transform.position;
      var hit = Physics2D.Raycast(new Vector2(pos.x, pos.y - distanceY),
        movement.dirVector, checkNpcDistance, layerMask);

      if (hit && hit.transform.CompareTag("Npc")) {
        var npc = hit.transform.GetComponent<NpcController>();

        npc.Meet();
      }
    }

    private void CheckItems() {
      var pos = transform.position;
      var hit = Physics2D.Raycast(new Vector2(pos.x, pos.y - distanceY), movement.dirVector, pickupDistance, layerMask);
      if (hit && hit.transform.CompareTag("Item")) {
        var item = hit.transform.GetComponent<ItemController>();
        if (!item.isPickingUp && !item.isThrowing)
          item.PickUp(transform, OnPickUpItem);
      }
    }

    public void EnableInputCooldown() => movement.EnableInputCooldown();

    private void OnPickUpItem((Item.Item item, byte count) data) {
      // Debug.Log($"get: {data.item._name}, count: {data.count}");
      AudioManager.Play("pickup_item");
      var left = inventory.GainItem(data.item, data.count);
      InventoryController.Instance.Refresh();
      if (left > 0) {
        ThrowItem(data.item, left);
      }
    }

    public void ThrowItem(Item.Item item, ushort count) =>
      base.ThrowItem(item, count, (sbyte)(movement.currentDirection == Direction.Left ? -1 : 1));
  }
}