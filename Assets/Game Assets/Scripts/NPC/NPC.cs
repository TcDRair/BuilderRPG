using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC {
    private float _prvStm = 100, _prvSpc = 0, _prvStf = 30;
    public float Stamina {
        get => _prvStm;
        protected set => _prvStm = Mathf.Clamp(value, 0, 100);
    }
    public float Suspicion {
        get => _prvSpc;
        protected set => _prvSpc = Mathf.Clamp(value, -20, 150);
    }
    public float Satisfaction {
        get => _prvStf;
        protected set => _prvStf = Mathf.Clamp(value, -100, 100);
    }
    public float Delta { get; protected set; }
    public enum Role { Citizen, Merchant, Vendor }
    public List<Role> role = new();
    public List<Routine> routines = new();
    public List<UexAct> uexActs = new();

    public GameObject gameObject;
    public Animator animator;
    public NavMeshAgent agent;


    private void ShowStressfulMotion() {
        //TODO Animator
    }
    public NPC() {
        uexActs.Add(new(3, 2, ShowStressfulMotion, () => Satisfaction < -20));
    }
}

public class NPCCitizen : NPC {
    public NPCCitizen() {
        role.Add(Role.Citizen);
    }
}

public class NPCVendor : NPCCitizen {
    public GameObject kiosk;
    public Transform kioskPosition;
    public NavMeshData workingArea;

    public NPCVendor() {
        role.Add(Role.Vendor);
        routines.Add(new Routine {
            Order = 2,
            StartCondition = () => true,
            RoutineAction = new() { new(0, GotoWorkingArea, () => true, 99) },
            EndCondition = () => Vector3.Distance(gameObject.transform.position, workingArea.position) < 1
        });
    }

    public void GotoWorkingArea() {
        agent.SetDestination(workingArea.position);
    }
}

public class Routine {
    public int Order { get; init; }
    public Func<bool> StartCondition { get; init; }
    public List<NormalAct> RoutineAction { get; init; }
    public Func<bool> EndCondition { get; init; }
}

public abstract class Act {
    public Action Action { get; init; }
    public Func<bool> Condition { get; init; }
    public int Cooldown { get; init; }
    public Act(int cooldown, Action action, Func<bool> condition) {
        Action = action;
        Cooldown = cooldown;
        Condition = condition;
    }
}
public class NormalAct : Act {
    public int Ratio { get; init; }
    public NormalAct(int cooldown, Action action, Func<bool> condition, int ratio) : base(cooldown, action, condition) {
        Ratio = ratio;
    }
}
public class UexAct : Act { // Unexpected Act
    public int Priority { get; init; }
    public UexAct(int priority, int cooldown, Action action, Func<bool> condition) : base(cooldown, action, condition) {
        Priority = priority;
    }
}


namespace System.Runtime.CompilerServices { // For Unity
    class IsExternalInit { }
}