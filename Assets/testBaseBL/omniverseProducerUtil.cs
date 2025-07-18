using UnityEngine;
using System;
using System.Collections.Generic;

public static class OmniverseProducerUtil
{
    public static Dictionary<string, object> ConvertUnityToHsml(GameObject obj)
    {
        Vector3 position = obj.transform.position;
        Quaternion rotation = obj.transform.rotation;

        return new Dictionary<string, object>
        {
            { "entity_id", obj.name },
            { "position", new Dictionary<string, object>
                {
                    { "x", position.x },
                    { "y", position.y },
                    { "z", position.z }
                }
            },
            { "rotation", new Dictionary<string, object>
                {
                    { "x", rotation.x },
                    { "y", rotation.y },
                    { "z", rotation.z },
                    { "w", rotation.w }
                }
            },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };
    }
}
