using UnityEngine;

[DefaultExecutionOrder(100)]
public class LimbAnimator : MonoBehaviour
{
    public enum LimbType { ArmLeft, ArmRight, LegLeft, LegRight }

    [Header("=== ТИП (у каждой свой) ===")]
    public LimbType limbType = LimbType.ArmLeft;

    [Header("=== W/S: СМЕЩЕНИЕ ПО Y ===")]
    public float amplitudeY = 0.06f;

    [Header("=== A/D: ПОВОРОТ ===")]
    public float angleAD = 30f;

    [Header("=== ИНВЕРСИЯ ===")]
    public bool invertAngle = false;

    public PlayerModular player;
    public MultiPose multiPose;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();
        if (multiPose == null) multiPose = GetComponent<MultiPose>();
    }

    void LateUpdate()
    {
        if (player == null || multiPose == null) return;

        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        if (!player.IsMoving) return;

        float phaseSign = GetPhaseSign();
        if (invertAngle) phaseSign *= -1f;

        float phase = Time.time * player.stepFrequency * Mathf.PI * 2f;
        float swing = Mathf.Sin(phase) * phaseSign;
        float runMult = player.IsRunning ? 1.4f : 1f;

        int dir = player.BodyDir;
        bool sideView = (dir == 2 || dir == 3);

        if (sideView)
        {
            float fwd = (dir == 3) ? 1f : -1f;
            float angle = swing * angleAD * fwd * runMult;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
            transform.localPosition = basePosition;
        }
        else
        {
            float offsetY = swing * amplitudeY * runMult;
            transform.localPosition = basePosition + new Vector3(0f, offsetY, 0f);
            transform.localRotation = baseRotation;
        }
    }

    float GetPhaseSign()
    {
        switch (limbType)
        {
            case LimbType.ArmLeft:  return -1f;
            case LimbType.ArmRight: return  1f;
            case LimbType.LegLeft:  return  1f;
            case LimbType.LegRight: return -1f;
        }
        return 1f;
    }
}