using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thinh.Base.UI
{
    [CreateAssetMenu(menuName = "Thinh/UIPopupConfig", fileName = "UIPopupConfig")]
    public class UIPopupConfig : ScriptableObject
    {
        public List<UIBasePopup> popups = new List<UIBasePopup>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            //Find all UIBaseDialog prefab
            try
            {
                popups.Clear();
                string[] guids = AssetDatabase.FindAssets("t:Prefab");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        UIBasePopup dialog = prefab.GetComponent<UIBasePopup>();
                        if (dialog != null)
                        {
                            popups.Add(dialog);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
            }
        }
#endif
    }
}

