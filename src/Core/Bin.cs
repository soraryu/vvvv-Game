using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;
using ImpromptuInterface.Dynamic;


namespace VVVV.Pack.Game.Core
{
    [Serializable]
    public class Bin<T> : Bin
    {
        public Bin()
        {
            
        }
        
        
        public Bin (params T[] values) 
        {
            foreach (var v in values)
            {
                this.Add(v);
            }

        }

        public Bin(IEnumerable<T> values)
        {
            Add(values);
        } 

        public override object First {
            get { return this[0]; }
            set
            {
                if (this.GetInnerType() == value.GetType())
                    this[0] = value;
                else throw new Exception(value.GetType().ToString() + " cannot be the first in Bin<" + this.GetInnerType() +">");
            } 
        }
        
        public override Type GetInnerType()
        {
            return typeof(T);
        }

        public override string GetTypeIdentity
        {
            get
            {
                var supportedTypes = TypeIdentity.Instance;
                if (supportedTypes.ContainsKey(typeof(T)))
                    return supportedTypes[typeof(T)];
                else return "Unsupported Type in TypeIdentity.";
            }
        }

        public new T[] ToArray()
        {
            return (T[])this.ToArray(typeof(T));
        }
        /*
         * no cast should be necessary!
         * 
        public static explicit operator Bin<T>(T[] s)  // explicit generic array to Bin conversion operator
        {
            return new Bin<T>(s);  // explicit conversion
        }

        public static explicit operator Bin<T>(T s)  // explicit generic fist value to Bin conversion operator
                {
                    return new Bin<T>(s);  // explicit conversion
                }
                */


        // implicit conversion
        public static implicit operator T(Bin<T> sl)  
        {
            return (T)sl.First;  
        }
        
        #region Essentials
        public new Bin Clone()
        {
            Bin<T> c = new Bin<T>();
            c.AssignFrom(this);

            return c;
        }
        #endregion

    }
    
    
    /// <summary>
	/// Description of 
	/// </summary>
	[Serializable]
	public abstract class Bin : ArrayList, ISerializable
	{
        public virtual Type GetInnerType() {
    		if (this.Count == 0) return typeof(object);
				else return this[0].GetType();
		}



        public virtual string GetTypeIdentity
        {
            get
            {
                if (this.Count == 0) return "Empty Spreadlist has neither a Type nor a TypeIdentity.";
                else
                {
                    var type = this[0].GetType();
                    var supportedTypes = TypeIdentity.Instance;
                    if (supportedTypes.ContainsKey(type))
                        return supportedTypes[type];
                    else return "Unsupported Type in TypeIdentity.";
                }
            }
        }

        public virtual object First
        {
            get { return this[0]; }
            set
            {
                this[0] = value;
                if (!TypeIdentity.Instance.ContainsKey(value.GetType()))
                {
                    throw new Exception(value.GetType() + " is not a supported Type in TypeIdentity.cs");
                }
                if (this.GetInnerType() != value.GetType())
                {
                    throw new Exception(value.GetType().ToString() +" cannot be the first among " +this.GetInnerType());
                }
            }
        }


        protected Bin(params object[] values) : base()
        {
            foreach (var v in values)
            {
                this.Add(v);
            }

        }

        #region Serialization
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			for (int i=0;i<this.Count;i++)
			{
				info.AddValue(i.ToString(CultureInfo.InvariantCulture), this[i]);
				
			}
		}
        #endregion

        #region ISpread conformity

        public override int Add(Object val)
        {
            var index = this.Count;
            if (val == null) return index;
            
            if (TypeIdentity.Instance.ContainsKey(val.GetType()))
            {
                return base.Add(val);
            } 

            if (val is IEnumerable)
            {
                var s = (IEnumerable) val;
                var num = s.GetEnumerator();
                
                num.MoveNext();
                if (this.GetInnerType() == num.Current.GetType())
                {
                    foreach (var o in s)
                    {
                        base.Add(o);
                    }
                    return index;
                }
            } 

            throw new Exception("Cannot add this value, it is neither a Enumeration of matching registered Type nor a matching Type.");
        }

        public void AssignFrom(IEnumerable source) {
			this.Clear();

            foreach (object o in source) {
				this.Add(o);
			}
			
		}
        #endregion

        #region alternative constructor for runtime typing of the bin
        public static Bin New(Type type)
        {
            Type spreadType = typeof(Bin<>).MakeGenericType(type);
            if (TypeIdentity.Instance.ContainsKey(type))
            {
                var sl = Activator.CreateInstance(spreadType);
                return (Bin)sl;
            }
            else
            {
                throw new Exception(type.ToString() + " is not a supported Type in TypeIdentity.cs");
            }
        }
        #endregion




    }
	
}