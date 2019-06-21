using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using BepInEx.Logging;
using RoR2.UI;
using MiniRpcLib;
using MiniRpcLib.Action;
using MiniRpcLib.Func;
using System.Collections.Generic;

namespace Paddywan
{
    public class CorpseReserve : MessageBase
    {
        public float currentReserve { get; set; }
        public float maxReserve { get; set; }

        public CorpseReserve()
        {
            currentReserve = 0f;
            maxReserve = 0f;
        }

        public CorpseReserve(float current)
        {
            this.currentReserve = current;
        }

        public CorpseReserve(float current, float max)
        {
            this.currentReserve = current;
            this.maxReserve = max;
        }

        public Tuple<float, float> getReserves()
        {
            return new Tuple<float, float>(currentReserve, maxReserve);
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(currentReserve);
            writer.Write(maxReserve);
        }
        public override void Deserialize(NetworkReader reader)
        {
            this.currentReserve = reader.ReadSingle();
            this.maxReserve = reader.ReadSingle();
        }
    }
}
