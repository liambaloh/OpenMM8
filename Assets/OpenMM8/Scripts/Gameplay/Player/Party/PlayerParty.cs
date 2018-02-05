﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Assets.OpenMM8.Scripts.Gameplay
{
    [RequireComponent(typeof(HostilityChecker))]
    [RequireComponent(typeof(Damageable))]
    public class PlayerParty : MonoBehaviour, ITriggerListener
    {
        public List<Character> Characters = new List<Character>();
        public Character ActiveCharacter;

        [Header("Sounds - Attack")]
        public List<AudioClip> SwordAttacks = new List<AudioClip>();
        public List<AudioClip> AxeAttacks = new List<AudioClip>();
        public List<AudioClip> BluntAttacks = new List<AudioClip>();
        public List<AudioClip> BowAttacks = new List<AudioClip>();
        public List<AudioClip> DragonAttacks = new List<AudioClip>();
        public List<AudioClip> BlasterAttacks = new List<AudioClip>();

        [Header("Sounds - Got Hit")]
        public AudioClip WeaponVsMetal_Light;
        public AudioClip WeaponVsMetal_Medium;
        public AudioClip WeaponVsMetal_Hard;
        public AudioClip WeaponVsLeather_Light;
        public AudioClip WeaponVsLeather_Medium;
        public AudioClip WeaponVsLeather_Hard;

        [Header("Sounds - Gold")]
        public AudioClip GoldChanged;

        [Header("Misc")]

        [SerializeField]
        private int MinutesSinceSleep;

        private HostilityChecker HostilityChecker;
        public AudioSource PlayerAudioSource;

        private List<GameObject> EnemiesInMeleeRange = new List<GameObject>();
        private List<GameObject> EnemiesInAgroRange = new List<GameObject>();
        private List<GameObject> ObjectsInMeleeRange = new List<GameObject>();

        // Misc
        private float AttackDelayTimeLeft = 0.0f;
        private float TimeSinceLastPartyText = 0.0f;

        public int Gold;
        public int Food;

        private void Awake()
        {
            
        }

        private void Start()
        {
            HostilityChecker = GetComponent<HostilityChecker>();

            Damageable damageable = GetComponent<Damageable>();
            damageable.OnAttackReceieved += new AttackReceived(OnAttackReceived);
            damageable.OnSpellReceived += new SpellReceived(OnSpellReceived);
            PlayerAudioSource = transform.Find("FirstPersonCharacter").GetComponent<AudioSource>();
        }

        public void Update()
        {
            TimeSinceLastPartyText += Time.deltaTime;
            if (TimeSinceLastPartyText > 2.0f)
            {
                SetPartyInfoText("");
            }

            foreach (Character character in Characters)
            {
                character.OnUpdate(Time.deltaTime);
            }

            if (ActiveCharacter == null || !ActiveCharacter.IsRecovered())
            {
                foreach (Character character in Characters)
                {
                    if (character.IsRecovered() && ((ActiveCharacter == null) || !ActiveCharacter.IsRecovered()))
                    {
                        ActiveCharacter = character;
                        ActiveCharacter.CharacterUI.SelectionRing.enabled = true;
                    }
                    else
                    {
                        character.CharacterUI.SelectionRing.enabled = false;
                    }
                }
            }

            AttackDelayTimeLeft -= Time.deltaTime;

            //HandleHover();

            if (Input.GetButton("Attack") && (AttackDelayTimeLeft <= 0.0f))
            {
                Attack();
            }

            if (Input.GetButtonDown("Interact"))
            {
                Interact();
            }
        }

        private void Attack()
        {
            if (ActiveCharacter != null && ActiveCharacter.IsRecovered())
            {
                Damageable victim = null;

                // 1) Try to attack NPC which is being targeted by the crosshair
                //    - does not have to be enemy, when we are aiming with attack
                //    directly on NPC, we can attack guard / villager this way
                RaycastHit hit;
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.595F, 0));
                if (Physics.Raycast(ray, out hit, 100.0f, 1 << LayerMask.NameToLayer("NPC")))
                {
                    Transform objectHit = hit.collider.transform;
                    if ((objectHit.GetComponent<Damageable>() != null) &&
                        (ObjectsInMeleeRange.Contains(objectHit.gameObject)))
                    {
                        victim = objectHit.GetComponent<Damageable>();
                    }
                }

                // 2) Try to attack enemy which is closest to Player
                if ((victim == null) && (EnemiesInMeleeRange.Count > 0))
                {
                    EnemiesInMeleeRange.OrderBy(t => (t.transform.position - transform.position).sqrMagnitude);
                    foreach (GameObject enemyObject in EnemiesInMeleeRange)
                    {
                        if (enemyObject.GetComponent<Renderer>().isVisible)
                        {
                            victim = enemyObject.GetComponent<Damageable>();
                            break;
                        }
                    }
                }

                if (victim == null)
                {
                    // No victim in Player's melee range was found. Try to find some in range if player has a bow / crossbow
                }

                /*if (victim != null)
                {
                    Debug.Log("Hit with ");
                }*/

                ActiveCharacter.Attack(victim);
                ActiveCharacter = null;
                AttackDelayTimeLeft = 0.1f;
            }
        }

        private bool Interact()
        {
            Interactable interactObject = null;

            ObjectsInMeleeRange.RemoveAll(go => go == null || 
                Vector3.Distance(transform.position, go.transform.position) > Constants.MeleeRangeDistance);

            // 1) Try to interact with object being targeted by Crosshair
            RaycastHit hit;
            Ray ray = Camera.main.ViewportPointToRay(Constants.CrosshairScreenRelPos);

            int layerMask = ~((1 << LayerMask.NameToLayer("NpcRangeTrigger")) | (1 << LayerMask.NameToLayer("Player")));
            if (Physics.Raycast(ray, out hit, 100.0f, layerMask))
            {
                Transform objectHit = hit.collider.transform;
                if ((objectHit.GetComponent<Interactable>() != null) &&
                    (objectHit.GetComponent<Interactable>().enabled) &&
                    (Vector3.Distance(transform.position, objectHit.transform.position) < Constants.MeleeRangeDistance))
                {
                    Debug.Log("Can interact with: " + objectHit.name);
                    interactObject = objectHit.GetComponent<Interactable>();
                }

                // Handle also HoverInfo
                if ((objectHit.GetComponent<HoverInfo>() != null) &&
                    (objectHit.GetComponent<HoverInfo>().enabled))
                {
                    string hoverText = objectHit.GetComponent<HoverInfo>().HoverText;
                    SetPartyInfoText(hoverText, true);
                }
            }

            // 2) Try to interact within any visible object within melee distance
            // Should I even want this to happen ?
            // Problem here is that Player's melee sensor does not intersect corpse's modified collider
            if (interactObject == null)
            {
                /*List<RaycastHit> closeObjects = Physics.SphereCastAll(
                    transform.position,
                    Constants.MeleeRangeDistance,
                    transform.forward, 
                    layerMask)
                    .ToList();

                Debug.Log("Found: " + closeObjects.Count);

                if (closeObjects.Count > 0)
                {
                    closeObjects.RemoveAll(r => r.distance > Constants.MeleeRangeDistance ||
                        r.collider.gameObject.GetComponent<Renderer>() == null ||
                        r.collider.gameObject.GetComponent<Interactable>() == null ||
                        r.collider.gameObject.GetComponent<Renderer>().isVisible == false);
                    if (closeObjects.Count > 0)
                    {
                        interactObject = closeObjects.OrderBy(r => (r.transform.position - transform.position).sqrMagnitude).
                            FirstOrDefault().
                            transform.GetComponent<Interactable>();
                    }
                }*/

                /*if ((interactObject == null) && (ObjectsInMeleeRange.Count > 0))
                {
                    ObjectsInMeleeRange.OrderBy(t => (t.transform.position - transform.position).sqrMagnitude);
                    foreach (GameObject closeObject in ObjectsInMeleeRange)
                    {
                        if (closeObject.GetComponent<Renderer>().isVisible &&
                            closeObject.GetComponent<Interactable>() != null)
                        {
                            interactObject = closeObject.GetComponent<Interactable>();
                            break;
                        }
                    }
                }*/
            }

            if (interactObject != null)
            {
                return interactObject.Interact(this.gameObject);
            }

            return false;
        }

        private bool HandleHover()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ViewportPointToRay(Constants.CrosshairScreenRelPos);
            int layerMask = ~((1 << LayerMask.NameToLayer("NpcRangeTrigger")) | (1 << LayerMask.NameToLayer("Player")));
            if (Physics.Raycast(ray, out hit, 1000.0f, layerMask))
            {
                Transform objectHit = hit.collider.transform;

                if ((objectHit.GetComponent<HoverInfo>() != null) &&
                    (objectHit.GetComponent<HoverInfo>().enabled))
                {
                    string hoverText = objectHit.GetComponent<HoverInfo>().HoverText;
                    SetPartyInfoText(hoverText, true);
                    return true;
                }
            }

            return false;
        }

        private void UpdatePartyEffects(int msDiff)
        {

        }

        private void UpdateConditions(int msDiff)
        {

        }

        private void UpdatePartyAgroStatus(AgroState agroState)
        {
            foreach (Character character in Characters)
            {
                character.CharacterUI.SetAgroStatus(agroState);
            }
        }

        public void AddCharacter(Character character)
        {
            character.PlayerParty = this;
            Characters.Add(character);
        }

        // Damageable events
        AttackResult OnAttackReceived(AttackInfo hitInfo, GameObject source)
        {
            Character hitCharacter = null;
            if (hitInfo.PreferredClass != Class.None)
            {
                List<Character> preferredCharacters = new List<Character>();
                foreach (Character character in Characters)
                {
                    if (character.CharacterModel.Class == hitInfo.PreferredClass)
                    {
                        preferredCharacters.Add(character);
                    }
                }

                if (preferredCharacters.Count > 0)
                {
                    hitCharacter = preferredCharacters[UnityEngine.Random.Range(0, preferredCharacters.Count)];
                }
                else
                {
                    hitCharacter = Characters[UnityEngine.Random.Range(0, Characters.Count)];
                }
            }
            else
            {
                hitCharacter = Characters[UnityEngine.Random.Range(0, Characters.Count)];
            }

            if (hitCharacter == null)
            {
                Debug.LogError("hitCharacter is null !");
                return new AttackResult();
            }

            AttackResult result = DamageCalculator.DamageFromNpcToPlayer(hitInfo,
                hitCharacter.CharacterModel.DefaultStats.Resistances,
                hitCharacter.CharacterModel.DefaultStats.ArmorClass,
                hitCharacter.CharacterModel.DefaultStats.Attributes[Attribute.Luck]);
            if (result.Type == AttackResultType.Miss)
            {
                return result;
            }

            hitCharacter.ModifyCurrentHitPoints(-1 * result.DamageDealt);
            if (hitCharacter.CharacterModel.CurrHitPoints <= 0)
            {
                result.Type = AttackResultType.Kill;
            }

            return result;
        }

        SpellResult OnSpellReceived(SpellInfo hitInfo, GameObject source)
        {
            return new SpellResult();
        }

        //---------------------------------------------------------------------
        // Triggers
        //---------------------------------------------------------------------
        public void OnObjectEnteredMyTrigger(GameObject other, TriggerType triggerType)
        {
            //Debug.Log("Entered: " + other.name);
            switch (triggerType)
            {
                case TriggerType.MeleeRange:
                    OnObjectEnteredMeleeRange(other);
                    break;

                case TriggerType.AgroRange:
                    OnObjectEnteredAgroRange(other);
                    break;

                case TriggerType.ObjectTrigger:
                    OnObjectEnteredInteractibleRange(other);
                    break;

                default:
                    Debug.LogError("Unhandled Trigger Type: " + triggerType);
                    break;
            }
        }

        public void OnObjectLeftMyTrigger(GameObject other, TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.MeleeRange:
                    OnObjectLeftMeleeRange(other);
                    break;

                case TriggerType.AgroRange:
                    OnObjectLeftAgroRange(other);
                    break;

                case TriggerType.ObjectTrigger:
                    OnObjectLeftInteractibleRange(other);
                    break;

                default:
                    Debug.LogError("Unhandled Trigger Type: " + triggerType);
                    break;
            }
        }

        private void OnObjectLeftInteractibleRange(GameObject other)
        {
            
        }

        private void OnObjectEnteredInteractibleRange(GameObject other)
        {
            
        }

        public void OnObjectEnteredMeleeRange(GameObject other)
        {
            if (HostilityChecker.IsHostileTo(other))
            {
                EnemiesInMeleeRange.Add(other);
                UpdateAgroStatus();
            }

            ObjectsInMeleeRange.Add(other);
        }

        public void OnObjectLeftMeleeRange(GameObject other)
        {
            EnemiesInMeleeRange.Remove(other);
            UpdateAgroStatus();

            ObjectsInMeleeRange.Remove(other);
        }

        public void OnObjectEnteredAgroRange(GameObject other)
        {
            if (HostilityChecker.IsHostileTo(other))
            {
                EnemiesInAgroRange.Add(other);
                UpdateAgroStatus();
            }
        }

        public void OnObjectLeftAgroRange(GameObject other)
        {
            EnemiesInAgroRange.Remove(other);
            UpdateAgroStatus();
        }

        private void UpdateAgroStatus()
        {
            if (EnemiesInMeleeRange.Count > 0)
            {
                UpdatePartyAgroStatus(AgroState.HostileClose);
            }
            else if (EnemiesInAgroRange.Count > 0)
            {
                UpdatePartyAgroStatus(AgroState.HostileNearby);
            }
            else
            {
                UpdatePartyAgroStatus(AgroState.Safe);
            }
        }

        public void OnAcquiredLoot(Loot loot)
        {
            // Handle item

            if (loot.GoldAmount > 0)
            {
                AddGold(loot.GoldAmount);
                SetPartyInfoText("You found " + loot.GoldAmount.ToString() + " gold !");
            }
        }

        public void AddGold(int amount)
        {
            GameMgr.Instance.PartyUI.AddGold(amount);
            PlayerAudioSource.PlayOneShot(GoldChanged, 1.5f);
        }

        public void AddFood(int amount)
        {
            GameMgr.Instance.PartyUI.AddFood(amount);
        }

        public void SetPartyInfoText(string text, bool onlyIfEmpty = false)
        {
            if (onlyIfEmpty && GameMgr.Instance.PartyUI.HoverInfoText.text != "")
            {
                return;
            }

            GameMgr.Instance.PartyUI.HoverInfoText.text = text;
            TimeSinceLastPartyText = 0.0f;
        }
    }
}
