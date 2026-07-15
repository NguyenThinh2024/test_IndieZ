using UnityEngine;
using Coffee.UIExtensions;
using System.Collections;

public class UIAutoShiny : MonoBehaviour
{
    public ShinyEffectForUGUI shiny;

    [Header("Auto Settings")]
    public bool autoPlay = true;
    public float duration = 1.2f;      // thời gian ánh sáng chạy qua
    public float delayBetween = 1.0f;  // khoảng delay giữa 2 lần chạy

    Coroutine routine;

    void OnEnable()
    {
        if (shiny == null)
            shiny = GetComponent<ShinyEffectForUGUI>();

        if (autoPlay)
            routine = StartCoroutine(AutoRun());
    }

    IEnumerator AutoRun()
    {
        while (true)
        {
            // chạy shiny
            shiny.Play(duration);

            // chờ hết duration
            yield return new WaitForSeconds(duration);

            // chờ thêm delay
            if (delayBetween > 0)
                yield return new WaitForSeconds(delayBetween);
        }
    }
}
