using System;
using System.Collections.Generic;
using UnityEngine;

public static class PluginLogic
{
    // Convert HSML → Omniverse format
    public static Dictionary<string, object> HsmlToOmniverse(Dictionary<string, object> hsml)
    {
        var position = hsml["position"] as Dictionary<string, object>;
        var rotation = hsml.ContainsKey("rotation") ? hsml["rotation"] as Dictionary<string, object> : null;

        Dictionary<string, object> transform = new Dictionary<string, object>
        {
            { "translation", new List<float>
                {
                    Convert.ToSingle(position["x"]),
                    Convert.ToSingle(position["y"]),
                    Convert.ToSingle(position["z"])
                }
            },
            { "rotation", rotation != null ?
                new List<float>
                {
                    Convert.ToSingle(rotation["x"]),
                    Convert.ToSingle(rotation["y"]),
                    Convert.ToSingle(rotation["z"]),
                    Convert.ToSingle(rotation.ContainsKey("w") ? rotation["w"] : 1f)
                } :
                new List<float> { 0, 0, 0, 1 }
            }
        };

        return new Dictionary<string, object>
        {
            { "id", hsml["entity_id"] },
            { "transform", transform },
            { "time", hsml.ContainsKey("timestamp") ? hsml["timestamp"] : null }
        };
    }

    // Convert Omniverse → HSML format
    public static Dictionary<string, object> OmniverseToHsml(Dictionary<string, object> omni)
    {
        var transform = omni["transform"] as Dictionary<string, object>;
        var translation = transform["translation"] as List<object>;
        var rotation = transform["rotation"] as List<object>;

        return new Dictionary<string, object>
        {
            { "entity_id", omni.ContainsKey("id") ? omni["id"] : omni["entity_id"] },
            { "position", new Dictionary<string, object>
                {
                    { "x", Convert.ToSingle(translation[0]) },
                    { "y", Convert.ToSingle(translation[1]) },
                    { "z", Convert.ToSingle(translation[2]) }
                }
            },
            { "rotation", new Dictionary<string, object>
                {
                    { "x", Convert.ToSingle(rotation[0]) },
                    { "y", Convert.ToSingle(rotation[1]) },
                    { "z", Convert.ToSingle(rotation[2]) },
                    { "w", Convert.ToSingle(rotation[3]) }
                }
            },
            { "timestamp", omni.ContainsKey("time") ? omni["time"] : omni["timestamp"] }
        };
    }
}
