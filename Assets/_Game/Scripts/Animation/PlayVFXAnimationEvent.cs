using UnityEngine;

/// <summary>
/// Dùng để chạy script runtime khi animator trigger một event trong animation clip
/// Thay vì enable và disable object trong animation clip (với vfx có life time dài,
/// khi bật tắt object làm cho vfx bị tắt đột ngột
/// </summary>
public class PlayVFXAnimationEvent : MonoBehaviour {
    [SerializeField] private ParticleSystem vfx;

    public void PlayVFX() {
        if (vfx) {
            vfx.Play();
        }
    }

    public void StopVFX() {
        if (vfx) {
            vfx.Stop();
        }
    }

    public void TurnOnLoop() {
        var main = vfx.main;
        main.loop = true;
    }
}
