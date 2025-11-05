using UnityEngine;

public interface IHitReceiver
{
    // ‘¼‚Ì•Ï”‚âŠÖ”‚ª‚ ‚Á‚Ä‚àOK

    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex);
}
