using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;

namespace IdunnParser
{
    public class Entity : JSONClass 
    {
        static public Entity Createfrom(string archetype, JSONClass archetypeObject)
        {
            Entity e = new Entity();

            if (archetypeObject != null)
            {
                foreach (KeyValuePair<string, JSONNode> pair in archetypeObject)
                {
                    e.Add(pair.Key, pair.Value);
                }
            }

            e.Add("archetype", archetype);

            return e;
        }
    }



    //----------------------------------------------

    public class Parser
    {
        public delegate JSONNode Func(Parser P, String[] pParams);
        public delegate void LogFunc(string value);

        public Dictionary<string, JSONClass> _archetypes;

        public Dictionary<string, Func> _functions;
        public LogFunc logFunc;

        public JSONClass _root;

        public enum Operations
        {
            ASSIGNEMENT,
            FUNC_CALL,
            CREATE_ARRAY,
            COMPARISON
        }

        //----------------------------

        public void LogError(string log)
        {
            logFunc("Error>" + log);
        }

        public void LogWarning(string log)
        {
            logFunc("Warning>" + log);
        }

        //----------------------------

        public Parser()
        {
            _functions = new Dictionary<string, Func>();
            _root = new JSONClass();
            _archetypes = new Dictionary<string, JSONClass>();

            _functions.Add("createEntity", StdLib.createEntity);
        }

        public void AddArchetype(string file)
        {
            JSONClass N = JSON.Parse(file).AsObject;

            foreach (KeyValuePair<string, JSONNode> node in N)
            {
                if (node.Value.AsObject != null)
                {
                    _archetypes[node.Key] = node.Value.AsObject;
                }
            }
        }


        protected JSONClass _localRoot;
        public void Parse(string sourceCode)
        {
            _localRoot = new JSONClass();
            string[] lines = sourceCode.Split('\n');

            foreach (string str in lines)
            {
                ParseLine(str);
            }
        }

        public JSONNode ParseLine(string line)
        {
            line = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", "");
            if (line.Contains('='))
            {
                int idx = line.IndexOf('=');


                return Assignement(line.Substring(0, idx), line.Substring(idx+1));
            }
            else if (line.Contains('('))
            {
                int opening = line.IndexOf('(');
                int closing = line.LastIndexOf(')');

                if (opening >= 0 && closing >= 0)
                {
                    int parameterLength = closing - opening - 1;

                    string funcName = line.Substring(0, opening);
                    string parameters = "";
                    
                    if(parameterLength > 0)
                        parameters = line.Substring(opening + 1, parameterLength);

                    return FunctionCall(funcName, parameters);
                }
                else
                {
                    LogError("Mismatching parenthesis : " + line);
                    return null;
                }
            }
            else if (line.Contains('"'))
            {
                int pos = line.IndexOf('"');
                int end = line.LastIndexOf('"');

                JSONData d = new JSONData(line.Substring(pos+1, end - pos - 1));

                return d;
            }

            JSONClass c = ResolvePath(line);

            return c;
        }

        public JSONNode Assignement(string left, string right)
        {
            JSONNode value = ParseLine(right);

            logFunc(left + " assigned value : " + value);

            int lastPoint = left.LastIndexOf('.');
            string parentPath = left;
            string objectName = left;

            if(lastPoint >= 0)
            {//no point, local var
                parentPath = left.Substring(0, lastPoint);
                objectName = left.Substring(lastPoint+1);
            }

            JSONClass parent = ResolvePath(parentPath);

            if (parent != null)
            {
                LogWarning("ASSGINING : " + objectName + " in " + parent + " value : " + ParseLine(right));
                parent[objectName] = ParseLine(right);
                return parent[objectName];
            }

            return null;
        }

        public JSONNode Indexing(string target, string index)
        {
            return null;
        }

        public JSONNode FunctionCall(string funcName, string parameters)
        {
            string[] splitedParams = parameters.Split(',');
            List<string> filteredParam = new List<string>();

            foreach (string s in splitedParams)
            {
                if(s!="")
                    filteredParam.Add(s);
            }

            Func f;
            if (_functions.TryGetValue(funcName, out f))
            {
                return f(this, filteredParam.ToArray());
            }
            else
            {
                LogError("Unknown Function : " + funcName);
                return null;
            }
        }


        public JSONClass ResolvePath(string path)
        {
            string[] composants = path.Split('.');

            JSONClass currentClass = null;
            int startIdx = 0;
            if (composants[0] == "global")
            {
                currentClass = _root;
                startIdx = 1;
            }
            else
            {
                currentClass = _localRoot;
                startIdx = 0;
            }

            for (int i = startIdx; i < composants.Length; ++i)
            {

                if(!currentClass.Contains(composants[startIdx]))
                {
                    LogError("Path "+path+" is pointing to an unknow key");
                    return null;
                }
                else if (currentClass[composants[startIdx]] == null)
                {
                    LogError("Tried ot access null object : " + composants[startIdx] + " in path " + path);
                    return null;
                }

                currentClass = currentClass[composants[startIdx]].AsObject;
            }

            return currentClass;
        }
    }


}
