using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nexzap.Base
{
    public class DontDestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }
}

