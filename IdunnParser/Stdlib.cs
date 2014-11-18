using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;

namespace IdunnParser
{
    class StdLib
    {
        static public JSONNode createEntity(Parser P, string[] parameters)
        {
            if (parameters.Length > 1)
            {
                P.LogError("Function new have the too many parameters");
                return null;
            }

            if (parameters.Length == 0)
            {
                Entity e = Entity.Createfrom("object", null);
                return e;
            }
            else
            {
                string type = parameters[0];
                if (P._archetypes.ContainsKey(type))
                {
                    Entity e = Entity.Createfrom(type, P._archetypes[type]);
                    return e;
                }

                P.LogError("Archetype : " + type + " Not found");
                return null;
            }
        }
    }
}
