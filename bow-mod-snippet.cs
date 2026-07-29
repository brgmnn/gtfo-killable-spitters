bool stopit = false;
var ray = new Ray(this.transform.position + this.transform.forward * 1f, this.transform.forward);
RaycastHit hit;                                                   // ping targets used to be here
bool result = Physics.Raycast(ray, out hit, 2.5f, LayerManager.MASK_GLUEGUN_TARGETS, QueryTriggerInteraction.Ignore) || Physics.Raycast(ray, out hit, 2.5f, LayerManager.MASK_GLUEGUN_TARGETS, QueryTriggerInteraction.Ignore) || Physics.CapsuleCast(this.transform.position, this.transform.position + this.transform.forward * 1.2f, 0.04f, this.transform.forward, out hit, 2f, LayerManager.MASK_GLUEGUN_TARGETS, QueryTriggerInteraction.Ignore);
if (result && hit.collider.GetComponentInParent<GenericDamageComponent>() != null) { hit.collider.GetComponentInParent<GenericDamageComponent>().BulletDamage(20, this.weapon.Owner, hit.point, (hit.point - this.transform.position).normalized, Vector3.zero); stopit = true; }
if (result && hit.collider.GetComponentInParent<CarryItemPickup_Core>() != null) stopit = true;
if (result && (hit.collider.gameObject.name != "Interaction" || stopit))
{
    var collider = hit.collider;
    Vector3 tmppos = collider.transform.position - hit.point;
    Vector3 tmpdir = (tmppos - this.transform.position).normalized;
    this.hitagent = collider.GetComponentInParent<Agent>();
    if (this.hitagent != null)
    {
        if ((collider.GetComponentInParent<EnemyAgent>() != null && !collider.GetComponentInParent<EnemyAgent>().EnemyBehaviorData.IsFlyer) || collider.GetComponentInParent<PlayerAgent>() != null)
        {
            NetworkAPI.InvokeEvent<Plugin.ArrowNetInfo>("ArrowNetInfo", new Plugin.ArrowNetInfo(this.arrowindex, this.myslot, this.transform.position, this.velocity, this.speed, 4, (uint)this.hitagent.m_replicator.Key + 1));
            this.enemyhit = true;
            this.arrow.transform.SetParent(collider.transform);
            this.arrow.transform.localPosition = new(0, 0, 0);
            this.arrow.transform.localEulerAngles = new(0, 0, 180); // tmpdir;
            this.arrow.transform.localScale = new(0.01f, 0.3f, 0.01f);
        }
    }
        IDamageable damageable = null;
    ColliderMaterial component = collider.GetComponent<ColliderMaterial>();
    if (component != null) damageable = component.Damageable;
    if (damageable == null) damageable = collider.GetComponent<IDamageable>();
    if (damageable != null)
    {
        if (damageable.TryCast<InfectionSpitterDamage>() != null)
        {
            damageable.BulletDamage(this.speed * (float)Plugin._BowMaxDamage, weapon.Owner, Vector3.zero, Vector3.zero, Vector3.zero);
        }
        else
        {
            damageable.MeleeDamage(this.speed * (float)Plugin._BowMaxDamage, this.weapon.Owner, hit.point, (hit.point - this.transform.position).normalized, 1f, 1f, 1f, 1f, 1f, false, DamageNoiseLevel.Low);
        }
    }
    else
    {
        this.weapon.DoAttackDamage(new MeleeWeaponDamageData() { damageGO = hit.collider.gameObject, hitPos = hit.point, sourcePos = this.transform.position });
    }
    this.audio.Post(3762526143);
    this.soundendtime = Time.time;
    this.status = 2;
    this.velocity = Vector3.zero;
    this.speed = 0;
