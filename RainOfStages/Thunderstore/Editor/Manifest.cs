﻿using System.Collections.Generic;
using UnityEngine;

namespace RainOfStages.Thunderstore
{
    public class Manifest : ScriptableObject
    {
        public string version_number;
        public string website_url;
        public string description;
        public List<string> dependencies;

    }
}