﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NicPlayer : PlayerBase
{
    //public GameObject autoPart;
    public Hurtbox QHB;
    public Hurtbox EHitbox;
    float damageReduction;
    float div;
    int autoOn = 1;
    public GameObject projSpawn;
    bool QTargetted;
    Creep creepQ;
    public GameObject hook;
    public GameObject hookObject;
    float followSharp = 0.1f;
    Vector3 followOffset;
    Vector3 upwardsOffset = new Vector3(0, 3, 0);
    public GameObject Qind2;
    bool QSlamming = false;
    Vector3 hookpos;
    bool goingUp;
    bool ticAuto;
    public GameObject leftTic;
    public GameObject rightTic;
    public GameObject shieldVis;
    public List<float> extraMaxShield = new List<float>();
    public List<int> shieldBaseScale = new List<int>();
    public List<int> monicaBaseHealth = new List<int>();
    int monicaHealth;
    int shieldDMG;
    float maxHPShield;
    bool movingHookE;
    public bool monicaAlive;
    Vector3 lanternPosition;
    public MonicaController monica;
    public GameObject monicaPointer;
    public GameObject monicaPos;
    public GameObject lanternPos;
    public AudioManager audManager;
    public int passiveStacks;
    public GameObject soul;
    void OnEnable()
    {
        shieldDMG = shieldBaseScale[0];
        maxHPShield = extraMaxShield[0];
        monicaHealth = monicaBaseHealth[0];
        StartCoroutine(ticcolas());
    }
    public void qNoise()
    {
        audManager.audioList[0].Play();
    }
    public void TouchSoul()
    {
        passiveStacks += 1;
        string Pdesc = "Every enemy defeated via auto attack has a chance to spawn a soul. Walking over a soul absorbs it, permanently increasing Nic's armor and ability power by 1. \n\n\nCurrent souls: " + passiveStacks;
        passiveDesc.GetComponentInParent<AbilityIndicator>().updateAbilityDescription(Pdesc);
        updateItemStats();
    }
    public void hitQ(Creep creepHit)
    {
        StartCoroutine(endQ());
        creepQ = creepHit;
        followOffset = creepQ.transform.position - transform.position;
        QTargetted = true;
        Anim.SetBool("hooked", true);
        hook.SetActive(true);
        audManager.audioList[3].Play();
    }
    protected override void updateItemStats()
    {
        AttackDamage = startAD + (ADPerLevel * level);
        AttackSpeed /= itemAtkSpeed + 1;
        Armor = startArmor;
        MagicResist = startMagicRes;       
        itemAD = 0;
        itemAP = 0;
        itemAtkSpeed = 0;
        itemHealth = 0;
        CDReduction = 0;
        foreach (Item statItem in items)
        {
            itemAD += statItem.attackDamage;
            itemAP += statItem.abilityPower;
            itemAtkSpeed += statItem.attackSpeed * 0.01f;
            itemHealth += statItem.health;
            MagicResist += statItem.magicResist;
            Armor += statItem.armor;
            CDReduction += statItem.cooldownReduction;
        }
        AttackDamage += itemAD;
        AbilityPower += itemAP;
        AttackSpeed *= itemAtkSpeed + 1;
        maxHealth += itemHealth;
        if (CDReduction > 45)
        {
            CDReduction = 45;
        }
        if (CDReduction > 0 && newCDR > 0)
        {
            CDQ /= newCDR;
            CDW /= newCDR;
            CDE /= newCDR;
            CDR /= newCDR;
        }
        newCDR = CDReduction * 0.01f;
        newCDR = 1 - newCDR;
        CDQ *= newCDR;
        CDW *= newCDR;
        CDE *= newCDR;
        CDR *= newCDR;
        AbilityPower += passiveStacks;

        if (itemsHad[0])
        {
            AbilityPower = (int)(AbilityPower * 1.3f);
        }
        if (itemsHad[4])
        {
            maxHealth += (int)(AttackDamage * 3f);
        }
        health += itemHealth;
        itemChange();
        abilityDescription();
        Armor += passiveStacks;
    }
 
    public override void activateQ()
    {
        if (canAttack)
        {
            if (QTargetted)
            {
                hookpos = hookObject.transform.position;
                QSlamming = true;
                canMove = false;
                forceStopMoving();
                hookObject.transform.position = hookpos;
                graphics.transform.LookAt(Qind2.transform.position);
                Anim.SetTrigger("QSlam");
                Anim.SetBool("isAttacking", true);
                Anim.SetBool("isIdle", false);
                //QHB.damage = QDamage + (int)(AbilityPower * 0.75);
                canAttack = false;
            }else
            {
                QSlamming = false;
                canMove = false;
                forceStopMoving();
                QuindRot = QIndicator.transform.rotation;
                graphics.transform.rotation = QIndicator.transform.rotation;
                Anim.SetTrigger("Q");
                Anim.SetBool("isAttacking", true);
                Anim.SetBool("isIdle", false);
                QHB.damage = QDamage + (int)(AbilityPower * 0.75);
                canAttack = false;
                if (itemsHad[6])
                {
                    StartCoroutine(hexagon());
                }
            }
        }
    }
    public override void uniqueWLevelUp(int level)
    {
        shieldDMG = shieldBaseScale[level -1];
        maxHPShield = extraMaxShield[level -1];
    }
    public override void uniqueRLevelUp(int level)
    {
        monicaHealth = monicaBaseHealth[level - 1];
    }
    IEnumerator endQ()
    {
        yield return new WaitForSeconds(6);
        endStunQ();
    }
    public void missedQ()
    {
        CooldownQ = false;
        IndQ.StartCooldown(CDQ, this, 1);
        EndAttack();
    }
    void endStunQ()
    {
        if (QTargetted)
        {
            creepQ.isQdNic = false;
            CooldownQ = false;
            IndQ.StartCooldown(CDQ, this, 1);
            QTargetted = false;
            Anim.SetBool("hooked", false);
            hook.SetActive(false);
            creepQ.isStunned = false;
            creepQ.canMove = true;
            QSlamming = false;
        }
    }
    void LateUpdate()
    {
        if (QTargetted && !movingHookE)
        {
            if (!QSlamming)
            {
                Vector3 targetPos = transform.position - followOffset;
                targetPos.y = hookObject.transform.position.y - 0.5f;
                hookObject.transform.position += (targetPos - hookObject.transform.position) * -1.1f;
            }else
            {
                if (goingUp)
                {
                    hookObject.transform.position = hookpos + upwardsOffset;
                }else
                {
                    hookObject.transform.position = hookpos;
                }

            }
        }else
        {
            Qind2.SetActive(false);
        }
    }
    public override void QTest()
    {
        if (CooldownQ && IndQ.levelNum > 0)
        {
            stoppingAttack = Input.GetKeyUp(KeyCode.Q);
            Ab1 = Input.GetKey(KeyCode.Q);
            bool leftClick = Input.GetMouseButton(0);
            bool touch = Input.GetKeyDown(KeyCode.Q);
            if (QTargetted)
            {
                if (Ab1 && !stoppingAttack && anythingWorks && QTargetted)
                {
                    if (creepQ != null)
                    {
                        Qind2.SetActive(true);
                        Qind2.transform.position = creepQ.transform.position - new Vector3(0, -1, 0);
                        QPressed = true;
                    }
                    else{
                        QTargetted = false;
                    }
                }
                else if (QPressed || (leftClick && Ab1))
                {
                    Qind2.SetActive(false);
                    QPressed = false;
                    isInvisible = false;
                    activateQ();
                }
                else
                {
                    Qind2.SetActive(false);
                    QPressed = false;
                }
                if (Ab1 && endingAttack)
                {
                }

                if (touch)
                {
                    anythingWorks = true;
                    endingAttack = false;
                }
            }
            else
            {
                if (Ab1 && !stoppingAttack && anythingWorks)
                {
                    QIndicator.SetActive(true);
                    QPressed = true;
                }
                else if (QPressed || (leftClick && Ab1))
                {
                    QIndicator.SetActive(false);
                    QPressed = false;
                    isInvisible = false;
                    activateQ();
                }
                else
                {
                    QIndicator.SetActive(false);
                    QPressed = false;
                }
                if (Ab1 && endingAttack)
                {
                }

                if (touch)
                {
                    anythingWorks = true;
                    endingAttack = false;
                }
            }
        }
    }
    public override void RTest()
    {
        if (CooldownR && IndR.levelNum > 0)
        {
            stoppingAttack = Input.GetKeyUp(KeyCode.R);
            bool leftClick = Input.GetMouseButton(0);
            bool touch = Input.GetKeyDown(KeyCode.R);
            if (monicaAlive)
            {

                if (Ab4 && !stoppingAttack && anythingWorks)
                {
                    monicaPointer.SetActive(true);
                    RPressed = true;
                }
                else if (RPressed || (leftClick && Ab4))
                {
                    monicaPointer.SetActive(false);
                    RPressed = false;
                    isInvisible = false;
                    monica.movePointNic(monicaPos.transform.position);
                }
                else
                {
                    RIndicator.SetActive(false);
                    RPressed = false;
                }
                if (Ab4 && endingAttack)
                {
                }
                if (touch)
                {
                    anythingWorks = true;
                    endingAttack = false;
                }
            }
            else
            {
                if (Ab4 && !stoppingAttack && anythingWorks)
                {
                    RIndicator.SetActive(true);
                    lanternPosition = lanternPos.transform.position;
                    RPressed = true;
                }
                else if (RPressed || (leftClick && Ab4))
                {
                    RIndicator.SetActive(false);
                    RPressed = false;
                    isInvisible = false;
                    activateR();
                }
                else
                {
                    RIndicator.SetActive(false);
                    RPressed = false;
                }
                if (Ab4 && endingAttack)
                {
                }
                if (touch)
                {
                    anythingWorks = true;
                    endingAttack = false;
                }
            }
        }
    }
    public void monicaDied()
    {
        CooldownR = false;
        IndR.StartCooldown(CDR, this, 4);
        monicaAlive = false;
    }
    public override void R()
    {
        GameObject nicLantern = GameObject.Instantiate((GameObject)Resources.Load("NicLantern"));
        Lantern newLantern = nicLantern.GetComponent<Lantern>();
        nicLantern.transform.position = lanternPosition;
        newLantern.gm = gameManager;
        newLantern.monicaDamage = RDamage + (int)(AbilityPower * 0.1);
        newLantern.monicaHealth = monicaHealth + (int)(maxHealth * 0.15);
        newLantern.nic = this;
    }
    public override void activateR()
    {
        if (canAttack)
        {
            canMove = false;
            RindRot = RIndicator.transform.rotation;
            graphics.transform.LookAt(lanternPosition);
            forceStopMoving();
            Anim.SetTrigger("R");
            Anim.SetBool("isAttacking", true);
            Anim.SetBool("isIdle", false);
            canAttack = false;
            if (itemsHad[6])
            {               
                StartCoroutine(hexagon());
            }
        }
    }
    public void MoveHook()
    {
        QSlamming = true;
        goingUp = true;
        hookObject.transform.position = hookpos + upwardsOffset;
    }
    IEnumerator goDown()
    {
        goingUp = false;
        hookObject.transform.position = hookpos;
        creepQ.gameObject.transform.position = hookpos;
        yield return new WaitForSeconds(0.1f);
        GameObject QCrater = GameObject.Instantiate((GameObject)Resources.Load("NicQHB"));
        QCrater.transform.position = hookObject.transform.position;
        NicSpecialHitbox qhb2 = QCrater.GetComponent<NicSpecialHitbox>();
        qhb2.damage = QDamage + (int)(AbilityPower * .75);
        qhb2.player = this;
        qhb2.creepTarget = creepQ;
        QSlamming = false;
        endStunQ();
    }
    public override void ClickUpdate()
    {
        bool RMB = Input.GetMouseButton(1);
        if (RMB && canMove)
        {
            stopAbilityIndication();
            EndAttack();
            autoNum = 1;
            RaycastHit hit;
            RaycastHit yeet;
            bool cant = false;
            Ray raymond = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(raymond, out hit, Mathf.Infinity, groundOnly))
            {
                if (Physics.Linecast(transform.position + rayOffset, hit.point + rayOffset, out yeet, wallMask))
                {
                    if (yeet.collider.transform != null)
                    {
                        cant = true;
                    }
                }
                if (!canAttackAfterAuto && !cant)
                {
                    bufferedPosition = hit.point;
                    StartCoroutine(waitToMove());
                    isBuffering = true;
                }
                else if (!cant)
                {
                    canAttackAfterAuto = true;
                    NewPosition = hit.point;
                    EndAttack();
                }
            }
            cant = false;
            if (Physics.Raycast(raymond, out hit, Mathf.Infinity, creepsOnly) && hit.transform.tag == "Creep")
            {
                Creep touchedCreep = hit.transform.gameObject.GetComponent<Creep>();
                creepSelected = touchedCreep;
                if (achecker.EnemiesInRadius.Contains(touchedCreep) && canAttack && !QTargetted)
                {
                    NewPosition = transform.position;
                    stopMoving();
                    transform.LookAt(touchedCreep.transform.position);
                    transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                    if (autoOn <= 2)
                    {
                        audManager.audioList[1].Play();
                        Anim.SetTrigger("Attack" + autoOn);
                        Anim.SetBool("isAttacking", true);
                        Anim.SetBool("isIdle", false);
                    }
                    else
                    {
                        audManager.audioList[1].Play();
                        autoOn = 1;
                        Anim.SetTrigger("Attack" + autoOn);
                        Anim.SetBool("isAttacking", true);
                        Anim.SetBool("isIdle", false);
                    }
                }
                else
                {
                    walkTowardsTarget(touchedCreep);
                }
            }
        }

        if (Vector3.Distance(NewPosition, transform.position) > walkRange)
        {
            isMoving = true;
            Anim.SetBool("isIdle", false);
            Anim.SetBool("isWalking", true);

            Vector3 lookPos = NewPosition - transform.position;
            lookPos.y = 0;
            transform.position = Vector3.MoveTowards(transform.position, NewPosition, speed * Time.deltaTime);
            Quaternion transRot = Quaternion.LookRotation(lookPos, Vector3.up);
            graphics.transform.rotation = Quaternion.Slerp(transRot, graphics.transform.rotation, 0.2f);
        }
        else
        {
            stopMoving();
        }
    }
    public override void W()
    {
        addShield(shieldDMG + (int)(maxHealth * maxHPShield), 7);
        StartCoroutine(sheildLoop());
    }
    public override void E()
    {
        movingHookE = true;
        EHitbox.damage = EDamage + (int)(AbilityPower * 0.4);
        if (QTargetted)
        {
            EHitbox.damage += (int)(creepQ.maxHealth * 0.07);
        }
        audManager.audioList[2].Play();
    }
    public override void EndAttack()
    {
        movingHookE = false;
        base.EndAttack();
    }
    IEnumerator ticcolas()
    {
        yield return new WaitForSeconds(8);
        Anim.SetTrigger("Tic");
        if (IndW.levelNum > 0)
        {
            ticAuto = true;
            leftTic.SetActive(true);
            rightTic.SetActive(true);

        }
        StartCoroutine(ticcolas());
    }
    public override void abilityDescription()
    {
        int BaseEDamage = EDamage + (int)(AttackDamage * 0.55);
        Qdesc = "Nic throws his hook, latching onto and stunning the first enemy hit for 6 seconds, and dealing " + QDamage + " <color=#32fb93>(+" + (int)(AbilityPower * 0.75) + ")</color> magic damage."
            + "\nWhile the enemy is stunned, Nic can reactivate this ability to slam it into the ground, dealing the same damage again to all nearby units, and knocking them up for 1 second.";
        Wdesc = "TICCOLAS: Every 8 seconds, Nic tics, empowering his next auto attack to deal an additional " + WDamage + " <color=#32fb93>(+" + (int)(AbilityPower * 0.65) + ")</color> magic damage." +
            "\nTHICCOLAS: Nic shields himself for " + shieldDMG + "<color=red> (+" + (int)(maxHealth * maxHPShield) + ")</color> damage for 7 seconds.";
        Edesc = "Nic spins his hook in a circle, knocking back all hit enemy units and dealing " + EDamage + " <color=#32fb93>(+" + (int)(AbilityPower * 0.4) + ")</color> magic damage. If an enemy is currently hooked, "
            + "this attack deals additional damage equal to 7% of the hooked enemy's maximum health.";
        Rdesc = "Nicolas summons Monica, vanquisher of the devil, to his aid. Monica has " + monicaHealth + " <color=red>(+" + (int)(maxHealth * 0.15) + ")</color> health, and her attacks deal " + RDamage +
            " <color=#32fb93>(+" + (int)(AbilityPower * 0.1) + ")</color> magic damage. \n \nWhile monica is alive, you can recast R to change her position";
        IndQ.updateAbilityDescription(Qdesc);
        IndW.updateAbilityDescription(Wdesc);
        IndE.updateAbilityDescription(Edesc);
        IndR.updateAbilityDescription(Rdesc);
        IndQ.abilityDescription.GetComponentInChildren<Text>().fontSize = 48;
        IndQ.updateAbilityName("(Q) Grab of Death");
        IndW.updateAbilityName("(W) Ticcolas/Thiccolas");
        IndE.updateAbilityName("(E) Flayer");
        IndR.updateAbilityName("(R) Summon: Monica");
        string Pdesc = "Every enemy defeated via auto attack has a chance to spawn a soul. Walking over a soul absorbs it, permanently increasing Nic's armor and ability power by 1. \n\n\nCurrent souls: " + passiveStacks;
        passiveDesc.GetComponentInParent<AbilityIndicator>().updateAbilityDescription(Pdesc);
        passiveDesc.GetComponentInParent<AbilityIndicator>().updateAbilityName("Soul Collection");
    }
    public override void AttackCreep(Transform target)
    {
        if (!isAttacking && creepSelected != null && !QTargetted)
        {
            isAttacking = true;
            NewPosition = transform.position;
            stopMoving();
            Quaternion transRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transRot *= new Quaternion(0, 0, 0, 0);
            graphics.transform.rotation = Quaternion.Slerp(transRot, graphics.transform.rotation, 0.2f);
            Anim.SetBool("isAttacking", true);
            Anim.SetBool("isIdle", false);
            audManager.audioList[1].Play();
            if (autoOn <= 2)
            {
                Anim.SetTrigger("Attack" + autoOn);
                Anim.SetBool("isAttacking", true);
                Anim.SetBool("isIdle", false);
            }
            else
            {
                autoOn = 1;
                Anim.SetTrigger("Attack" + autoOn);
                Anim.SetBool("isAttacking", true);
                Anim.SetBool("isIdle", false);
            }
        }
    }

    public override void hitCreepWithAuto()
    {
        bool creepKilled = false;
        if (!isMoving)
        {
            canAttackAfterAuto = false;
            autoOn += 1;
            if (itemsHad[2])
            {
                Vector3 kb = (creepSelected.transform.position - transform.position).normalized / 2;
                creepSelected.takeKnockback(creepSelected.transform.position + kb, 5, 0.3f);
            }
            if (itemsHad[3])
            {
                numFelix += 1;
                if (numFelix >= 3)
                {
                    GameObject thunder = GameObject.Instantiate((GameObject)Resources.Load("Thunder"));
                    thunder.transform.position = creepSelected.transform.position + new Vector3(0, 10, 0);
                    numFelix = 0;
                }
            }
            if (itemsHad[5])
            {
                if (numGorilla <= 10)
                {
                    StartCoroutine(gorilla());
                }
            }
            if (itemsHad[7])
            {
                creepSelected.shredResist(10, 3);
            }
            if (itemsHad[8])
            {
                int rando = Random.Range(0, 99);
                if (rando >= 80)
                {
                    creepKilled = creepSelected.takeDamage(AttackDamage);
                }
            }
            if (itemsHad[10])
            {
                takeDamage((int)(AttackDamage * -0.2), false, false);
            }
            if (hexagonAttack)
            {
                creepKilled = creepSelected.takeDamage(AttackDamage + 100);
                hexagonAttack = false;
            }
            else
            {
                if (ticAuto)
                {
                    creepKilled = creepSelected.takeMagicDamage(WDamage + (int)(AbilityPower * 0.65));
                    ticAuto = false;
                    leftTic.SetActive(false);
                    rightTic.SetActive(false);
                }
                creepKilled = creepSelected.takeDamage(AttackDamage);
                ticAuto = false;
            }
            GameObject effect = GameObject.Instantiate((GameObject)Resources.Load("NicAutoFlash"));
            //GameObject hitNoise = GameObject.Instantiate((GameObject)Resources.Load("KentAutoNoise"));
            //hitNoise.GetComponent<AudioSource>().pitch = Random.Range(0.7f, 1.3f);
            effect.transform.position = creepSelected.transform.position;
            if (creepKilled)
            {
                int rand = Random.Range(0, 100);
                if (rand >= 40)
                {
                    Instantiate(soul, creepSelected.transform.position, Quaternion.identity);
                }
            }
        }
    }
    IEnumerator sheildLoop()
    {
        while (hasShield)
        {
            shieldVis.SetActive(true);
            yield return new WaitForEndOfFrame();
        }
        shieldVis.SetActive(false);
    }
    public void endDamageAuto()
    {
        Anim.SetBool("isIdle", true);
        Anim.SetBool("isWalking", false);
        Anim.SetBool("isAttacking", false);
    }
}
