using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
    public class BulletInfo
    {
        public enum BulletTypes { AP, HE }

        public float positiveCoefficient { get; private set; }
        public FloatCurve penetration { get; private set; }
        public string name { get; private set; }
        public BulletTypes bulletType { get; private set; }
        public float ricochetAngleMin { get; private set; }
        public float ricochetAngleMax { get; private set; }
        public static BulletInfos bullets;

        public BulletInfo(string name, float positiveCoefficient, BulletTypes bulletType, FloatCurve penetration, float ricochetAngleMin, float ricochetAngleMax)
        {
            this.name = name;
            this.positiveCoefficient = positiveCoefficient;
            this.penetration = penetration;
            this.bulletType = bulletType;
            this.ricochetAngleMax = ricochetAngleMax;
            this.ricochetAngleMin = ricochetAngleMin;
        }

        public static void Load()
        {
            bullets = new BulletInfos();
            var nodes = GameDatabase.Instance.GetConfigs("BULLET");
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i].config;
                var penetrationCurve = new FloatCurve();
                penetrationCurve.Load(node.GetNode("penetration"));
                bullets.Add(new BulletInfo(node.GetValue("name"), float.Parse(node.GetValue("positiveCoefficient")),
                    (BulletTypes)Enum.Parse(typeof(BulletTypes), node.GetValue("bulletType")),
                    penetrationCurve,
                    float.Parse(node.GetValue("ricochetAngleMin")),
                    float.Parse(node.GetValue("ricochetAngleMax"))));
            }
        }
    }

    public class BulletInfos : List<BulletInfo>
    {
        public BulletInfo this[string name]
        {
            get { return Find((value) => { return value.name == name; }); }
        }
    }
}