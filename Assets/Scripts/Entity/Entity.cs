﻿using System;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace Entity {
  public abstract class Entity : MonoBehaviour {
    [FormerlySerializedAs("name")]
    public string entityName;
    public abstract EntityType type { get; }

    public virtual Vector3 position {
      get => transform.position;
      set => transform.position = value;
    }

    public virtual Vector2 size {
      get => transform.localScale;
      set => transform.localScale = new Vector3(value.x, value.y, transform.localScale.z);
    }

    // private void OnBecameInvisible() => Release();

    public void Release() => EntityManager.Release(this);
    
  }
}