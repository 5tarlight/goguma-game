﻿using System;
using System.Linq;
using Dialogue;
using Entity.Enemy;
using Entity.Item;
using Entity.Npc;
using Player.Attack;
using UnityEngine;

namespace Entity.Player {
  public class PlayerController : Entity {
    public override EntityType type => EntityType.Player;
    public static PlayerController Instance { get; private set; }

    // Components
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerMovement movement;
    private AudioSource audioSrc;
    private BoxCollider2D col;

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
    private Weapon[] weapons;

    [SerializeField]
    private Weapons currentWeapon = 0;

    [SerializeField]
    private SkillType currentAttack = 0;

    [SerializeField]
    private Sprite avatar;

    // Variables
    private bool hasWeapon => anim.GetBool("hasWeapon");
    private float curCoolTime;
    private float curEndTime;
    private float distanceY;
    public Dialogue.Speaker speakerData => new Dialogue.Speaker(entityName, avatar, AvatarPosition.Left);
    public bool isInputCooldown => movement.isInputCooldown;
    public PlayerStatus status;

    private void Awake() {
      if (Instance == null) Instance = this;
      else Destroy(gameObject);
      DontDestroyOnLoad(gameObject);

      rb = GetComponent<Rigidbody2D>();
      anim = GetComponent<Animator>();
      movement = GetComponent<PlayerMovement>();
      audioSrc = GetComponent<AudioSource>();
      col = GetComponent<BoxCollider2D>();

      distanceY = col.bounds.extents.y - 0.1f;
    }


    private void OnDestroy() {
      if (Instance == this) Instance = null;
    }

    private void Update() {
      if (movement.isInputCooldown) return;
      
      TryAttack();
      CheckNpc();
      CheckItems();
    }

    private void TryAttack() {
      if (!hasWeapon || DialogueController.Instance.isEnabled) return;

      if (curCoolTime <= 0) {
        foreach (var key in attackKeys) {
          if (Input.GetKey(key)) {
            Attack(weapons.Get(currentWeapon), key.GetSkill());
            break;
          }
        }
      } else {
        curCoolTime -= Time.deltaTime;
      }

      if (curEndTime <= 0) {
        EndAttack();
      } else {
        curEndTime -= Time.deltaTime;
      }
    }

    private void Attack(Weapon weapon, SkillType atkType) {
      var attack = weapon.Attacks.Get(atkType);

      movement.canFlip = false;
      currentAttack = atkType;
      anim.SetInteger("weaponType", (int) weapon.type);
      anim.SetInteger("attackType", (int) atkType);
      anim.SetBool("isAttack", true);
      anim.SetTrigger("attack");
      curCoolTime = attack.coolTime;
      curEndTime = attack.endTime;

      audioSrc.clip = attack.sound;
      audioSrc.PlayDelayed(attack.soundDelay);

      var colliders = Physics2D.OverlapBoxAll(attack.hitBoxPos.position, attack.hitBoxSize, 0);
      foreach (var col in colliders) {
        if (col.CompareTag("Enemy")) {
          var enemy = col.GetComponent<EnemyController>();
          enemy.Hit(weapon.damage * attack.damagePercent);
        }
      }
    }

    private void EndAttack() {
      anim.SetBool("isAttack", false);
      movement.canFlip = true;
    }

    private void OnDrawGizmos() {
      // Attack HitBox Gizmos
      // if (isStarted) {
      //   var attack = weapons.Get(currentWeapon).Attacks.Get(currentAttack);
      //   Gizmos.color = Color.red;
      //   Gizmos.DrawWireCube(attack.hitBoxPos.position, attack.hitBoxSize);
      // }
      // 
      // Check Npc Ray Gizmos
      // var pos = transform.position;
      // Gizmos.color = Color.yellow;
      // Gizmos.DrawRay(new Vector2(pos.x, pos.y - distanceY), (movement.wasLeft ? Vector2.right : Vector2.left) * checkNpcDistance);
    }

    public bool ChangeWeapon(Weapons type) {
      if (!weapons.Select(weapon => weapon.type).Contains(type)) return false;

      currentWeapon = type;
      anim.SetInteger("weaponType", (int) type);
      anim.SetBool("hasWeapon", type != Weapons.None);

      return true;
    }

    private void Start() {
      ChangeWeapon(Weapons.Sword);
      GetComponent<NameTag>().OnGetEntity(this);
      var testItem = (ItemController)EntityManager.Get(EntityType.Item);
      testItem.SetItem("apple", position: new Vector2(2f,5f));

      var testNpc = (NpcController) EntityManager.Get(EntityType.Npc);
      testNpc.Initialize("TallCarrot", new Vector2(-4.3f, -2.2f));
      
      InvokeRepeating(nameof(SummonTestItem), 0f, 4f);
    }

    private void CheckNpc() {
      if (!Input.GetKeyDown(meetNpcKey)) return;

      var pos = transform.position;
      var hit = Physics2D.Raycast(new Vector2(pos.x, pos.y - distanceY),
        movement.direction, checkNpcDistance, layerMask);

      if (hit && hit.transform.CompareTag("Npc")) {
        var npc = hit.transform.GetComponent<NpcController>();
        
        npc.Meet();
      }
    }

    private void CheckItems() {
      var pos = transform.position;
      var hit = Physics2D.Raycast(new Vector2(pos.x, pos.y - distanceY), movement.direction, pickupDistance, layerMask);
      if (hit && hit.transform.CompareTag("Item")) {
        var item = hit.transform.GetComponent<ItemController>();
        if (!item.isPickingUp)
          item.PickUp(transform, OnPickUpItem);
      }
    }

    public void EnableInputCooldown() => movement.EnableInputCooldown();
    
    private void OnPickUpItem((Item.Item item, byte count) data) {
      Debug.Log($"get: {data.item._name}, count: {data.count}");
      
      
    }

    private void SummonTestItem() {
      var testItem = (ItemController)EntityManager.Get(EntityType.Item);
      testItem.SetItem("apple",position: new Vector2(2f,5f));
    }
    
  }
}