using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ImpromptuInterface;
using VVVV.Pack.Game.Faces;
//using VVVV.PluginInterfaces.V2;


namespace VVVV.Pack.Game.Core
{
    [DataContract]
    public class Agent : DynamicObject, IDisposable, IComparable
    {
        #region pins & fields

        [DataMember]
		Dictionary<string, Bin> Data = new Dictionary<string, Bin>() ;
		
		[DataMember]
		public string Id {
			get;
			private set;
		}

        [DataMember]
        public bool Dispose
        {
            get; set; 
        }

        [DataMember]
        public ReturnCodeEnum ReturnCode
        {
            get;
            set;
        }

        [DataMember]
        public DateTime BirthTime
        {
            get;
            private set;
        }


        public Dictionary<object, ArrayList> RunningNodes { get; private set; }

        #endregion

        public Agent()
        {
            RunningNodes = new Dictionary<object, ArrayList>();
            Id = Guid.NewGuid().ToString();
            BirthTime = DateTime.Now;
            Dispose = false;
        }

        #region duck casting

        public T Face<T>() where T : class, IAgent
        {
            return Impromptu.ActLike<T>(this);
        }


        #endregion

        #region DynamicObject

        // If you try to get a value of a property  
        // not defined in the class, this method is called. 
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            bool success; 
            string name = binder.Name;
            result = null;

            // If the property name is not found in a dictionary, 
            // add it.

            if (!Data.ContainsKey(name))
            {
                throw new Exception(name + " has not been initialized!");
//                Data[name] = SpreadList.New(binder.ReturnType);
            }
            
            Bin bin = Data[name];


            
            if (binder.ReturnType.IsInstanceOfType(typeof(Bin)))
            {
                success = true;
                result = bin;
            }
            else
            {
                if (TypeIdentity.Instance.ContainsKey(binder.ReturnType))
                {
                    if (bin.Count > 0)
                    {
                        result = bin[0];
                        success = true;
                    }
                    else
                    {
                        result = null;
                        success = true; // this list is empty, no type detectable
                    }
                }
                else
                {
                    result = null;
                    success = false; // this type is not defined in TypeIdentity.cs
                }
            }
            return success;


        }

        // If you try to set a value of a property that is 
        // not defined in the class, this method is called. 
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Assign(binder.Name, value);
            return true;
        }
        #endregion DynamicObject


        #region SpreadList Access

        // you can use the ["key"] Operator to access a specific attribute 
        public Bin this[string name]
        {
            get
            {
                if (Data.ContainsKey(name)) return Data[name];
                else
                {
                    Add(name, null);
                    return Data[name];
                }
            }
            set { Data[name] = (Bin)value; }
        }

        // you can use the ["key0", "key1", "key2"] Operator to retrieve a range of specific attributes
        public IEnumerable<Bin> this[params string[] keys]
        {
            get
            {
                return keys.Select(key => Data[key]).AsEnumerable();
            }
        }

        // you can add any object, it will be automatically be wrapped by a SpreadList. 
        // that is unless you try to add a SpreadList 
        public void Add(string name, object val)
        {
            Type type;
            if (val is IEnumerable && !(val is string))
            {
                var e = (IEnumerable)val;
                var num = e.GetEnumerator();
                num.MoveNext();
                type = num.Current.GetType();
            }
            else
            {
                type = val.GetType();
            } 
            
            if (!Data.ContainsKey(name))
                Data.Add(name, Bin.New(type));

            var spread = Data[name];

            if (val is IEnumerable && !(val is string))
            {
                foreach (var o in (IEnumerable)val)
                {
                    spread.Add(o);
                }
            }
            else spread.Add(val);
        }

        public void Assign(string name, object val)
        {
            Type type;
            if (val is IEnumerable && !(val is string))
            {
                var e = (IEnumerable) val;
                var num = e.GetEnumerator();
                num.MoveNext();
                type = num.Current.GetType();
            } else
            {
                type = val.GetType();
            }

            
            if (!Data.ContainsKey(name))
                Data.Add(name, Bin.New(type));
                else Data[name].Clear();

            var spread = Data[name];

            if (val is IEnumerable && !(val is string) ) // unfortunately string is IEnumerable
            {
                foreach (var o in (IEnumerable)val)
                {
                    spread.Add(o);
                }
            } else spread.Add(val);
        }

        #endregion SpreadList Access

        #region Access Default
        //public SpreadList GetOrDefault(string name, object defaultValue)
        //{
        //    if (!Data.ContainsKey(name))
        //    {
        //        Add(name, defaultValue);                
        //    }
        //    return Data[name];
        //}

        //public object GetFirstOrDefault(string name, object defaultValue)
        //{
        //    return GetOrDefault(name, defaultValue)[0];
        //}
        
        #endregion Access Default

        #region Essentials

        public int CompareTo(object other)
        {
            if (other == null) return 0;
            else if (other is Agent)
                return BirthTime.CompareTo(((Agent)other).BirthTime);
            else return 0;
        }
        
        public object Clone()
        {
            var m = new Agent();

            foreach (string name in Data.Keys)
            {
                Bin list = Data[name];
                m.Add(name, list.Clone());

                // really deep cloning
                try
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i] = ((ICloneable)list[i]).Clone();
                    }
                }
                catch (Exception err)
                {
                    err.ToString(); // no warning
                    // not cloneble. so keep it
                }
            }

            return m;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Agent\n");
            foreach (string name in Data.Keys)
            {

                sb.Append(" " + name +" ("+ Data[name].GetTypeIdentity.ToString()+") \t: ");
                foreach (object o in Data[name])
                {
                    string typeIdentity = TypeIdentity.Instance[o.GetType()];
                    sb.Append(o.ToString() + "["+typeIdentity+"], ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        void IDisposable.Dispose()
        {
 //           foreach(var spread in Data.Values) spread.Clear();
            Data.Clear();
        }

        #endregion Essentials

        #region Running Cache
        public void AddRunning(object node, object pin)
        {
            var list = RunningNodes.ContainsKey(node) ? RunningNodes[node] : new ArrayList();
            if (!list.Contains(pin)) list.Add(pin);
            RunningNodes[node] = list;
        }

        public void RemoveRunning(object node, object pin)
        {
            var list = RunningNodes.ContainsKey(node) ? RunningNodes[node] : new ArrayList();
            if (list.Contains(pin)) list.Remove(pin);
            if (list.Count == 0)
                RunningNodes.Remove(node);
            else RunningNodes[node] = list;
        }
        #endregion
    }

}
