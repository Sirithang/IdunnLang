using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;

namespace IdunnParser
{
    public class Entity : JSONClass 
    {
        public string _path = null;

        static public Entity Createfrom(string archetype, JSONClass archetypeObject)
        {
            Entity e = new Entity();

            foreach (KeyValuePair<string, JSONNode> N in archetypeObject)
            {
                e.Add(N.Key, Parse(N.Value.ToString()));
            }

            e.Add("archetype", archetype);

            return e;
        }
    }


    public class Screen
    {
        public string _name;

        public string _text;
        //child screen 
        public List<Screen> _childs;

        public string _exec;
        public string _condition;

        public virtual void FromJSON(JSONClass root)
        {
            _text = root["text"];
            _condition = root["condition"];
            _exec = root["exec"];
            _name = root["name"];

            foreach (JSONClass c in root["childs"].AsArray)
            {
                Screen s = new Screen();
                s.FromJSON(c);
                _childs.Add(s);
            }
        }
    }

    public class Event : Screen
    {
        public List<string> _tags;

        public override void FromJSON(JSONClass root)
        {
            foreach (JSONNode n in root["tags"].AsArray)
            {
                _tags.Add(n.Value);
            }

            base.FromJSON(root);
        }
    }

    //----------------------------------------------

    public class Parser
    {
        public delegate JSONNode Func(Parser P, String[] pParams);
        public delegate void LogFunc(string value);

        //chqched for fqst query by name
        public Dictionary<string, JSONClass> _archetypes;

        public Dictionary<string, Func> _functions;
        public LogFunc logFunc;


        public List<Event> _events;
        public JSONClass _root;

        public JSONNode database = null;

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
            logFunc("Error >" + log);
        }

        public void LogWarning(string log)
        {
            logFunc("Warning >" + log);
        }

        //----------------------------

        public Parser()
        {
            _functions = new Dictionary<string, Func>();
            _root = new JSONClass();
            _archetypes = new Dictionary<string, JSONClass>();

            _functions.Add("new", StdLib.createEntity);

            database = new JSONClass();
            database.Add("archetypes", new JSONArray());
        }

        public void ReloadAllEvent()
        {
            _events.Clear();
            AddEvent(database["events"]);
        }

        public void AddEvent(string file)
        {
            JSONArray N = JSON.Parse(file).AsArray;

            foreach (JSONClass node in N)
            {
                Event e = new Event();
                e.FromJSON(node);
                _events.Add(e);
            }
        }

        public void ReloadAllArchetypes()
        {
            _archetypes.Clear();
            AddArchetype(database["archetypes"].Value);
        }

        public void AddArchetype(string file)
        {
            JSONArray N = JSON.Parse(file).AsArray;

            foreach (JSONNode node in N)
            {
                JSONClass c = node.AsObject;
                if (c != null && c.Contains("archetype"))
                {
                    _archetypes[c["archetype"].Value] = c;
                }
            }
        }

        public bool CompareNode(JSONNode a, JSONNode b, string op)
        {

            float outA, outB;
            if(float.TryParse(a, out outA) && float.TryParse(b, out outB))
            {//we try to cast stuff to float to compare
                switch (op)
                    {
                        case "==":
                            return outA == outB;
                        case "!=":
                            return outA != outB;
                        case ">=":
                            return outA >= outB;
                        case "<=":
                            return outA <= outB;
                        case ">":
                            return outA > outB;
                        case "<":
                            return outA < outB;
                        default:
                            return false;
                    }
            }
            else
            {//we couldn't, we compare as string
                switch (op)
                {
                    case "==":
                        return a.Value == b.Value;
                    case "!=":
                        return a.Value != b.Value;
                    default:
                        return false;
                };
            }
        }

        //this evaluate the piece of code and return true or false. A statement is valid if the comparison is valid
        //or if the piece return a non null JSONNode
        public bool Evaluate(string code)
        {
            string searched = null;
            int idx = -1;
            int idxend = -1;

            char[] searchChar = {'>', '<', '!', '='};

            foreach(char c in searchChar)
            {
                if (code.Contains(c))
                {
                    searched = "";
                    idx = code.IndexOf(c);
                    idxend = idx + 1;

                    searched += c;

                    if (code[idx + 1] == '=')
                    {
                        idxend++;
                        searched += '=';
                    }
                    break;
                }
            }

            if (searched != null)
            {
                string left = code.Substring(0, idx);
                string right = code.Substring(idxend);

                JSONNode l = ParseLine(left);
                JSONNode r = ParseLine(right);

                return CompareNode(l, r, searched);
            }

            return ParseLine(code) != null;
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

        protected string CleanOfWhitespace(string line)
        {
            return System.Text.RegularExpressions.Regex.Replace(line, @"\s+", "");
        }

        public JSONNode ParseLine(string line)
        {
            //line = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", "");

            if (line.Length > 2 && line[0] == '/' && line[1] == '/')
                return null;
            
            int idx = line.IndexOf('=');
            if (idx > 0 && idx < line.Length-1)
            {
                if(line[idx-1] == '+')
                    return PushValue(line.Substring(0, idx-1), ParseLine(line.Substring(idx+1)));

                if (line[idx - 1] == '-')
                    return RemoveValue(line.Substring(0, idx - 1), ParseLine(line.Substring(idx + 1)));

                return Assignement(line.Substring(0, idx), line.Substring(idx+1));
            }

            if (line.Contains("<-"))
            {
                idx = line.IndexOf("<-");

                string target = line.Substring(0, idx);
                string source = line.Substring(idx + 2, line.Length - idx - 2);

                return TransferValue(target, source);
            }

            if(line.Contains("->"))
            {
                idx = line.IndexOf("->");

                string source = line.Substring(0, idx);
                string target = line.Substring(idx + 2, line.Length - idx - 2);

                return TransferValue(target, source);
            }

            if (line.Contains('('))
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

            if (line.Contains('['))
            {
                int start = line.IndexOf('[');
                int end = line.LastIndexOf(']');

                string target = line.Substring(0, start);
                string index = line.Substring(start + 1 , end - start - 1);

                return Indexing(target, index);
            }

            if (line.Contains('"'))
            {
                int pos = line.IndexOf('"');
                int end = line.LastIndexOf('"');

                JSONData d = new JSONData(line.Substring(pos+1, end - pos - 1));

                return d;
            }

            if (line.Contains('.'))
            {
                float result;
                if (float.TryParse(line, out result))
                    return new JSONData(result);
            }

            int intres;
            if(int.TryParse(line, out intres))
                return new JSONData(intres);

            JSONNode c = ResolvePath(line);

            return c;
        }


        public JSONNode TransferValue(string left, string right)
        {
            string targetArray = CleanOfWhitespace(left);
            string originArray = CleanOfWhitespace(right);

            if (!originArray.Contains('['))
            {
                LogError("Transfer operation on " + originArray + " must be an array search expression");
                return null;
            }

            string originWOIndexing = originArray.Substring(0, originArray.IndexOf('['));

            JSONNode Value = ParseLine(originArray); 

            PushValue(targetArray, Value);
            RemoveValue(originWOIndexing, Value);

            return null;
        }

        public JSONNode PushValue(string left, JSONNode value)
        {
            if (value == null)
                return null;

            JSONNode n = ParseLine(left);

            JSONArray a = n != null ? n.AsArray : null;
            if (a != null)
            {
                a[a.Count] = value;
                return a;
            }
            else
            {
                LogError(left + " is not an array!!");
                return null;
            }
        }

        public JSONNode RemoveValue(string left, JSONNode value)
        {
            JSONNode n = ParseLine(left);

            JSONArray a = n != null ? n.AsArray : null;
            if (a != null)
            {
                for (int i = 0; i < a.Count; ++i)
                {
                    if (a[i] == value)
                    {
                        a.Remove(i);
                        return a;
                    }
                }

                LogError("Couldn't find value : " + value + " in array : " + left);
            }
            else
            {
                LogError(left + " is not an array!!");
            }

            return null;
        }

        public JSONNode Assignement(string left, string right)
        {
            string target = CleanOfWhitespace(left);

            JSONNode value = ParseLine(right);
            Entity AsEntity = value as Entity;

            logFunc(target + " assigned value : " + value);

            int lastPoint = target.LastIndexOf('.');
            string parentPath = null;
            string objectName = target;

            if(lastPoint >= 0)
            {//no point, local var
                parentPath = target.Substring(0, lastPoint);
                objectName = target.Substring(lastPoint + 1);
            }

            JSONNode foundNode = ResolvePath(parentPath);

            if (foundNode == null)
            {
                return null;
            }

            JSONClass parent = foundNode.AsObject;

            if (parent != null)
            {
                LogWarning("ASSIGNING : " + objectName + " in " + parent + " value : " + ParseLine(right));

                if (AsEntity != null && AsEntity._path != null)
                {
                    if (AsEntity._path == null)
                    {//this is a new entity stock it here.
                        AsEntity._path = parentPath + objectName;
                        parent[objectName] = value;
                    }
                    else
                    {
                        parent[objectName] = (value as Entity)._path;
                    }
                }
                else
                {
                    parent[objectName] = value;
                }
                
                return parent[objectName];
            }

            return null;
        }

        public JSONNode Indexing(string target, string index)
        {
            logFunc("indexing : " + target + " index : " + index);
            JSONNode n = ResolvePath(target);
            if (n == null)
            {
                return null;
            }
            JSONArray a = n.AsArray;

            if (a == null) return null;

            if (index.Contains(':'))
            {//research
                string[] datas = index.Split(':');

                foreach (JSONNode N in a)
                {
                    JSONClass c = N.AsObject;

                    if (c == null)
                        continue;

                    if (c.Contains(datas[0]) && c[datas[0]].Value == datas[1])
                        return c;
                }

                return null;
            }
            else
            {
                int idx = System.Convert.ToInt32(index);
                if (idx < -1)
                    return null;

                return a[idx];
            }
        }

        public JSONNode FunctionCall(string funcName, string parameters)
        {
            string[] splitedParams = parameters.Split(',');
            List<string> filteredParam = new List<string>();

            foreach (string s in splitedParams)
            {
                if(s!="")
                    filteredParam.Add(CleanOfWhitespace(s));
            }

            Func f;
            if (_functions.TryGetValue(CleanOfWhitespace(funcName), out f))
            {
                return f(this, filteredParam.ToArray());
            }
            else
            {
                LogError("Unknown Function : " + funcName);
                return null;
            }
        }


        public JSONNode ResolvePath(string path)
        {
            if (path == null)
                return _localRoot; //corner case we acces a non existing local var, creating it.

            string[] composants = CleanOfWhitespace(path).Split('.');

            JSONClass currentClass = null;
            JSONNode currentNode = null;
            int startIdx = 0;
            if (composants[0] == "global")
            {
                currentNode = _root;
                startIdx = 1;
            }
            else
            {
                currentNode = _localRoot;
                startIdx = 0;
            }

            currentClass = currentNode.AsObject;

            for (int i = startIdx; i < composants.Length; ++i)
            {

                if(!currentClass.Contains(composants[i]))
                {
                    LogError("Path "+path+" is pointing to an unknow key");
                    return null;
                }
                else if (currentClass[composants[i]] == null)
                {
                    LogError("Tried ot access null object : " + composants[i] + " in path " + path);
                    return null;
                }

                currentNode = currentClass[composants[i]];
                currentClass = currentNode.AsObject;
            }

            return currentNode;
        }
    }


}
